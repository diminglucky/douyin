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

      <!-- 任务列表 -->
      <div class="task-list" v-if="downloadStore.tasks.length > 0">
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

      <!-- 空状态提示 -->
      <div class="empty-tip" v-else>
        <CloudDownloadOutlined class="empty-icon" />
        <p>支持抖音分享链接或视频页面链接</p>
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
} from '@ant-design/icons-vue';
import { useDownloadStore, DownloadTask } from '@/store/downloadStore';
import http from '@/store/http';

const downloadStore = useDownloadStore();
const videoUrl = ref('');
const downloadType = ref('video');
const submitting = ref(false);

// 预览相关
const previewVisible = ref(false);
const previewTask = ref<DownloadTask | null>(null);
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

onMounted(() => {
  downloadStore.fetchTasks();
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

.task-list {
  margin-top: 24px;
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
