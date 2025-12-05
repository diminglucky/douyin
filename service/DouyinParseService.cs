using dy.net.dto;
using dy.net.model;
using dy.net.utils;
using System.Text.RegularExpressions;

namespace dy.net.service
{
    /// <summary>
    /// 抖音视频解析服务 - 通过网页抓取获取视频信息
    /// </summary>
    public class DouyinParseService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly DouyinHttpClientService _httpClientService;
        private readonly DouyinCookieService _cookieService;
        private readonly DouyinCommonService _commonService;

        public DouyinParseService(
            IHttpClientFactory clientFactory,
            DouyinHttpClientService httpClientService,
            DouyinCookieService cookieService,
            DouyinCommonService commonService)
        {
            _clientFactory = clientFactory;
            _httpClientService = httpClientService;
            _cookieService = cookieService;
            _commonService = commonService;
        }

        /// <summary>
        /// 从分享链接解析视频ID
        /// </summary>
        public async Task<string> ExtractAwemeIdAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            // 尝试直接从URL提取视频ID
            // 格式1: https://www.douyin.com/video/7123456789
            var videoMatch = Regex.Match(url, @"douyin\.com/video/(\d+)");
            if (videoMatch.Success)
                return videoMatch.Groups[1].Value;

            // 格式2: https://www.douyin.com/note/7123456789
            var noteMatch = Regex.Match(url, @"douyin\.com/note/(\d+)");
            if (noteMatch.Success)
                return noteMatch.Groups[1].Value;

