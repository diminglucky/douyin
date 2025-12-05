namespace dy.net.dto
{
    /// <summary>
    /// 解析视频请求
    /// </summary>
    public class ParseVideoRequest
    {
        /// <summary>
        /// 抖音视频链接（支持分享链接和直接链接）
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 保存路径（可选，默认使用系统配置路径）
        /// </summary>
        public string SavePath { get; set; }

        /// <summary>
        /// 下载类型：video-视频 audio-音频（默认video）
        /// </summary>
        public string Type { get; set; } = "video";
    }

    /// <summary>
    /// 解析视频响应
    /// </summary>
    public class ParseVideoResponse
    {
        /// <summary>
        /// 视频ID
        /// </summary>
        public string AwemeId { get; set; }

        /// <summary>
        /// 视频标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 作者昵称
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// 作者ID
        /// </summary>
        public string AuthorId { get; set; }

        /// <summary>
        /// 封面地址
        /// </summary>
        public string CoverUrl { get; set; }

        /// <summary>
        /// 视频时长（秒）
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// 是否为图文
        /// </summary>
        public bool IsImagePost { get; set; }

        /// <summary>
        /// 下载状态：0-未下载 1-下载中 2-已完成 3-失败
        /// </summary>
        public int DownloadStatus { get; set; }

        /// <summary>
        /// 下载进度或错误信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 视频下载地址（内部使用）
        /// </summary>
        public string VideoUrl { get; set; }

        /// <summary>
        /// 音频下载地址（内部使用）
        /// </summary>
        public string AudioUrl { get; set; }

        /// <summary>
        /// 下载后的文件路径
        /// </summary>
        public string FilePath { get; set; }
    }
}
