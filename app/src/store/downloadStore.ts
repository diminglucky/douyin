import { defineStore } from 'pinia';
import http from '@/store/http';

export interface DownloadTask {
  taskId: string;
  url: string;
  awemeId: string;
  title: string;
  author: string;
  coverUrl: string;
  filePath: string;
  status: number; // 0-等待 1-下载中 2-完成 3-失败
  message: string;
  createTime: string;
  completeTime: string | null;
  type: string; // video-视频 audio-音频
}

export interface HistoryItem {
  id: string;
  awemeId: string;
  title: string;
  author: string;
  coverUrl: string;
  downloadType: string;
  filePath: string;
  status: number;
  message: string;
  downloadTime: string;
}

export const useDownloadStore = defineStore('download', {
  state: () => ({
    tasks: [] as DownloadTask[],
    polling: false,
    history: [] as HistoryItem[],
    historyTotal: 0,
    historyPage: 1,
  }),

  getters: {
    // 获取进行中的任务
    activeTasks: (state) => state.tasks.filter(t => t.status === 0 || t.status === 1),
    // 获取已完成的任务
    completedTasks: (state) => state.tasks.filter(t => t.status === 2 || t.status === 3),
    // 是否有正在进行的任务
    hasActiveTask: (state) => state.tasks.some(t => t.status === 0 || t.status === 1),
  },

  actions: {
    // 提交下载任务
    async submitTask(url: string, type: string = 'video') {
      try {
        const res: any = await http.post('/api/Parse/submit', { url, type });
        // http拦截器已经处理了code，这里res就是完整响应
        const task = (res.data || res) as DownloadTask;
        if (task && task.taskId) {
          const existingIndex = this.tasks.findIndex(t => t.taskId === task.taskId);
          if (existingIndex === -1) {
            this.tasks.unshift(task);
          }
          // 开始轮询
          this.startPolling();
          return task;
        }
        throw new Error('返回数据格式错误');
      } catch (error: any) {
        console.error('提交任务失败:', error);
        throw new Error(error.message || error.error || '提交失败');
      }
    },

    // 获取所有任务
    async fetchTasks() {
      try {
        const res: any = await http.get('/api/Parse/tasks');
        // http拦截器处理后，res就是完整响应
        const tasks = res.data || res;
        if (Array.isArray(tasks)) {
          this.tasks = tasks;
        }
      } catch (error) {
        console.error('获取任务列表失败', error);
      }
    },

    // 开始轮询任务状态
    startPolling() {
      if (this.polling) return;
      this.polling = true;
      this.pollTasks();
    },

    // 轮询任务
    async pollTasks() {
      if (!this.polling) return;

      try {
        // 记录之前的完成任务数
        const prevCompleted = this.tasks.filter(t => t.status === 2 || t.status === 3).length;
        
        await this.fetchTasks();
        
        // 检查是否有新完成的任务
        const nowCompleted = this.tasks.filter(t => t.status === 2 || t.status === 3).length;
        if (nowCompleted > prevCompleted) {
          // 有新任务完成，刷新历史记录
          this.fetchHistory(1);
        }
        
        // 如果还有进行中的任务，继续轮询
        if (this.hasActiveTask) {
          setTimeout(() => this.pollTasks(), 1500);
        } else {
          this.polling = false;
        }
      } catch (error) {
        this.polling = false;
      }
    },

    // 停止轮询
    stopPolling() {
      this.polling = false;
    },

    // 清空已完成任务
    clearCompleted() {
      this.tasks = this.tasks.filter(t => t.status === 0 || t.status === 1);
    },

    // 获取历史记录
    async fetchHistory(page: number = 1, pageSize: number = 20) {
      try {
        const res: any = await http.get(`/api/Parse/history?page=${page}&pageSize=${pageSize}`);
        const data = res.data || res;
        if (data) {
          this.history = data.list || [];
          this.historyTotal = data.total || 0;
          this.historyPage = page;
        }
      } catch (error) {
        console.error('获取历史记录失败', error);
      }
    },

    // 删除历史记录
    async deleteHistory(id: string) {
      try {
        await http.delete(`/api/Parse/history/${id}`);
        this.history = this.history.filter(h => h.id !== id);
        this.historyTotal--;
      } catch (error) {
        console.error('删除历史记录失败', error);
      }
    },

    // 清空历史记录
    async clearHistory() {
      try {
        await http.delete('/api/Parse/history/clear');
        this.history = [];
        this.historyTotal = 0;
      } catch (error) {
        console.error('清空历史记录失败', error);
      }
    },
  },
});
