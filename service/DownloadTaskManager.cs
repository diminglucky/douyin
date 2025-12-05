using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace dy.net.service
{
    /// <summary>
    /// 下载任务状态
    /// </summary>
    public enum DownloadTaskStatus
    {
        Pending = 0,    // 等待中
        Running = 1,    // 下载中
        Completed = 2,  // 已完成
        Failed = 3      // 失败
    }

    /// <summary>
    /// 下载任务信息
    /// </summary>
    public class DownloadTask
    {
        public string TaskId { get; set; }
        public string Url { get; set; }
        public string AwemeId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string CoverUrl { get; set; }
        public string FilePath { get; set; }
        public DownloadTaskStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? CompleteTime { get; set; }
        /// <summary>
        /// 下载类型：video-视频 audio-音频
        /// </summary>
        public string Type { get; set; } = "video";
    }

    /// <summary>
    /// 下载任务管理器（单例）
    /// </summary>
    public class DownloadTaskManager
    {
        private static readonly Lazy<DownloadTaskManager> _instance = 
            new Lazy<DownloadTaskManager>(() => new DownloadTaskManager());
        
        public static DownloadTaskManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, DownloadTask> _tasks = new();
        private readonly ConcurrentQueue<string> _taskQueue = new();
        private bool _isProcessing = false;
        private readonly object _lock = new();

        private DownloadTaskManager() { }

        /// <summary>
        /// 添加下载任务
        /// </summary>
        public string AddTask(string url, string awemeId, string type = "video")
        {
            var taskId = Guid.NewGuid().ToString("N")[..8];
            var task = new DownloadTask
            {
                TaskId = taskId,
                Url = url,
                AwemeId = awemeId,
                Type = type ?? "video",
                Status = DownloadTaskStatus.Pending,
                Message = "等待下载",
                CreateTime = DateTime.Now
            };

            _tasks[taskId] = task;
            _taskQueue.Enqueue(taskId);

            Serilog.Log.Debug($"添加下载任务: {taskId}, awemeId: {awemeId}, type: {type}");

            return taskId;
        }

        /// <summary>
        /// 获取任务状态
        /// </summary>
        public DownloadTask GetTask(string taskId)
        {
            _tasks.TryGetValue(taskId, out var task);
            return task;
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public IEnumerable<DownloadTask> GetAllTasks()
        {
            return _tasks.Values.OrderByDescending(t => t.CreateTime).Take(20);
        }

        /// <summary>
        /// 更新任务状态
        /// </summary>
        public void UpdateTask(string taskId, Action<DownloadTask> updateAction)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                updateAction(task);
            }
        }

        /// <summary>
        /// 获取下一个待处理任务
        /// </summary>
        public string DequeueTask()
        {
            return _taskQueue.TryDequeue(out var taskId) ? taskId : null;
        }

        /// <summary>
        /// 清理已完成的旧任务（保留最近20个）
        /// </summary>
        public void CleanupOldTasks()
        {
            var oldTasks = _tasks.Values
                .Where(t => t.Status == DownloadTaskStatus.Completed || t.Status == DownloadTaskStatus.Failed)
                .OrderByDescending(t => t.CompleteTime)
                .Skip(20)
                .ToList();

            foreach (var task in oldTasks)
            {
                _tasks.TryRemove(task.TaskId, out _);
            }
        }

        /// <summary>
        /// 检查是否正在处理
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set => _isProcessing = value;
        }
    }
}
