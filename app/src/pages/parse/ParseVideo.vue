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
        >
          <div class="task-cover" v-if="task.coverUrl">
            <img :src="task.coverUrl" alt="封面" />
          </div>
          <div class="task-cover placeholder" v-else>
            <CloudDownloadOutlined />
          </div>
          <div class="task-detail">
            <div class="task-title">{{ task.title || task.awemeId || '加载中...' }}</div>
            <div class="task-meta" v-if="task.author">@{{ task.author }}</div>
            <a-tag :color="getStatusColor(task.status)" size="small">
              {{ getStatusText(task.status) }}
            </a-tag>
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
import { ref, onMounted, onUnmounted } from 'vue';
import { message } from 'ant-design-vue';
import {
  CloudDownloadOutlined,
  DownloadOutlined,
  LinkOutlined,
} from '@ant-design/icons-vue';
import { useDownloadStore } from '@/store/downloadStore';

const downloadStore = useDownloadStore();
const videoUrl = ref('');
const submitting = ref(false);

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

const handleSubmit = async () => {
  if (!videoUrl.value.trim()) {
    message.warning('请输入视频链接');
    return;
  }

  submitting.value = true;

  try {
    await downloadStore.submitTask(videoUrl.value.trim());
    message.success('已添加到下载队列');
    videoUrl.value = '';
  } catch (error: any) {
    message.error(error.message || '提交失败');
  } finally {
    submitting.value = false;
  }
};

onMounted(() => {
  // 加载已有任务
  downloadStore.fetchTasks();
});

onUnmounted(() => {
  // 组件销毁时不停止轮询，保持后台运行
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
    
    .ant-btn {
      margin: -4px -8px -4px 8px;
      height: 36px;
      border-radius: 6px;
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
}

.task-cover {
  width: 60px;
  height: 80px;
  flex-shrink: 0;
  border-radius: 6px;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.05);

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
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
