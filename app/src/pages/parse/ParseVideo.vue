<template>
  <div class="parse-page">
    <div class="parse-container">
      <!-- 输入区域 -->
      <div class="input-section">
        <a-input
          v-model:value="videoUrl"
          placeholder="粘贴抖音视频链接"
          size="large"
          allow-clear
          @pressEnter="handleSubmit"
        >
          <template #prefix>
            <LinkOutlined style="color: #999" />
          </template>
          <template #suffix>
            <a-radio-group v-model:value="downloadType" size="small" class="type-switch">
              <a-radio-button value="video">视频</a-radio-button>
              <a-radio-button value="audio">音频</a-radio-button>
              <a-radio-button value="text">转文字</a-radio-button>
            </a-radio-group>
            <a-button 
              type="primary" 
              :loading="submitting" 
              @click="handleSubmit" 
              :disabled="!videoUrl"
            >
              <DownloadOutlined />
              下载
            </a-button>
          </template>
        </a-input>
      </div>

      <!-- 标签页切换 -->
      <div class="tab-bar">
        <div class="tab-item" :class="{ active: activeTab === 'tasks' }" @click="activeTab = 'tasks'">
          当前任务 ({{ downloadStore.tasks.length }})
        </div>
        <div class="tab-item" :class="{ active: activeTab === 'history' }" @click="switchToHistory">
          历史记录 ({{ downloadStore.historyTotal }})
        </div>
      </div>

      <!-- 当前任务列表 -->
      <div class="task-list" v-if="activeTab === 'tasks' && downloadStore.tasks.length > 0">
        <div 
          class="task-card" 
          v-for="task in downloadStore.tasks" 
          :key="task.taskId"
          @click="handlePreview(task)"
          :class="{ clickable: task.status === 2 }"
        >
          <div class="task-cover" v-if="task.coverUrl">
            <img :src="task.coverUrl" alt="封面" />
            <div class="play-overlay" v-if="task.status === 2">
              <PlayCircleOutlined />
            </div>
          </div>
          <div class="task-cover placeholder" v-else>
            <CloudDownloadOutlined />
          </div>
          <div class="task-detail">
            <div class="task-title">{{ task.title || task.awemeId || '加载中...' }}</div>
            <div class="task-meta" v-if="task.author">@{{ task.author }}</div>
            <div class="task-status-row">
              <a-tag :color="getStatusColor(task.status)" size="small">
                {{ getStatusText(task.status) }}
              </a-tag>
              <span class="task-type" v-if="task.type">{{ getTypeText(task.type) }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 历史记录列表 -->
      <div class="task-list" v-else-if="activeTab === 'history' && downloadStore.history.length > 0">
        <div 
          class="task-card clickable" 
          v-for="item in downloadStore.history" 
          :key="item.id"
          @click="handleHistoryPreview(item)"
        >
          <div class="task-cover" v-if="item.coverUrl">
            <img :src="item.coverUrl" alt="封面" />
            <div class="play-overlay">
              <PlayCircleOutlined />
            </div>
          </div>
          <div class="task-cover placeholder" v-else>
            <HistoryOutlined />
          </div>
          <div class="task-detail">
            <div class="task-title">{{ item.title || item.awemeId }}</div>
            <div class="task-meta" v-if="item.author">@{{ item.author }}</div>
            <div class="task-status-row">
              <a-tag :color="getStatusColor(item.status)" size="small">
                {{ getStatusText(item.status) }}
              </a-tag>
              <span class="task-type">{{ getTypeText(item.downloadType) }}</span>
              <span class="task-time">{{ formatTime(item.downloadTime) }}</span>
            </div>
          </div>
          <div class="task-action" @click.stop="handleDeleteHistory(item.id)">
            <DeleteOutlined />
          </div>
        </div>
        <!-- 加载更多 -->
        <div class="load-more" v-if="downloadStore.history.length < downloadStore.historyTotal" @click="loadMoreHistory">
          加载更多...
        </div>
      </div>

      <!-- 空状态提示 -->
      <div class="empty-tip" v-else>
        <CloudDownloadOutlined class="empty-icon" v-if="activeTab === 'tasks'" />
        <HistoryOutlined class="empty-icon" v-else />
        <p v-if="activeTab === 'tasks'">支持抖音分享链接或视频页面链接</p>
        <p v-else>暂无下载历史</p>
      </div>
    </div>

    <!-- 预览弹窗 -->
    <a-modal
      v-model:visible="previewVisible"
      :title="previewTask?.title || '预览'"
      :footer="null"
      :width="previewType === 'text' ? 600 : 800"
      :bodyStyle="{ padding: previewType === 'text' ? '20px' : '0' }"
      @cancel="closePreview"
    >
      <!-- 视频预览 -->
      <div v-if="previewType === 'video'" class="preview-video">
        <video
          ref="videoRef"
          controls
          autoplay
          :src="mediaUrl"
          style="width: 100%; max-height: 70vh;"
        />
      </div>
      
      <!-- 音频预览 -->
      <div v-else-if="previewType === 'audio'" class="preview-audio">
        <div class="audio-cover" v-if="previewTask?.coverUrl">
          <img :src="previewTask.coverUrl" alt="封面" />
        </div>
        <audio
          ref="audioRef"
          controls
          autoplay
          :src="mediaUrl"
          style="width: 100%;"
        />
      </div>
      
      <!-- 文字预览 -->
      <div v-else-if="previewType === 'text'" class="preview-text">
        <a-spin v-if="textLoading" />
        <div v-else class="text-content">{{ textContent }}</div>
      </div>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { message } from 'ant-design-vue';
import {
  CloudDownloadOutlined,
  DownloadOutlined,
  LinkOutlined,
  PlayCircleOutlined,
  HistoryOutlined,
  DeleteOutlined,
} from '@ant-design/icons-vue';
import { useDownloadStore, DownloadTask, HistoryItem } from '@/store/downloadStore';
import http from '@/store/http';

const downloadStore = useDownloadStore();
const videoUrl = ref('');
const downloadType = ref('video');
const submitting = ref(false);
const activeTab = ref<'tasks' | 'history'>('tasks');

// 预览相关
const previewVisible = ref(false);
const previewTask = ref<DownloadTask | HistoryItem | null>(null);
const previewType = ref<'video' | 'audio' | 'text'>('video');
const mediaUrl = ref('');
const textContent = ref('');
const textLoading = ref(false);
const videoRef = ref<HTMLVideoElement | null>(null);
const audioRef = ref<HTMLAudioElement | null>(null);

const getStatusColor = (status: number) => {
  switch (status) {
    case 0: return 'blue';
    case 1: return 'orange';
    case 2: return 'green';
    case 3: return 'red';
    default: return 'default';
  }
};

const getStatusText = (status: number) => {
  switch (status) {
    case 0: return '等待中';
    case 1: return '下载中';
    case 2: return '已完成';
    case 3: return '失败';
    default: return '未知';
  }
};

const getTypeText = (type: string) => {
  switch (type) {
    case 'video': return '视频';
    case 'audio': return '音频';
    case 'text': return '文字';
    default: return '';
  }
};

const handleSubmit = async () => {
  if (!videoUrl.value.trim()) {
    message.warning('请输入视频链接');
    return;
  }

  submitting.value = true;

  try {
    await downloadStore.submitTask(videoUrl.value.trim(), downloadType.value);
    message.success('已添加到下载队列');
    videoUrl.value = '';
  } catch (error: any) {
    message.error(error.message || '提交失败');
  } finally {
    submitting.value = false;
  }
};

const handlePreview = async (task: DownloadTask) => {
  if (task.status !== 2) return; // 只预览已完成的
  
  previewTask.value = task;
  
  // 根据任务类型确定预览类型
  if (task.type === 'text') {
    previewType.value = 'text';
    textLoading.value = true;
    previewVisible.value = true;
    
    try {
      const res: any = await http.get(`/api/Parse/text/${task.taskId}`);
      textContent.value = res.data || res || '无内容';
    } catch (e) {
      textContent.value = '加载失败';
    } finally {
      textLoading.value = false;
    }
  } else {
    previewType.value = task.type === 'audio' ? 'audio' : 'video';
    mediaUrl.value = `/api/Parse/media/${task.taskId}`;
    previewVisible.value = true;
  }
};

const closePreview = () => {
  previewVisible.value = false;
  mediaUrl.value = '';
  textContent.value = '';
  if (videoRef.value) videoRef.value.pause();
  if (audioRef.value) audioRef.value.pause();
};

// 切换到历史记录
const switchToHistory = () => {
  activeTab.value = 'history';
  // 每次切换都刷新历史记录
  downloadStore.fetchHistory(1);
};

// 历史记录预览
const handleHistoryPreview = async (item: HistoryItem) => {
  if (item.status !== 2) return;
  
  previewTask.value = item;
  const previewUrl = `/api/Parse/history/preview/${item.id}`;
  
  if (item.downloadType === 'text') {
    previewType.value = 'text';
    textLoading.value = true;
    previewVisible.value = true;
    
    try {
      const response = await http.get(previewUrl, { responseType: 'text' });
      textContent.value = response.data;
    } catch (e) {
      textContent.value = '无法加载文本内容';
    } finally {
      textLoading.value = false;
    }
  } else {
    previewType.value = item.downloadType === 'audio' ? 'audio' : 'video';
    mediaUrl.value = previewUrl;
    previewVisible.value = true;
  }
};

// 删除历史记录
const handleDeleteHistory = async (id: string) => {
  await downloadStore.deleteHistory(id);
  message.success('已删除');
};

// 加载更多历史
const loadMoreHistory = () => {
  downloadStore.fetchHistory(downloadStore.historyPage + 1);
};

// 格式化时间
const formatTime = (time: string) => {
  if (!time) return '';
  const date = new Date(time);
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const day = date.getDate().toString().padStart(2, '0');
  const hour = date.getHours().toString().padStart(2, '0');
  const min = date.getMinutes().toString().padStart(2, '0');
  return `${month}-${day} ${hour}:${min}`;
};

onMounted(() => {
  downloadStore.fetchTasks();
  downloadStore.fetchHistory(); // 预加载历史数量
});

onUnmounted(() => {
  closePreview();
});
</script>

<style scoped lang="less">
.parse-page {
  min-height: 60vh;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 40px 24px;
}

.parse-container {
  width: 100%;
  max-width: 600px;
}

.input-section {
  :deep(.ant-input-affix-wrapper) {
    padding: 8px 12px;
    border-radius: 8px;
    
    .ant-input {
      font-size: 15px;
    }
    
    .ant-input-suffix {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    
    .ant-btn {
      margin: -4px -8px -4px 0;
      height: 36px;
      border-radius: 6px;
    }
  }

  .type-switch {
    :deep(.ant-radio-button-wrapper) {
      padding: 0 8px;
      height: 28px;
      line-height: 26px;
      font-size: 12px;
    }
  }
}

.tab-bar {
  display: flex;
  gap: 4px;
  margin-top: 20px;
  padding: 4px;
  background: rgba(255, 255, 255, 0.04);
  border-radius: 8px;

  .tab-item {
    flex: 1;
    text-align: center;
    padding: 8px 12px;
    border-radius: 6px;
    font-size: 13px;
    cursor: pointer;
    transition: all 0.2s;
    color: rgba(255, 255, 255, 0.5);

    &:hover {
      color: rgba(255, 255, 255, 0.7);
    }

    &.active {
      background: rgba(255, 255, 255, 0.1);
      color: #fff;
    }
  }
}

.task-list {
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.task-card {
  display: flex;
  gap: 12px;
  padding: 12px;
  background: rgba(255, 255, 255, 0.04);
  border-radius: 10px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  transition: all 0.2s;

  &.clickable {
    cursor: pointer;
    &:hover {
      background: rgba(255, 255, 255, 0.08);
      border-color: rgba(255, 255, 255, 0.15);
    }
  }
}

.task-cover {
  width: 60px;
  height: 80px;
  flex-shrink: 0;
  border-radius: 6px;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.05);
  position: relative;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .play-overlay {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 0, 0, 0.4);
    font-size: 24px;
    color: #fff;
    opacity: 0;
    transition: opacity 0.2s;
  }

  &:hover .play-overlay {
    opacity: 1;
  }

  &.placeholder {
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 24px;
    color: rgba(255, 255, 255, 0.2);
  }
}

.task-detail {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.task-title {
  font-size: 13px;
  font-weight: 500;
  line-height: 1.4;
  margin-bottom: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
}

.task-meta {
  color: rgba(255, 255, 255, 0.45);
  font-size: 12px;
  margin-bottom: 6px;
}

.task-status-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.task-type {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.35);
}

.task-time {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.3);
  margin-left: auto;
}

