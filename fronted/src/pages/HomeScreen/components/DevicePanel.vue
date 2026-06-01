<template>
  <div class="device-panel" :class="{ 'panel-normal': status === 'normal', 'panel-warning': status === 'warning', 'panel-error': status === 'error', 'panel-standby': status === 'standby' }">
    <div class="device-svg-container">
      <img :src="svgPath" :alt="deviceName" class="device-svg" />
    </div>
    
    <div class="device-info">
      <div class="device-header">
        <span class="device-name">{{ deviceName }}</span>
        <span class="device-tag" :class="statusClass">{{ statusText }}</span>
      </div>
      
      <div class="device-tagcode">{{ tagCode }}</div>
      
      <div class="params-list">
        <div 
          v-for="(param, index) in params" 
          :key="index" 
          class="param-item"
        >
          <div class="param-label">{{ param.label }}</div>
          <div class="param-value">
            {{ param.value }} <span class="param-unit">{{ param.unit }}</span>
          </div>
        </div>
      </div>
      
      <div v-if="showActions" class="device-actions">
        <slot name="actions"></slot>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Param {
  label: string
  value: string | number
  unit: string
}

interface Props {
  svgName: string
  deviceName: string
  tagCode: string
  params: Param[]
  status?: 'normal' | 'warning' | 'error' | 'standby'
  svgWidth?: string
  svgHeight?: string
  showActions?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  status: 'normal',
  svgWidth: '180px',
  svgHeight: '180px',
  showActions: false
})

// 导入所有 SVG 文件
const svgModules = import.meta.glob('@/assets/icons/svg/*.svg', { eager: true }) as Record<string, any>

const svgPath = computed(() => {
  const found = Object.keys(svgModules).find(key => key.includes(`${props.svgName}.svg`))
  if (found && svgModules[found]?.default) {
    return svgModules[found].default
  }
  // 如果找不到，返回原始路径
  return new URL(`@/assets/icons/svg/${props.svgName}.svg`, import.meta.url).href
})

const statusClass = computed(() => {
  return {
    'status-normal': props.status === 'normal',
    'status-warning': props.status === 'warning',
    'status-error': props.status === 'error',
    'status-standby': props.status === 'standby'
  }
})

const statusText = computed(() => {
  const statusMap = {
    normal: '正常',
    warning: '警告',
    error: '故障',
    standby: '备用'
  }
  return statusMap[props.status]
})
</script>

<style lang="scss" scoped>
.device-panel {
  display: flex;
  gap: 20px;
  background: rgba(30, 60, 100, 0.4);
  border: 1px solid rgba(100, 150, 255, 0.3);
  border-radius: 8px;
  padding: 20px;
  transition: all 0.3s;

  &:hover {
    border-color: rgba(100, 150, 255, 0.6);
    box-shadow: 0 0 20px rgba(100, 150, 255, 0.2);
  }

  &.panel-warning {
    border-color: rgba(255, 206, 86, 0.5);
  }

  &.panel-error {
    border-color: rgba(246, 173, 85, 0.5);
  }

  &.panel-standby {
    border-color: rgba(150, 150, 150, 0.3);
  }
}

.device-svg-container {
  flex: 0 0 180px;
  width: 180px;
  height: 180px;
  display: flex;
  align-items: center;
  justify-content: center;

  .device-svg {
    width: 100%;
    height: 100%;
    object-fit: contain;
  }
}

.device-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.device-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.device-name {
  color: #ffffff;
  font-size: 16px;
  font-weight: 600;
}

.device-tagcode {
  color: rgba(255, 255, 255, 0.5);
  font-size: 12px;
  font-family: 'Courier New', monospace;
  margin-bottom: 12px;
}

.device-tag {
  padding: 4px 12px;
  border-radius: 4px;
  font-size: 12px;

  &.status-normal {
    background: rgba(74, 204, 74, 0.2);
    color: #4acc4a;
    border: 1px solid #4acc4a;
  }

  &.status-warning {
    background: rgba(255, 206, 86, 0.2);
    color: #ffce56;
    border: 1px solid #ffce56;
  }

  &.status-error {
    background: rgba(246, 173, 85, 0.2);
    color: #f6ad55;
    border: 1px solid #f6ad55;
  }

  &.status-standby {
    background: rgba(150, 150, 150, 0.2);
    color: #999999;
    border: 1px solid #999999;
  }
}

.params-list {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
  flex: 1;

  .param-item {
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 8px 12px;
    background: rgba(20, 40, 80, 0.2);
    border-radius: 4px;

    .param-label {
      color: rgba(255, 255, 255, 0.6);
      font-size: 11px;
      white-space: nowrap;
    }

    .param-value {
      color: #ffffff;
      font-size: 14px;
      font-weight: 500;

      .param-unit {
        font-size: 11px;
        color: rgba(255, 255, 255, 0.4);
        margin-left: 2px;
      }
    }
  }
}

.device-actions {
  margin-top: 12px;
  display: flex;
  gap: 10px;
}
</style>
