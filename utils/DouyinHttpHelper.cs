using System.Net.Http;
using System.Threading.Tasks;

namespace dy.sync.lib
{
    /// <summary>
    /// 开源实现的 DouyinHttpHelper
    /// 替代原闭源 dy.sync.lib.dll 中的同名类
    /// </summary>
    public static class DouyinHttpHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// 腾讯云镜像仓库API地址
        /// 用于获取Docker镜像版本信息
        /// </summary>
        private const string TencentCloudImageApi = "https://ccr.ccs.tencentyun.com/v2/jianzhichu/dysync/tags/list";

        /// <summary>
        /// 获取腾讯云镜像仓库的镜像标签信息
        /// </summary>
        /// <param name="tagName">当前标签名（用于比较）</param>
        /// <returns>HTTP响应</returns>
        public static async Task<HttpResponseMessage> GetTenImage(string tagName)
        {
            try
            {
                // 构建请求 - 使用Docker Registry V2 API
                var request = new HttpRequestMessage(HttpMethod.Get, TencentCloudImageApi);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("User-Agent", "dysync-version-checker/1.0");

                var response = await _httpClient.GetAsync(TencentCloudImageApi);
                
                // 如果腾讯云API需要认证，我们返回一个模拟的成功响应
                // 包含当前版本信息（本地版本检查模式）
                if (!response.IsSuccessStatusCode)
                {
                    return CreateLocalVersionResponse(tagName);
                }

                return response;
            }
            catch (Exception ex)
            {
                // 网络错误时，返回本地版本信息
                Console.WriteLine($"[DouyinHttpHelper] 版本检查失败: {ex.Message}");
                return CreateLocalVersionResponse(tagName);
            }
        }

        /// <summary>
        /// 创建本地版本响应（当无法访问远程API时使用）
        /// </summary>
        private static HttpResponseMessage CreateLocalVersionResponse(string currentTag)
        {
            var localResponse = new
            {
                Response = new
                {
                    Data = new
                    {
                        RepoName = "jianzhichu/dysync",
                        Server = "local",
                        TagCount = 1,
                        TagInfo = new[]
                        {
                            new
                            {
                                Architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                                Author = "local",
                                CreationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                DockerVersion = "local",
                                DurationDays = "0",
                                Id = 1,
                                ImageId = "local",
                                Kind = "local",
                                OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                                PushTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                Size = "0",
                                SizeByte = 0,
                                TagId = "local",
                                TagName = currentTag ?? "unknown",
                                UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            }
                        }
                    },
                    RequestId = Guid.NewGuid().ToString()
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(localResponse);
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            return response;
        }
    }
}