.task-action {
  display: flex;
  align-items: center;
  padding: 0 8px;
  color: rgba(255, 255, 255, 0.3);
  cursor: pointer;
  transition: color 0.2s;

  &:hover {
    color: #ff4d4f;
  }
}

.load-more {
  text-align: center;
  padding: 12px;
  color: rgba(255, 255, 255, 0.4);
  cursor: pointer;
  font-size: 13px;

  &:hover {
    color: rgba(255, 255, 255, 0.6);
  }
}

.empty-tip {
  margin-top: 80px;
  text-align: center;
  color: rgba(255, 255, 255, 0.25);

  .empty-icon {
    font-size: 48px;
    margin-bottom: 16px;
  }

  p {
    font-size: 14px;
    margin: 0;
  }
}

// 预览样式
.preview-video {
  background: #000;
  video {
    display: block;
  }
}

.preview-audio {
  padding: 20px;
  text-align: center;
  
  .audio-cover {
    width: 200px;
    height: 200px;
    margin: 0 auto 20px;
    border-radius: 12px;
    overflow: hidden;
    
    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  }
}

.preview-text {
  .text-content {
    white-space: pre-wrap;
    line-height: 1.8;
    font-size: 14px;
    max-height: 60vh;
    overflow-y: auto;
  }
}
</style>
