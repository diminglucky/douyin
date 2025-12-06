using ClockSnowFlake;
using dy.net.model;
using SqlSugar;

namespace dy.net.service
{
    /// <summary>
    /// 单视频解析下载历史记录服务
    /// </summary>
    public class ParseHistoryService
    {
        private readonly ISqlSugarClient _db;

        public ParseHistoryService(ISqlSugarClient sqlSugarClient)
        {
            _db = sqlSugarClient;
            // 自动建表
            _db.CodeFirst.InitTables<ParseDownloadHistory>();
        }

        /// <summary>
        /// 添加下载记录
        /// </summary>
        public async Task<ParseDownloadHistory> AddAsync(string awemeId, string title, string author, 
            string coverUrl, string downloadType, string filePath, int status, string message)
        {
            var history = new ParseDownloadHistory
            {
                Id = IdGener.GetLong().ToString(),
                AwemeId = awemeId,
                Title = title,
                Author = author,
                CoverUrl = coverUrl,
                DownloadType = downloadType,
                FilePath = filePath,
                Status = status,
                Message = message,
                DownloadTime = DateTime.Now
            };

            await _db.Insertable(history).ExecuteCommandAsync();
            return history;
        }

        /// <summary>
        /// 获取下载历史列表（分页）
        /// </summary>
        public async Task<List<ParseDownloadHistory>> GetListAsync(int page = 1, int pageSize = 20)
        {
            return await _db.Queryable<ParseDownloadHistory>()
                .OrderByDescending(x => x.DownloadTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// 获取总数
        /// </summary>
        public async Task<int> GetCountAsync()
        {
            return await _db.Queryable<ParseDownloadHistory>().CountAsync();
        }

        /// <summary>
        /// 根据ID获取记录
        /// </summary>
        public async Task<ParseDownloadHistory> GetByIdAsync(string id)
        {
            return await _db.Queryable<ParseDownloadHistory>()
                .FirstAsync(x => x.Id == id);
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            return await _db.Deleteable<ParseDownloadHistory>()
                .Where(x => x.Id == id)
                .ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// 清空所有记录
        /// </summary>
        public async Task<int> ClearAllAsync()
        {
            return await _db.Deleteable<ParseDownloadHistory>().ExecuteCommandAsync();
        }
    }
}
