using dy.net.dto;
using dy.net.service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dy.net.Controllers
{
    /// <summary>
    /// 视频解析下载控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ParseController : ControllerBase
    {
        private readonly DouyinParseService _parseService;

        public ParseController(DouyinParseService parseService)
        {
            _parseService = parseService;
        }

        /// <summary>
        /// 提交下载任务（异步，立即返回）
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTask([FromBody] ParseVideoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return Ok(new { code = -1, message = "请输入视频链接" });
            }

            try
            {
                var task = await _parseService.SubmitDownloadTaskAsync(request.Url);
                return Ok(new { code = 0, message = "success", data = task });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"提交任务失败: {ex.Message}");
                return Ok(new { code = -1, message = $"提交失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 查询任务状态
        /// </summary>
        [HttpGet("task/{taskId}")]
        public IActionResult GetTask(string taskId)
        {
            var task = DownloadTaskManager.Instance.GetTask(taskId);
            if (task == null)
            {
                return Ok(new { code = -1, message = "任务不存在" });
            }
            return Ok(new { code = 0, message = "success", data = task });
        }

        /// <summary>
        /// 获取所有任务列表
        /// </summary>
        [HttpGet("tasks")]
        public IActionResult GetAllTasks()
        {
            var tasks = DownloadTaskManager.Instance.GetAllTasks();
            return Ok(new { code = 0, message = "success", data = tasks });
        }

        /// <summary>
        /// 解析视频信息（不下载）
        /// </summary>
        [HttpPost("info")]
        public async Task<IActionResult> ParseInfo([FromBody] ParseVideoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return Ok(new { code = -1, error = "请输入视频链接" });
            }

            try
            {
                var awemeId = await _parseService.ExtractAwemeIdAsync(request.Url);
                if (string.IsNullOrWhiteSpace(awemeId))
                {
                    return Ok(new { code = -1, error = "无法解析视频链接" });
                }

                var detail = await _parseService.GetVideoDetailAsync(awemeId);
                if (detail == null)
                {
                    return Ok(new { code = -1, error = "获取视频信息失败" });
                }

                return Ok(new { code = 0, data = detail });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"解析视频失败: {ex.Message}");
                return Ok(new { code = -1, error = $"解析失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 同步下载（等待完成）
        /// </summary>
        [HttpPost("download")]
        public async Task<IActionResult> ParseAndDownload([FromBody] ParseVideoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return Ok(new { code = -1, error = "请输入视频链接" });
            }

            try
            {
                var result = await _parseService.ParseAndDownloadAsync(request.Url, request.SavePath);

                if (result.DownloadStatus == 2)
                {
                    return Ok(new { code = 0, data = result, message = "下载成功" });
                }
                else
                {
                    return Ok(new { code = -1, data = result, error = result.Message });
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"下载视频失败: {ex.Message}");
                return Ok(new { code = -1, error = $"下载失败: {ex.Message}" });
            }
        }
    }
}
