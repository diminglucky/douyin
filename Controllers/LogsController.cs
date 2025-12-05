using dy.net.dto;
using dy.net.service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace dy.net.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly DouyinHttpClientService douyinHttpClientService;

        public LogsController(IWebHostEnvironment webHostEnvironment,DouyinHttpClientService douyinHttpClientService)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.douyinHttpClientService = douyinHttpClientService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLog(string type, string date)
        {
            // 统一使用当前工作目录，避免开发/生产环境路径不一致
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", $"log-{type}-{date}.txt");
            if (!System.IO.File.Exists(filePath))
            {
                var msg = type == "error" 
                    ? $"暂无错误日志 (log-error-{date}.txt)，说明系统运行正常！" 
                    : $"日志文件 log-{type}-{date}.txt 不存在";
                return Ok(msg);
            }
            return PhysicalFile(filePath, "text/plain; charset=utf-8");

            //下面的方案提示文件被占用
            //var encoding = Encoding.GetEncoding("UTF-8"); // 指定文本文件的编码
            //var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            //var fileContent = encoding.GetString(fileBytes);
            //return  Content (fileContent, "text/plain", encoding);
        }
    }
}