            // 格式3: 短链接 https://v.douyin.com/xxxxx/
            if (url.Contains("v.douyin.com") || url.Contains("v.ixigua.com"))
            {
                try
                {
                    using var httpClient = _clientFactory.CreateClient("dy_collect");
                    // 不自动跟随重定向，手动获取Location
                    var handler = new HttpClientHandler { AllowAutoRedirect = false };
                    using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                    var response = await client.GetAsync(url);
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                    {
                        var location = response.Headers.Location?.ToString();
                        if (!string.IsNullOrEmpty(location))
                        {
                            // 递归解析重定向后的URL
                            return await ExtractAwemeIdAsync(location);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error($"解析短链接失败: {ex.Message}");
                }
            }

            // 格式4: 从文本中提取链接
            var linkMatch = Regex.Match(url, @"https?://[^\s]+douyin[^\s]+");
            if (linkMatch.Success && linkMatch.Value != url)
            {
                return await ExtractAwemeIdAsync(linkMatch.Value);
            }

            return null;
        }

        /// <summary>
        /// 从HTML中提取视频下载地址
        /// </summary>
        private string ExtractVideoUrlFromHtml(string html, string awemeId)
        {
            try
            {
                // 查找视频播放地址 - 通常在 play_addr 或 playAddr 中
                var patterns = new[]
                {
                    @"""play_addr""[^}]*""url_list""\s*:\s*\[""([^""]+)""",
                    @"""playAddr""[^}]*""urlList""\s*:\s*\[""([^""]+)""",
                    @"https://[^""]*douyinvod[^""]*\.mp4[^""]*",
                    @"https://v[0-9]*-[^""]*\.douyinvod\.com/[^""]+",
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(html, pattern);
                    if (match.Success)
                    {
                        var url = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                        // URL解码
                        url = url.Replace("\\u002F", "/").Replace("\\/", "/");
                        if (url.StartsWith("http"))
                        {
                            Serilog.Log.Debug($"提取到视频URL: {url.Substring(0, Math.Min(100, url.Length))}...");
                            return url;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning($"提取视频URL失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 获取视频详情 - 使用和同步功能相同的API
        /// </summary>
        public async Task<ParseVideoResponse> GetVideoDetailAsync(string awemeId, string cookie = null)
        {
            if (string.IsNullOrWhiteSpace(awemeId))
                return null;

            // 如果没有提供cookie，从数据库获取第一个可用的
            if (string.IsNullOrWhiteSpace(cookie))
            {
                var cookies = await _cookieService.GetAllOpendAsync(x => x.Status == 1);
                cookie = cookies?.FirstOrDefault()?.Cookies;
            }

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return new ParseVideoResponse
                {
                    AwemeId = awemeId,
                    DownloadStatus = 3,
                    Message = "请先配置抖音Cookie"
                };
            }

            try
            {
                // 调用现有的API获取视频详情（和同步功能使用相同的方法）
                var detail = await _httpClientService.GetVideoDetailAsync(awemeId, cookie);
                
                if (detail == null)
                {
                    return new ParseVideoResponse
                    {
                        AwemeId = awemeId,
                        DownloadStatus = 3,
                        Message = "获取视频详情失败，请检查Cookie是否有效"
                    };
                }

                // 获取视频播放地址
                var videoUrl = detail.Video?.BitRate?
                    .Where(x => !string.IsNullOrEmpty(x.Format))
                    .FirstOrDefault()?.PlayAddr?.UrlList?.FirstOrDefault();

                var isImagePost = detail.Images != null && detail.Images.Count > 0;

                // 获取音频地址
                var audioUrl = detail.Music?.PlayUrl?.UrlList?.FirstOrDefault();

                return new ParseVideoResponse
                {
                    AwemeId = detail.AwemeId ?? awemeId,
                    Title = detail.Desc,
                    Author = detail.Author?.Nickname,
                    AuthorId = detail.Author?.Uid,
                    CoverUrl = detail.Video?.Cover?.UrlList?.FirstOrDefault(),
                    IsImagePost = isImagePost,
                    DownloadStatus = 0,
                    Message = "解析成功",
                    VideoUrl = videoUrl,
                    AudioUrl = audioUrl
                };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"获取视频详情异常: {ex.Message}");
                return new ParseVideoResponse
                {
                    AwemeId = awemeId,
                    DownloadStatus = 3,
                    Message = $"解析异常: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 提交异步下载任务（立即返回任务ID）
        /// </summary>
        /// <param name="url">视频链接</param>
        /// <param name="type">下载类型：video-视频 audio-音频</param>
        public async Task<DownloadTask> SubmitDownloadTaskAsync(string url, string type = "video")
        {
            // 1. 提取视频ID
            var awemeId = await ExtractAwemeIdAsync(url);
            if (string.IsNullOrWhiteSpace(awemeId))
            {
                return new DownloadTask
                {
                    Status = DownloadTaskStatus.Failed,
                    Message = "无法解析视频ID"
                };
            }

            // 2. 添加到任务队列
            var taskId = DownloadTaskManager.Instance.AddTask(url, awemeId, type);
            var task = DownloadTaskManager.Instance.GetTask(taskId);

            // 3. 启动后台处理
            _ = ProcessTasksAsync();

            return task;
        }

        /// <summary>
        /// 后台处理下载任务
        /// </summary>
        private async Task ProcessTasksAsync()
        {
            if (DownloadTaskManager.Instance.IsProcessing)
                return;

            DownloadTaskManager.Instance.IsProcessing = true;

            try
            {
                while (true)
                {
                    var taskId = DownloadTaskManager.Instance.DequeueTask();
                    if (taskId == null) break;

                    var task = DownloadTaskManager.Instance.GetTask(taskId);
                    if (task == null) continue;

                    try
                    {
                        // 更新状态为下载中
                        DownloadTaskManager.Instance.UpdateTask(taskId, t =>
                        {
                            t.Status = DownloadTaskStatus.Running;
                            t.Message = "正在下载...";
                        });

                        // 执行下载（传入任务的type）
                        var result = await ParseAndDownloadAsync(task.Url, null, task.Type);

                        // 更新结果
                        DownloadTaskManager.Instance.UpdateTask(taskId, t =>
                        {
                            t.Title = result?.Title;
                            t.Author = result?.Author;
                            t.CoverUrl = result?.CoverUrl;
                            t.FilePath = result?.FilePath;
                            t.Status = result?.DownloadStatus == 2 ? DownloadTaskStatus.Completed : DownloadTaskStatus.Failed;
                            t.Message = result?.Message ?? "下载失败";
                            t.CompleteTime = DateTime.Now;
                        });
                    }
                    catch (Exception ex)
                    {
                        DownloadTaskManager.Instance.UpdateTask(taskId, t =>
                        {
                            t.Status = DownloadTaskStatus.Failed;
                            t.Message = ex.Message;
                            t.CompleteTime = DateTime.Now;
                        });
                    }
                }

                // 清理旧任务
                DownloadTaskManager.Instance.CleanupOldTasks();
            }
            finally
            {
                DownloadTaskManager.Instance.IsProcessing = false;
            }
        }

        /// <summary>
        /// 解析并下载视频或音频
        /// </summary>
        /// <param name="url">视频链接</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="type">下载类型：video-视频 audio-音频 text-转文字</param>
        public async Task<ParseVideoResponse> ParseAndDownloadAsync(string url, string savePath = null, string type = "video")
        {
            var typeLower = type?.ToLower();
            var isAudio = typeLower == "audio" || typeLower == "text";
            var isText = typeLower == "text";

            // 1. 提取视频ID
            var awemeId = await ExtractAwemeIdAsync(url);
            if (string.IsNullOrWhiteSpace(awemeId))
            {
                return new ParseVideoResponse
                {
                    DownloadStatus = 3,
                    Message = "无法从链接中解析视频ID，请检查链接格式"
                };
            }

            // 2. 获取Cookie
            var cookies = await _cookieService.GetAllOpendAsync(x => x.Status == 1);
            var cookieInfo = cookies?.FirstOrDefault();
            if (cookieInfo == null)
            {
                return new ParseVideoResponse
                {
                    AwemeId = awemeId,
                    DownloadStatus = 3,
                    Message = "请先配置抖音Cookie"
                };
            }

            // 3. 获取视频详情（包含下载地址）
            var detail = await GetVideoDetailAsync(awemeId, cookieInfo.Cookies);
            if (detail == null || detail.DownloadStatus == 3)
            {
                return detail ?? new ParseVideoResponse
                {
                    AwemeId = awemeId,
                    DownloadStatus = 3,
                    Message = "获取详情失败"
                };
            }

            // 4. 检查是否为图文
            if (detail.IsImagePost)
            {
                detail.DownloadStatus = 3;
                detail.Message = "图文类型暂不支持下载";
                return detail;
            }

            // 5. 检查下载地址
            var downloadUrl = isAudio ? detail.AudioUrl : detail.VideoUrl;
            var needExtract = isAudio && string.IsNullOrWhiteSpace(downloadUrl); // 需要从视频提取音频
            
            if (needExtract)
            {
                // 原声视频：需要先下载视频再提取音频
                downloadUrl = detail.VideoUrl;
                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    detail.DownloadStatus = 3;
                    detail.Message = "未找到视频地址";
                    return detail;
                }
                Serilog.Log.Debug("原声视频，将从视频中提取音频");
            }
            else if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                detail.DownloadStatus = 3;
                detail.Message = isAudio ? "未找到音频地址" : "未找到视频地址";
                return detail;
            }

            // 6. 执行下载
            try
            {
                // 确定保存路径
                if (string.IsNullOrWhiteSpace(savePath))
                {
                    savePath = cookieInfo.SavePath ?? Environment.CurrentDirectory;
                }

                // 使用视频标题命名，清理非法字符
                var safeTitle = string.IsNullOrWhiteSpace(detail.Title) ? awemeId : 
                    Regex.Replace(detail.Title, @"[\\/:*?""<>|\n\r]", "").Trim();
                // 限制文件名长度
                if (safeTitle.Length > 80) safeTitle = safeTitle.Substring(0, 80);
                
                var ext = (isAudio && !needExtract) ? ".mp3" : ".mp4";
                var audioPath = Path.Combine(savePath, "parse", $"{safeTitle}.mp3");
                var videoPath = Path.Combine(savePath, "parse", $"{safeTitle}.mp4");
                var fullPath = needExtract ? videoPath : Path.Combine(savePath, "parse", $"{safeTitle}{ext}");

                // 确保目录存在
                var dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                detail.DownloadStatus = 1;
                detail.Message = needExtract ? "正在下载视频..." : "正在下载...";

                // 执行下载
                var success = await _httpClientService.DownloadAsync(downloadUrl, fullPath, cookieInfo.Cookies);

                if (!success)
                {
                    detail.DownloadStatus = 3;
                    detail.Message = "下载失败，请重试";
                    return detail;
                }

                // 如果需要提取音频
                if (needExtract)
                {
                    detail.Message = "正在提取音频...";
                    Serilog.Log.Debug($"从视频提取音频: {videoPath} -> {audioPath}");
                    
                    using var ffmpeg = new FFmpegHelper();
                    var extractSuccess = await ffmpeg.ExtractAudioAsync(videoPath, audioPath);
                    
                    // 删除临时视频文件
                    try { File.Delete(videoPath); } catch { }

                    if (extractSuccess)
                    {
                        // 如果需要转文字
                        if (isText)
                        {
                            detail.Message = "正在转录文字...";
                            Serilog.Log.Debug($"开始转录: {audioPath}");
                            
                            var whisper = new WhisperHelper();
                            var txtPath = await whisper.TranscribeAsync(audioPath);
                            
                            if (!string.IsNullOrEmpty(txtPath))
                            {
                                // 删除音频文件，只保留文字
                                try { File.Delete(audioPath); } catch { }
                                detail.DownloadStatus = 2;
                                detail.FilePath = txtPath;
                                detail.Message = $"转录完成: {txtPath}";
                                Serilog.Log.Debug($"转录成功: {txtPath}");
                            }
                            else
                            {
                                detail.DownloadStatus = 3;
                                detail.Message = "转录失败";
                            }
                        }
                        else
                        {
                            detail.DownloadStatus = 2;
                            detail.FilePath = audioPath;
                            detail.Message = $"音频提取完成: {audioPath}";
                            Serilog.Log.Debug($"音频提取成功: {audioPath}");
                        }
                    }
                    else
                    {
                        detail.DownloadStatus = 3;
                        detail.Message = "音频提取失败";
                    }
                }
                else
                {
                    // 如果是直接下载的音频且需要转文字
                    if (isText && isAudio)
                    {
                        detail.Message = "正在转录文字...";
                        Serilog.Log.Debug($"开始转录: {fullPath}");
                        
                        var whisper = new WhisperHelper();
                        var txtPath = await whisper.TranscribeAsync(fullPath);
                        
                        if (!string.IsNullOrEmpty(txtPath))
                        {
                            try { File.Delete(fullPath); } catch { }
                            detail.DownloadStatus = 2;
                            detail.FilePath = txtPath;
                            detail.Message = $"转录完成: {txtPath}";
                            Serilog.Log.Debug($"转录成功: {txtPath}");
                        }
                        else
                        {
                            detail.DownloadStatus = 3;
                            detail.Message = "转录失败";
                        }
                    }
                    else
                    {
                        var typeText = isAudio ? "音频" : "视频";
                        detail.DownloadStatus = 2;
                        detail.FilePath = fullPath;
                        detail.Message = $"下载完成: {fullPath}";
                        Serilog.Log.Debug($"{typeText}下载成功: {fullPath}");
                    }
                }

                return detail;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"下载异常: {ex.Message}");
                detail.DownloadStatus = 3;
                detail.Message = $"下载异常: {ex.Message}";
                return detail;
            }
        }
    }
}
