<template>
  <div class="tank-card">
    <!-- 储罐图形 -->
    <div class="tank-visual">
      <svg width="100%" height="100%" viewBox="0 0 200 150" class="tank-svg">
        <!-- 储罐主体 -->
        <rect x="50" y="20" width="100" height="110" fill="#4a5568" stroke="#718096" stroke-width="2" rx="5" />
        <!-- 顶部椭圆 -->
        <ellipse cx="100" cy="20" rx="50" ry="15" fill="#4a5568" stroke="#718096" stroke-width="2" />
        <!-- 底部椭圆 -->
        <ellipse cx="100" cy="130" rx="50" ry="15" fill="#2d3748" stroke="#718096" stroke-width="2" />
        <!-- 液位 -->
        <rect
          x="50"
          :y="130 - (liquidLevel / 100 * 110)"
          width="100"
          :height="liquidLevel / 100 * 110"
          fill="#48bb78"
          stroke="#38a169"
          stroke-width="1"
        />
        <!-- 液位顶部椭圆 -->
        <ellipse
          v-if="liquidLevel > 0"
          cx="100"
          :cy="130 - (liquidLevel / 100 * 110)"
          rx="50"
          ry="15"
          fill="#48bb78"
          stroke="#38a169"
          stroke-width="1"
        />
        <!-- 状态指示点（正常运行时有绿色小圆点） -->
        <circle
          v-if="status === '正常运行'"
          cx="160"
          cy="110"
          r="6"
          fill="#48bb78"
        />
      </svg>
    </div>

    <!-- 数据看板 -->
    <div class="tank-info-panel">
      <div class="info-header">
        <div class="tank-id">{{ tankId }} {{ tankName }}</div>
      </div>

      <div class="info-content">
        <!-- 状态 -->
        <div class="info-row">
          <span class="status-indicator" :class="statusClass">
            ● {{ status }}
          </span>
        </div>

        <!-- 总容量 -->
        <div class="info-row">
          <span class="label">总容量：</span>
          <span class="value">{{ totalCapacity }}</span>
        </div>

        <!-- 当前液位 -->
        <div class="info-row">
          <span class="label">当前液位：</span>
          <span class="value">{{ liquidLevel }}%</span>
        </div>

        <!-- 液位超限警告 -->
        <div v-if="hasLevelAlert" class="info-row alert-row">
          <span class="alert-indicator">▲ 液位超限</span>
        </div>

        <!-- 温度 -->
        <div class="info-row">
          <span class="label">温度：</span>
          <span class="value">{{ temperature }}</span>
        </div>
      </div>

      <!-- 详情按钮 -->
      <div class="info-footer">
        <el-button type="primary" link class="detail-btn" @click="handleDetail">详情</el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

type TankStatus = '正常运行' | '故障' | '停机' | '备用'

const props = defineProps<{
  tankId: string
  tankName: string
  status: TankStatus
  totalCapacity?: string
  liquidLevel: number
  temperature?: string
  hasLevelAlert?: boolean
}>()

const emit = defineEmits<{
  detail: [data: { tankId: string; tankName: string }]
}>()

const statusClass = computed(() => {
  const statusMap: Record<TankStatus, string> = {
    正常运行: 'status-normal',
    故障: 'status-fault',
    停机: 'status-shutdown',
    备用: 'status-standby'
  }
  return statusMap[props.status] || 'status-normal'
})

const handleDetail = () => {
  emit('detail', {
    tankId: props.tankId,
    tankName: props.tankName
  })
}
</script>

<style lang="scss" scoped>
.tank-card {
  display: flex;
  flex-direction: row;
  align-items: center;
  background: linear-gradient(135deg, #1a202c 0%, #2d3748 100%);
  border: 1px solid #4a5568;
  border-radius: 6px;
  padding: 1.5%;
  height: 100%;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
  transition: transform 0.2s, box-shadow 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.4);
  }
}

.tank-visual {
  flex: 0 0 35%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5%;
}

.tank-svg {
  max-width: 100%;
  max-height: 100%;
}

.tank-info-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 1.5%;
  color: #e2e8f0;
}

.info-header {
  margin-bottom: 2%;

  .tank-id {
    font-size: 1.1em;
    font-weight: bold;
    color: #cbd5e0;
  }
}

.info-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 1.5%;
}

.info-row {
  display: flex;
  align-items: center;
  font-size: 1em;

  .label {
    color: #a0aec0;
    margin-right: 1%;
  }

  .value {
    color: #e2e8f0;
    font-weight: 500;
  }
}

.status-indicator {
  font-size: 1.05em;
  font-weight: 500;

  &.status-normal {
    color: #48bb78;
  }

  &.status-fault {
    color: #f56565;
  }

  &.status-shutdown {
    color: #a0aec0;
  }

  &.status-standby {
    color: #4299e1;
  }
}

.alert-row {
  margin-top: 1%;

  .alert-indicator {
    color: #f6ad55;
    font-size: 1em;
    font-weight: 500;
  }
}

.info-footer {
  margin-top: 2%;
  padding-top: 1.5%;
  text-align: right;

  .detail-btn {
    color: #4299e1;
    font-size: 0.95em;

    &:hover {
      color: #63b3ed;
    }
  }
}
</style>
