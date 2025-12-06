using SqlSugar;

namespace dy.net.model
{
    /// <summary>
    /// 单视频解析下载历史记录
    /// </summary>
    [SugarTable(TableName = "dy_parse_history")]
    public class ParseDownloadHistory
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }

        /// <summary>
        /// 视频ID (aweme_id)
        /// </summary>
        [SugarColumn(Length = 100)]
        public string AwemeId { get; set; }

        /// <summary>
        /// 视频标题
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Title { get; set; }

        /// <summary>
        /// 作者昵称
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string Author { get; set; }

        /// <summary>
        /// 封面URL
        /// </summary>
        [SugarColumn(Length = 1000, IsNullable = true)]
        public string CoverUrl { get; set; }

        /// <summary>
        /// 下载类型：video, audio, text
        /// </summary>
        [SugarColumn(Length = 20)]
        public string DownloadType { get; set; }

        /// <summary>
        /// 文件保存路径
        /// </summary>
        [SugarColumn(Length = 1000, IsNullable = true)]
        public string FilePath { get; set; }

        /// <summary>
        /// 下载状态：0-等待 1-下载中 2-完成 3-失败
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 状态消息
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Message { get; set; }

        /// <summary>
        /// 下载时间
        /// </summary>
        public DateTime DownloadTime { get; set; }
    }
}
