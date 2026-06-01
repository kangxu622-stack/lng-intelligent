<template>
  <div class="tank-monitor-container">
    <!-- 顶部警告消息 -->
    <div class="alert-bar" v-if="alerts.length > 0">
      <div
        v-for="(alert, index) in alerts"
        :key="index"
        class="alert-item"
        :class="alert.type"
      >
        <span class="alert-icon">{{ alert.icon }}</span>
        <span>{{ alert.message }}</span>
      </div>
    </div>

    <!-- 储罐网格 -->
    <div class="tank-grid">
      <TankCard
        v-for="tank in tanks"
        :key="tank.id"
        :tank-id="tank.id"
        :tank-name="tank.name"
        :status="tank.status"
        :total-capacity="tank.totalCapacity"
        :liquid-level="tank.liquidLevel"
        :temperature="tank.temperature"
        :has-level-alert="tank.hasLevelAlert"
        @detail="handleTankDetail"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import TankCard from './components/TankCard.vue'

interface Alert {
  type: 'severe' | 'warning'
  icon: string
  message: string
}

interface Tank {
  id: string
  name: string
  status: '正常运行' | '故障' | '停机' | '备用'
  totalCapacity: string
  liquidLevel: number
  temperature: string
  hasLevelAlert: boolean
}

// 警告消息列表
const alerts = ref<Alert[]>([
  {
    type: 'severe',
    icon: '▲',
    message: '2号储罐液位超限'
  },
  {
    type: 'warning',
    icon: '●',
    message: '2号储罐内低压泵振动异常'
  }
])

// 储罐数据
const tanks = ref<Tank[]>([
  {
    id: '0330TK01',
    name: '1#储罐',
    status: '正常运行',
    totalCapacity: '5000m³',
    liquidLevel: 90,
    temperature: '-161℃',
    hasLevelAlert: false
  },
  {
    id: '0330TK02',
    name: '2#储罐',
    status: '故障',
    totalCapacity: '5000m³',
    liquidLevel: 100,
    temperature: '-161℃',
    hasLevelAlert: true
  },
  {
    id: '0330TK03',
    name: '3#储罐',
    status: '停机',
    totalCapacity: '5000m³',
    liquidLevel: 90,
    temperature: '-161℃',
    hasLevelAlert: false
  },
  {
    id: '0330TK04',
    name: '4#储罐',
    status: '备用',
    totalCapacity: '5000m³',
    liquidLevel: 90,
    temperature: '-161℃',
    hasLevelAlert: false
  }
])

const handleTankDetail = (data: { tankName: string; tankId: string }) => {
  console.log('查看详情:', data)
  ElMessage.info(`查看 ${data.tankName}(${data.tankId}) 的详细信息`)
}
</script>

<style lang="scss" scoped>
.tank-monitor-container {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  // background: #0a1929;
  padding: 0 1% 1% 1%;
  overflow: hidden;
}

.alert-bar {
  flex: 0 0 auto;
  display: flex;
  flex-direction: row;
  gap: 10px;
  margin-bottom: 1%;
  font-size: 15px;
  .alert-item {
    display: flex;
    align-items: center;
    padding: 1% 2%;
    border-radius: 4px;
    font-size: 0.95em;
    gap: 1%;
    white-space: nowrap;

    .alert-icon {
      font-size: 1.2em;
      font-weight: bold;
    }

    &.severe {
      background: rgba(246, 173, 85, 0.2);
      border: 1px solid #f6ad55;
      color: #f6ad55;
    }

    &.warning {
      background: rgba(246, 173, 85, 0.15);
      border: 1px solid #f6ad55;
      color: #f6ad55;
    }
  }
}

.tank-grid {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  grid-template-rows: repeat(2, 1fr);
  gap: 1.5%;
  min-height: 0;
  overflow: auto;
}

// 响应式设计
@media (max-width: 1200px) {
  .tank-grid {
    grid-template-columns: 1fr;
    grid-template-rows: repeat(4, 1fr);
  }
}
</style>
