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
        /// 解析视频信息（不下载）
        /// </summary>
        /// <param name="request">包含视频链接</param>
        /// <returns>视频详情</returns>
        [HttpPost("info")]
        public async Task<IActionResult> ParseInfo([FromBody] ParseVideoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return Ok(new { code = -1, error = "请输入视频链接" });
            }

            try
            {
                // 1. 提取视频ID
                var awemeId = await _parseService.ExtractAwemeIdAsync(request.Url);
                if (string.IsNullOrWhiteSpace(awemeId))
                {
                    return Ok(new { code = -1, error = "无法解析视频链接，请检查格式" });
                }

                // 2. 获取视频详情
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
        /// 解析并下载视频
        /// </summary>
        /// <param name="request">包含视频链接和可选保存路径</param>
        /// <returns>下载结果</returns>
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
