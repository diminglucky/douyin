using dy.net.dto;
using dy.net.utils;
using NetTaste;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;

namespace dy.net.service
{
    public class DouyinHttpClientService
    {
        public static readonly string DouYinApi = "https://www.douyin.com/aweme/v1/web/aweme";
        // 随机数生成器（避免重复实例化，保证随机性）
        private readonly IHttpClientFactory _clientFactory;
        // 下载信号量锁：初始计数1，最大并发1（同时只能一个下载任务）
        private readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(1, 1);
        public DouyinHttpClientService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }


        /// <summary>
        /// 查询用户收藏的视频
        /// </summary>
        /// <param name="cursor"></param>
        /// <param name="count"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public async Task<DouyinVideoInfo> SyncCollectVideos(string cursor, string count, string cookie)
        {
            if (string.IsNullOrWhiteSpace(cursor))
            {
                throw new ArgumentException($"“{nameof(cursor)}”不能为 null 或空。", nameof(cursor));
            }

            if (string.IsNullOrWhiteSpace(count))
            {
                throw new ArgumentException($"“{nameof(count)}”不能为 null 或空。", nameof(count));
            }

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new ArgumentException($"“{nameof(cookie)}”不能为 null 或空。", nameof(cookie));
            }

            try
            {
                using var httpClient = _clientFactory.CreateClient("dy_collect");
                if (httpClient.DefaultRequestHeaders.Contains("Cookie"))
                {
                    httpClient.DefaultRequestHeaders.Remove("Cookie");
                }
                httpClient.DefaultRequestHeaders.Add("Cookie", cookie);

                var dics = DouyinBaseParamDics.CollectParams;
                dics["cursor"]=cursor;
                dics["count"] = count;
                try
                {
                    var token = await TokenManager.GenRealMsTokenAsync();
                    dics["msToken"] = token;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error($"获取mstoken失败{ex.Message}");
                }
            string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";
            var endPoint = BogusManager.XbModel2Endpoint($"{DouYinApi}/listcollection", dics, UserAgent);
                var respose = await httpClient.PostAsync(endPoint, null);
                if (respose.IsSuccessStatusCode)
                {
                    var data = await respose.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<DouyinVideoInfo>(data);
                }
                else
                {
                    Serilog.Log.Error($"SyncCollectVideos fail: {respose.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"SyncCollectVideos error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 查询用户喜欢的视频
        /// </summary>
        /// <param name="count"></param>
        /// <param name="cursor"></param>
        /// <param name="secUserId"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<DouyinVideoInfo> SyncFavoriteVideos(string count,string cursor, string secUserId, string cookie)
        {
            if (string.IsNullOrWhiteSpace(cursor))
            {
                throw new ArgumentException($"“{nameof(cursor)}”不能为 null 或空。", nameof(cursor));
            }

            if (string.IsNullOrWhiteSpace(secUserId))
            {
                throw new ArgumentException($"“{nameof(secUserId)}”不能为 null 或空。", nameof(secUserId));
            }

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new ArgumentException($"“{nameof(cookie)}”不能为 null 或空。", nameof(cookie));
            }

            try
            {
                using var httpClient = _clientFactory.CreateClient("dy_favorite");
                if (httpClient.DefaultRequestHeaders.Contains("Cookie"))
                {
                    httpClient.DefaultRequestHeaders.Remove("Cookie");
                }
                httpClient.DefaultRequestHeaders.Add("Cookie", cookie);

                var dics = DouyinBaseParamDics.FavoriteParams;
                {
                    // 添加动态参数
                    dics["max_cursor"] = cursor;
                    dics["sec_user_id"] = secUserId;
                    dics["count"] = count;
                }

                // 构建请求URL
                var queryString = new FormUrlEncodedContent(dics);
                string fullUrl = $"{DouYinApi}/favorite?{await queryString.ReadAsStringAsync()}";
                var respose = await httpClient.GetAsync(fullUrl);
                if (respose.IsSuccessStatusCode)
                {
                    var data = await respose.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<DouyinVideoInfo>(data);
                }
                else
                {
                    Serilog.Log.Error($"SyncFavoriteVideos fail: {respose.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"SyncFavoriteVideos error: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// 查询Up主作品
        /// </summary>
        /// <param name="count"></param>
        /// <param name="cursor"></param>
        /// <param name="secUserId"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<DouyinVideoInfo> SyncUpderPostVideos(string count, string cursor, string secUserId, string cookie)
        {
            if (string.IsNullOrWhiteSpace(cursor))
            {
                throw new ArgumentException($"“{nameof(cursor)}”不能为 null 或空。", nameof(cursor));
            }

            if (string.IsNullOrWhiteSpace(secUserId))
            {
                throw new ArgumentException($"“{nameof(secUserId)}”不能为 null 或空。", nameof(secUserId));
            }

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new ArgumentException($"“{nameof(cookie)}”不能为 null 或空。", nameof(cookie));
            }

            try
            {
                using var httpClient = _clientFactory.CreateClient("dy_uper");
                if (httpClient.DefaultRequestHeaders.Contains("Cookie"))
                {
                    httpClient.DefaultRequestHeaders.Remove("Cookie");
                }
                httpClient.DefaultRequestHeaders.Add("Cookie", cookie);

                var parameters = DouyinBaseParamDics.InitializeDouyinPostParams();//修复关注的不下载图文视频
                {
                    // 添加动态参数
                    parameters["max_cursor"] = cursor;
                    parameters["sec_user_id"] = secUserId;
                    parameters["count"] = count;
                }

                // 构建请求URL
                var queryString = new FormUrlEncodedContent(parameters);
                string fullUrl = $"{DouYinApi}/post?{await queryString.ReadAsStringAsync()}";
                //var ablog = new ABogus();//计算X-Bogus
                //var a_bogus = ablog.GetValue(parameters);

                //fullUrl += $"&X-Bogus={a_bogus}";
                var respose = await httpClient.GetAsync(fullUrl);
                if (respose.IsSuccessStatusCode)
                {
                    var data = await respose.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<DouyinVideoInfo>(data);
                }
                else
                {
                    Serilog.Log.Error($"SyncUpderPostVideos fail: {respose.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"SyncUpderPostVideos error: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// 查询我的关注用户列表
        /// </summary>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        /// <param name="secUserId"></param>
        /// <param name="cookie"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public async Task<DouyinFollowInfo> SyncMyFollows(string count,string offset,string secUserId,string cookie,Action<FollowErrorDto> callBack)
        {

            try
            {
                using var httpClient = _clientFactory.CreateClient("dy_follow");
                if (httpClient.DefaultRequestHeaders.Contains("Cookie"))
                {
                    httpClient.DefaultRequestHeaders.Remove("Cookie");
                }
                httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
                var dics = DouyinBaseParamDics.MyFollowParams;
                {
                    // 添加动态参数
                    dics["sec_user_id"] = secUserId;
                    dics["count"] = count;
                    dics["offset"] = offset;
                }
                // 构建请求URL
                var queryString = new FormUrlEncodedContent(dics);
                string fullUrl = $"https://www.douyin.com/aweme/v1/web/user/following/list/?{await queryString.ReadAsStringAsync()}";
                var respose = await httpClient.GetAsync(fullUrl);
                if (respose.IsSuccessStatusCode)
                {
                    var data = await respose.Content.ReadAsStringAsync();
                    var res= JsonConvert.DeserializeObject<DouyinFollowInfo>(data);
                    if (res != null)
                    {
                        if (res.Followings == null)
                        {
                            var err = JsonConvert.DeserializeObject<FollowErrorDto>(data);
                         
                            if (err != null)
                            {
                                Serilog.Log.Error($"SyncMyFollows error: {err.StatusMsg}");
                                callBack(err);
                            }
                        }
                        else
                        {
                            callBack(new FollowErrorDto { StatusCode = 0 });
                        }
                    }
                    else
                    {
                        Serilog.Log.Error($"SyncMyFollows error: 反序列化结果为空,接口返回数据：{data}");
                        callBack(new FollowErrorDto { StatusCode = 8,StatusMsg="未知" });
                    }
                    return res;
                }
                else
                {
                    Serilog.Log.Error($"SyncMyFollows fail: {respose.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"SyncMyFollows error: {ex.Message}");
                throw;
            }
        }


        //
        /// <summary>
        /// 下载文件并保存到本地（支持重试机制，优化批量下载稳定性）
        /// </summary>
        /// <param name="videoUrl">文件地址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="cookie">请求Cookie</param>
        /// <param name="cancellationToken">取消令牌（用于终止任务）</param>
        /// <param name="streamTimeout">流读取超时时间（默认60秒）</param>
        /// <param name="maxRetryCount">最大重试次数（默认3次）</param>
        /// <param name="initialRetryDelay">初始重试延迟（默认1秒，指数退避）</param>
        /// <returns>是否下载成功</returns>
        public async Task<bool> DownloadAsync(
            string videoUrl,
            string savePath,
            string cookie,
            CancellationToken cancellationToken = default,
            TimeSpan? streamTimeout = null,
            int maxRetryCount = 3,
            TimeSpan? initialRetryDelay = null)
        {
            // 重试参数初始化
            int retryCount = 0;
            var retryDelay = initialRetryDelay ?? TimeSpan.FromSeconds(1); // 初始延迟1秒
            streamTimeout ??= TimeSpan.FromSeconds(60); // 流读取超时默认60秒

            while (true)
            {
                try
                {
                    // 执行单次下载逻辑（提取为内部方法，避免代码重复）
                    return await TryDownloadOnceAsync(
                        videoUrl, savePath, cookie, cancellationToken, streamTimeout.Value);
                }
                catch (Exception ex) when (IsRetryableException(ex) && retryCount < maxRetryCount)
                {
                    // 可重试异常且未达最大次数，等待后重试
                    retryCount++;
                    var delay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1)); // 指数退避（1s→2s→4s...）
                    Serilog.Log.Warning(ex, $"下载失败（第{retryCount}/{maxRetryCount}次重试）：{videoUrl}，将在{delay.TotalSeconds:F1}秒后重试");

                    try
                    {
                        // 等待重试延迟（响应取消令牌）
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // 等待期间被取消，直接返回失败
                        Serilog.Log.Information($"重试等待被取消：{videoUrl}");
                        return false;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // 主动取消，不重试
                    Serilog.Log.Information($"下载被取消：{videoUrl}");
                    return false;
                }
                catch (Exception ex)
                {
                    // 不可重试的异常（如参数错误、权限不足等），直接失败
                    Serilog.Log.Error(ex, $"下载失败（不可重试）：{videoUrl}");
                    CleanupIncompleteFile(savePath); // 清理不完整文件
                    return false;
                }
            }
        }

        /// <summary>
        /// 下载文件并保存到本地（支持重试机制+单线程限制，同时只能一个下载）
        /// </summary>
        /// <param name="videoUrl">文件地址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="cookie">请求Cookie</param>
        /// <param name="httpclientName">HttpClient名称（默认"dy_down1"）</param>
        /// <param name="cancellationToken">取消令牌（用于终止任务）</param>
        /// <param name="streamTimeout">流读取超时时间（默认60秒）</param>
        /// <param name="maxRetryCount">最大重试次数（默认3次）</param>
        /// <param name="initialRetryDelay">初始重试延迟（默认1秒，指数退避）</param>
        /// <returns>是否下载成功</returns>
        //public async Task<bool> DownloadAsync(
        //       string videoUrl,
        //       string savePath,
        //       string cookie,
        //       CancellationToken cancellationToken = default,
        //       TimeSpan? streamTimeout = null,
        //       int maxRetryCount = 3,
        //       TimeSpan? initialRetryDelay = null)
        //{
        //    bool lockAcquired = false;
        //    try
        //    {
        //        // 申请锁：如果已有下载任务在执行，会阻塞等待直到锁释放
        //        // 传入cancellationToken支持取消等待
        //        await _downloadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        //        lockAcquired = true; // 标记锁已获取

        //        // 重试参数初始化
        //        int retryCount = 0;
        //        var retryDelay = initialRetryDelay ?? TimeSpan.FromSeconds(1);
        //        streamTimeout ??= TimeSpan.FromSeconds(60);

        //        while (true)
        //        {
        //            try
        //            {
        //                return await TryDownloadOnceAsync(
        //                    videoUrl, savePath, cookie, cancellationToken, streamTimeout.Value);
        //            }
        //            catch (Exception ex) when (IsRetryableException(ex) && retryCount < maxRetryCount)
        //            {
        //                retryCount++;
        //                var delay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
        //                Serilog.Log.Warning(ex, $"下载失败（第{retryCount}/{maxRetryCount}次重试）：{videoUrl}，将在{delay.TotalSeconds:F1}秒后重试");

        //                try
        //                {
        //                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        //                }
        //                catch (OperationCanceledException)
        //                {
        //                    Serilog.Log.Information($"重试等待被取消：{videoUrl}");
        //                    CleanupIncompleteFile(savePath);
        //                    return false;
        //                }
        //            }
        //            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        //            {
        //                Serilog.Log.Information($"下载被取消：{videoUrl}");
        //                CleanupIncompleteFile(savePath);
        //                return false;
        //            }
        //            catch (Exception ex)
        //            {
        //                Serilog.Log.Error(ex, $"下载失败（不可重试）：{videoUrl}");
        //                CleanupIncompleteFile(savePath);
        //                return false;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        // 确保锁一定释放（无论是否发生异常）
        //        if (lockAcquired)
        //        {
        //            _downloadSemaphore.Release();
        //            Serilog.Log.Debug($"下载锁已释放，下一个任务可执行");
        //        }
        //    }
        //}

        // 辅助类：用于using语句自动释放SemaphoreSlim
        //public sealed class SemaphoreReleaser : IDisposable
        //{
        //    private readonly SemaphoreSlim _semaphore;
        //    private bool _disposed;

        //    public SemaphoreReleaser(SemaphoreSlim semaphore)
        //    {
        //        _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
        //    }

        //    public void Dispose()
        //    {
        //        if (!_disposed)
        //        {
        //            _semaphore.Release(); // 释放锁，允许下一个任务执行
        //            _disposed = true;
        //        }
        //    }
        //}


        /// <summary>
        /// 单次下载尝试（核心下载逻辑）
        /// </summary>
        private async Task<bool> TryDownloadOnceAsync(
            string videoUrl,
            string savePath,
            string cookie,
            CancellationToken cancellationToken,
            TimeSpan streamTimeout)
        {
            DateTime lastStreamActivity = DateTime.UtcNow; // 流活动时间戳

            // 确保目录存在
            var directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 清理已存在的文件（避免占用）
            CleanupIncompleteFile(savePath);

            using (var httpClient = _clientFactory.CreateClient("dy_download"))
            {
                // 配置请求头
                httpClient.DefaultRequestHeaders.Remove("Cookie");
                httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
                httpClient.Timeout = TimeSpan.FromMinutes(5); // 总请求超时

                // 发送请求
                using (var response = await httpClient.GetAsync(
                    videoUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode(); // 验证HTTP状态

                    long? totalBytes = response.Content.Headers.ContentLength;

                    // 读取流并写入文件
                    using (var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                    using (var fileStream = new FileStream(
                        savePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 8192,
                        options: FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long totalRead = 0;

                        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            // 检查流读取超时
                            if (DateTime.UtcNow - lastStreamActivity > streamTimeout)
                            {
                                throw new TimeoutException($"流读取超时（{streamTimeout.TotalSeconds}秒无数据）");
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                            totalRead += bytesRead;
                            lastStreamActivity = DateTime.UtcNow;

                            // 进度计算（可选）
                            if (totalBytes.HasValue)
                            {
                                double progress = (double)totalRead / totalBytes.Value * 100;
                                // 进度上报逻辑：OnProgressChanged(progress);
                            }
                        }

                        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false); // 确保数据写入磁盘
                    }
                }
            }

            return true;
        }


        ///// <summary>下载网络文件到指定路径</summary>
        //public async Task DownloadFileAsync(string url, string savePath)
        //{
        //    if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
        //    if (string.IsNullOrEmpty(savePath)) throw new ArgumentNullException(nameof(savePath));

        //    // 创建目录（如果不存在）
        //    var directory = Path.GetDirectoryName(savePath)!;
        //    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        //    // 下载文件
        //    using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        //    response.EnsureSuccessStatusCode(); // 非2xx状态码抛出异常

        //    using var stream = await response.Content.ReadAsStreamAsync();
        //    using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        //    await stream.CopyToAsync(fileStream);
        //}



        /// <summary>
        /// 判断异常是否可重试
        /// </summary>
        private bool IsRetryableException(Exception ex)
        {
            // 可重试的异常类型：网络错误、超时、服务器临时错误等
            return ex is HttpRequestException // HTTP请求失败（如5xx错误、连接中断）
                || ex is TimeoutException // 超时（包括流超时和请求超时）
                || ex is IOException // IO错误（如临时磁盘问题）
                || (ex is AggregateException aggEx && aggEx.InnerExceptions.Any(IsRetryableException)); // 聚合异常中包含可重试项
        }

        /// <summary>
        /// 清理不完整的文件
        /// </summary>
        private static void CleanupIncompleteFile(string savePath)
        {
            if (File.Exists(savePath))
            {
                try
                {
                    File.Delete(savePath);
                }
                catch (IOException ex)
                {
                    Serilog.Log.Warning(ex, $"清理不完整文件失败：{savePath}（可能被占用）");
                }
            }
        }

        /// <summary>
        /// 获取单个视频详情（直接从分享页面提取完整信息）
        /// </summary>
        /// <param name="awemeId">视频ID</param>
        /// <param name="cookie">用户Cookie（可选）</param>
        /// <returns>包含视频详情的对象</returns>
        public async Task<Aweme> GetVideoDetailAsync(string awemeId, string cookie)
        {
            if (string.IsNullOrWhiteSpace(awemeId))
                throw new ArgumentException("视频ID不能为空", nameof(awemeId));

            try
            {
                // 直接从分享页面获取完整视频信息
                var shareUrl = $"https://www.iesdouyin.com/share/video/{awemeId}";
                Serilog.Log.Debug($"访问分享页面获取视频信息: {shareUrl}");

                using var handler = new HttpClientHandler { AllowAutoRedirect = true };
                using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15");

                var response = await httpClient.GetAsync(shareUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Serilog.Log.Error($"访问分享页面失败: {response.StatusCode}");
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                // 验证视频ID是否匹配
                var awemeIdMatch = System.Text.RegularExpressions.Regex.Match(html, @"""aweme_id""\s*:\s*""(\d+)""");
                if (awemeIdMatch.Success && awemeIdMatch.Groups[1].Value != awemeId)
                {
                    Serilog.Log.Error($"视频ID不匹配: 请求={awemeId}, 返回={awemeIdMatch.Groups[1].Value}");
                    return null;
                }

                // 提取视频信息
                var aweme = new Aweme { AwemeId = awemeId };

                // 提取描述
                var descMatch = System.Text.RegularExpressions.Regex.Match(html, @"""desc""\s*:\s*""([^""]+)""");
                if (descMatch.Success)
                    aweme.Desc = descMatch.Groups[1].Value;

                // 提取作者信息
                var nicknameMatch = System.Text.RegularExpressions.Regex.Match(html, @"""nickname""\s*:\s*""([^""]+)""");
                if (nicknameMatch.Success)
                {
                    aweme.Author = new Author { Nickname = nicknameMatch.Groups[1].Value };
                }

                // 提取视频播放地址
                var playAddrMatch = System.Text.RegularExpressions.Regex.Match(html, @"play_addr.*?url_list.*?\[""([^""]+)""", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (playAddrMatch.Success)
                {
                    var videoUrl = playAddrMatch.Groups[1].Value.Replace("\\u002F", "/");
                    aweme.Video = new Video
                    {
                        BitRate = new List<VideoBitRate>
                        {
                            new VideoBitRate
                            {
                                Format = "mp4",
                                PlayAddr = new PlayAddr { UrlList = new List<string> { videoUrl } }
                            }
                        }
                    };
                }

                // 提取封面
                var coverMatch = System.Text.RegularExpressions.Regex.Match(html, @"""cover"".*?url_list.*?\[""([^""]+)""", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (coverMatch.Success && aweme.Video != null)
                {
                    aweme.Video.Cover = new ImageInfo { UrlList = new List<string> { coverMatch.Groups[1].Value.Replace("\\u002F", "/") } };
                }

                if (aweme.Video?.BitRate?.FirstOrDefault()?.PlayAddr?.UrlList?.FirstOrDefault() != null)
                {
                    Serilog.Log.Debug($"成功从分享页面获取视频: {aweme.Desc?.Substring(0, Math.Min(30, aweme.Desc?.Length ?? 0))}...");
                    return aweme;
                }

                Serilog.Log.Error("未能从分享页面提取视频播放地址");
                return null;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"GetVideoDetailAsync error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从视频分享页面提取作者的sec_uid
        /// 使用 iesdouyin.com/share/video 端点，该端点返回作者信息
        /// </summary>
        private async Task<string> ExtractAuthorSecUidAsync(string awemeId, string cookie)
        {
            try
            {
                // 使用移动端分享页面获取作者信息
                var shareUrl = $"https://www.iesdouyin.com/share/video/{awemeId}";
                Serilog.Log.Debug($"访问分享页面获取作者信息: {shareUrl}");

                using var handler = new HttpClientHandler { AllowAutoRedirect = true };
                using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15");

                var response = await httpClient.GetAsync(shareUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Serilog.Log.Error($"访问分享页面失败: {response.StatusCode}");
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                // 从响应中提取 sec_uid
                var match = System.Text.RegularExpressions.Regex.Match(html, @"sec_uid""\s*:\s*""(MS4[A-Za-z0-9_-]+)""");
                if (match.Success)
                {
                    var secUid = match.Groups[1].Value;
                    Serilog.Log.Debug($"成功提取作者sec_uid: {secUid.Substring(0, Math.Min(30, secUid.Length))}...");
                    return secUid;
                }

                // 备用方式：直接搜索MS4格式
                var ms4Match = System.Text.RegularExpressions.Regex.Match(html, @"MS4[A-Za-z0-9_-]{40,}");
                if (ms4Match.Success)
                {
                    Serilog.Log.Debug($"使用备用方式提取sec_uid");
                    return ms4Match.Value;
                }

                Serilog.Log.Error("未能从分享页面提取作者sec_uid");
                return null;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"ExtractAuthorSecUidAsync error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 通过作品列表API获取特定视频
        /// </summary>
        private async Task<Aweme> GetVideoByPostApiAsync(string awemeId, string secUid, string cookie)
        {
            try
            {
                using var httpClient = _clientFactory.CreateClient("dy_uper");
                httpClient.DefaultRequestHeaders.Remove("Cookie");
                httpClient.DefaultRequestHeaders.Add("Cookie", cookie);

                // 使用作品列表参数，获取更多视频以便查找目标
                var parameters = DouyinBaseParamDics.InitializeDouyinPostParams();
                parameters["sec_user_id"] = secUid;
                parameters["locate_item_id"] = awemeId;
                parameters["count"] = "20";  // 获取更多视频以便查找
                parameters["max_cursor"] = "0";

                // 获取msToken
                try
                {
                    var token = await TokenManager.GenRealMsTokenAsync();
                    parameters["msToken"] = token;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning($"获取msToken失败: {ex.Message}");
                }

                string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";
                var endPoint = BogusManager.XbModel2Endpoint($"{DouYinApi}/post", parameters, UserAgent);

                Serilog.Log.Debug($"调用作品列表API定位视频: {awemeId}");
                var response = await httpClient.GetAsync(endPoint);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<DouyinVideoInfo>(data);

                    if (result?.AwemeList != null && result.AwemeList.Any())
                    {
                        // 必须精确匹配目标视频ID
                        var targetVideo = result.AwemeList.FirstOrDefault(v => v.AwemeId == awemeId);
                        if (targetVideo != null)
                        {
                            Serilog.Log.Debug($"成功获取视频详情: {targetVideo.Desc}");
                            return targetVideo;
                        }

                        // 未找到匹配的视频，记录日志
                        var availableIds = string.Join(", ", result.AwemeList.Take(5).Select(v => v.AwemeId));
                        Serilog.Log.Error($"未在作品列表中找到目标视频 {awemeId}，可用ID: {availableIds}");
                        return null;
                    }
                    else
                    {
                        Serilog.Log.Error($"作品列表为空，响应: {data.Substring(0, Math.Min(300, data.Length))}");
                    }
                }
                else
                {
                    Serilog.Log.Error($"调用作品列表API失败: {response.StatusCode}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"GetVideoByPostApiAsync error: {ex.Message}");
                return null;
            }
        }
    }
}
