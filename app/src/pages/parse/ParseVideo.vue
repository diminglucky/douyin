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
          @pressEnter="handleDownload"
        >
          <template #prefix>
            <LinkOutlined style="color: #999" />
          </template>
          <template #suffix>
            <a-button 
              type="primary" 
              :loading="downloading" 
              @click="handleDownload" 
              :disabled="!videoUrl"
            >
              <DownloadOutlined />
              下载
            </a-button>
          </template>
        </a-input>
      </div>

      <!-- 下载结果 -->
      <div class="result-section" v-if="videoInfo">
        <div class="video-card">
          <div class="video-cover" v-if="videoInfo.coverUrl">
            <img :src="videoInfo.coverUrl" alt="封面" />
          </div>
          <div class="video-detail">
            <div class="video-title">{{ videoInfo.title || '无标题' }}</div>
            <div class="video-meta">
              <span>@{{ videoInfo.author || '未知' }}</span>
            </div>
            <a-tag :color="statusColor" class="status-tag">{{ statusText }}</a-tag>
          </div>
        </div>
      </div>

      <!-- 空状态提示 -->
      <div class="empty-tip" v-else>
        <CloudDownloadOutlined class="empty-icon" />
        <p>支持抖音分享链接或视频页面链接</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { message } from 'ant-design-vue';
import {
  CloudDownloadOutlined,
  DownloadOutlined,
  LinkOutlined,
} from '@ant-design/icons-vue';
import http from '@/store/http';

interface VideoInfo {
  awemeId: string;
  title: string;
  author: string;
  authorId: string;
  coverUrl: string;
  duration: number;
  isImagePost: boolean;
  downloadStatus: number; // 0-未下载 1-下载中 2-已完成 3-失败
  message: string;
}

const videoUrl = ref('');
const downloading = ref(false);
const videoInfo = ref<VideoInfo | null>(null);

const statusColor = computed(() => {
  if (!videoInfo.value) return 'default';
  switch (videoInfo.value.downloadStatus) {
    case 0: return 'blue';
    case 1: return 'orange';
    case 2: return 'green';
    case 3: return 'red';
    default: return 'default';
  }
});

const statusText = computed(() => {
  if (!videoInfo.value) return '';
  switch (videoInfo.value.downloadStatus) {
    case 0: return '待下载';
    case 1: return '下载中';
    case 2: return '已完成';
    case 3: return '失败';
    default: return '未知';
  }
});

const handleDownload = async () => {
  if (!videoUrl.value.trim()) {
    message.warning('请输入视频链接');
    return;
  }

  downloading.value = true;
  videoInfo.value = null;

  try {
    const res: any = await http.post('/api/Parse/download', { url: videoUrl.value.trim() });
    if (res.code === 0 && res.data) {
      videoInfo.value = res.data;
      message.success('下载成功');
    } else {
      videoInfo.value = res.data;
      message.error(res.error || '下载失败');
    }
  } catch (error: any) {
    message.error(error.message || '下载请求失败');
  } finally {
    downloading.value = false;
  }
};

</script>

<style scoped lang="less">
.parse-page {
  min-height: 60vh;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 60px 24px;
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
    
    .ant-btn {
      margin: -4px -8px -4px 8px;
      height: 36px;
      border-radius: 6px;
    }
  }
}

.result-section {
  margin-top: 32px;
}

.video-card {
  display: flex;
  gap: 16px;
  padding: 16px;
  background: var(--bg-color-secondary, rgba(255, 255, 255, 0.04));
  border-radius: 12px;
  border: 1px solid var(--border-color, rgba(255, 255, 255, 0.08));
}

.video-cover {
  width: 100px;
  height: 130px;
  flex-shrink: 0;
  border-radius: 8px;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.05);

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }
}

.video-detail {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.video-title {
  font-size: 14px;
  font-weight: 500;
  line-height: 1.5;
  margin-bottom: 8px;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
}

.video-meta {
  color: rgba(255, 255, 255, 0.45);
  font-size: 13px;
  margin-bottom: 12px;
}

.status-tag {
  width: fit-content;
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
</style>
