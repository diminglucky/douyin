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
}

export const useDownloadStore = defineStore('download', {
  state: () => ({
    tasks: [] as DownloadTask[],
    polling: false,
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
    async submitTask(url: string) {
      try {
        const res: any = await http.post('/api/Parse/submit', { url });
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
        await this.fetchTasks();
        
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
  },
});
