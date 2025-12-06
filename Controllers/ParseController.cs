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
        private readonly ParseHistoryService _historyService;

        public ParseController(DouyinParseService parseService, ParseHistoryService historyService)
        {
            _parseService = parseService;
            _historyService = historyService;
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
                var task = await _parseService.SubmitDownloadTaskAsync(request.Url, request.Type ?? "video");
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

        /// <summary>
        /// 获取文本文件内容
        /// </summary>
        [HttpGet("text/{taskId}")]
        public IActionResult GetTextContent(string taskId)
        {
            var task = DownloadTaskManager.Instance.GetTask(taskId);
            if (task == null || string.IsNullOrEmpty(task.FilePath))
            {
                return Ok(new { code = -1, message = "任务不存在或文件路径为空" });
            }

            if (!System.IO.File.Exists(task.FilePath))
            {
                return Ok(new { code = -1, message = "文件不存在" });
            }

            try
            {
                var content = System.IO.File.ReadAllText(task.FilePath);
                return Ok(new { code = 0, message = "success", data = content });
            }
            catch (Exception ex)
            {
                return Ok(new { code = -1, message = $"读取文件失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 播放媒体文件（视频/音频流）
        /// </summary>
        [HttpGet("media/{taskId}")]
        [AllowAnonymous]
        public IActionResult GetMediaFile(string taskId)
        {
            var task = DownloadTaskManager.Instance.GetTask(taskId);
            if (task == null || string.IsNullOrEmpty(task.FilePath))
            {
                return NotFound("文件不存在");
            }

            if (!System.IO.File.Exists(task.FilePath))
            {
                return NotFound("文件不存在");
            }

            var ext = Path.GetExtension(task.FilePath).ToLower();
            var contentType = ext switch
            {
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(task.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, contentType, enableRangeProcessing: true);
        }

        /// <summary>
        /// 获取下载历史记录
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var list = await _historyService.GetListAsync(page, pageSize);
            var total = await _historyService.GetCountAsync();
            return Ok(new { code = 0, message = "success", data = new { list, total, page, pageSize } });
        }

        /// <summary>
        /// 删除历史记录
        /// </summary>
        [HttpDelete("history/{id}")]
        public async Task<IActionResult> DeleteHistory(string id)
        {
            var result = await _historyService.DeleteAsync(id);
            return Ok(new { code = result ? 0 : -1, message = result ? "删除成功" : "删除失败" });
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        [HttpDelete("history/clear")]
        public async Task<IActionResult> ClearHistory()
        {
            var count = await _historyService.ClearAllAsync();
            return Ok(new { code = 0, message = $"已清空 {count} 条记录" });
        }

        /// <summary>
        /// 预览历史记录文件
        /// </summary>
        [HttpGet("history/preview/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> PreviewHistory(string id)
        {
            var history = await _historyService.GetByIdAsync(id);
            if (history == null)
                return NotFound(new { code = -1, message = "历史记录不存在" });

            if (string.IsNullOrEmpty(history.FilePath) || !System.IO.File.Exists(history.FilePath))
                return NotFound(new { code = -1, message = "文件不存在或已被删除" });

            var ext = Path.GetExtension(history.FilePath).ToLower();
            var contentType = ext switch
            {
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                ".txt" => "text/plain; charset=utf-8",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(history.FilePath, FileMode.Open, FileAccess.Read);
            return File(stream, contentType, enableRangeProcessing: true);
        }
    }
}
