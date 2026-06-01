<template>
  <DeviceDetailLayout summary-title="卸船作业概览" device-title="卸船臂状态" aside-title="船舶与作业信息">
    <template #summary>
      <div class="device-inline-note-list" v-if="alerts.length">
        <div
          v-for="(alert, index) in alerts"
          :key="index"
          class="device-inline-note"
          :class="{ 'is-warning': alert.type !== 'severe' }"
        >
          {{ alert.icon }} {{ alert.message }}
        </div>
      </div>

      <div class="device-metric-grid">
        <div class="device-metric-card" v-for="(card, index) in overviewCards" :key="index">
          <div class="device-metric-head">
            <span class="device-metric-label">{{ card.title }}</span>
            <span
              class="device-metric-tag"
              :class="{
                'is-warning': card.status === 'warning',
                'is-error': card.status === 'error'
              }"
            >
              {{ card.statusText }}
            </span>
          </div>
          <div class="device-metric-value">{{ card.value }}</div>
          <div class="device-metric-foot">{{ card.label }} · {{ card.time }}</div>
        </div>
      </div>
    </template>

    <div class="device-list-grid">
      <DevicePanel
        v-for="arm in unloadingArms"
        :key="arm.tagCode"
        :svg-name="'unloading-arm'"
        :device-name="arm.name"
        :tag-code="arm.tagCode"
        :params="arm.params"
        :status="arm.status"
        :show-actions="true"
      >
        <template #actions>
          <div class="device-progress">
            <div class="device-progress-track">
              <div class="device-progress-fill" :style="{ width: `${arm.usage}%` }"></div>
            </div>
            <div class="device-progress-text">使用率 {{ arm.usage }}%</div>
          </div>
        </template>
      </DevicePanel>
    </div>

    <template #aside>
      <div class="device-info-card">
        <div class="device-info-title">{{ shipInfo.name }}</div>
        <div class="device-info-grid">
          <div v-for="(detail, idx) in shipDetails" :key="idx" class="device-info-item">
            <span class="device-info-label">{{ detail.label }}</span>
            <span class="device-info-value">{{ detail.value }}</span>
          </div>
        </div>
      </div>

      <div class="device-info-card">
        <div class="device-info-title">作业进度</div>
        <div class="device-stat-list">
          <div class="device-stat-row">
            <span class="device-stat-label">卸船进度</span>
            <span class="device-stat-value">{{ shipInfo.progress }}%</span>
          </div>
          <el-progress :percentage="shipInfo.progress" />
          <div class="device-stat-row">
            <span class="device-stat-label">剩余量</span>
            <span class="device-stat-value">{{ shipInfo.remaining }} m³</span>
          </div>
          <div class="device-stat-row">
            <span class="device-stat-label">预计完成时间</span>
            <span class="device-stat-value">{{ shipInfo.estimatedCompletion }}</span>
          </div>
        </div>
      </div>
    </template>
  </DeviceDetailLayout>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import DevicePanel from '../components/DevicePanel.vue'
import DeviceDetailLayout from '../components/DeviceDetailLayout.vue'

interface Alert {
  type: 'severe' | 'warning'
  icon: string
  message: string
}

interface OverviewCard {
  title: string
  status: 'normal' | 'warning' | 'error'
  statusText: string
  value: string
  label: string
  time: string
}

interface UnloadingArm {
  tagCode: string
  name: string
  status: 'normal' | 'warning' | 'error' | 'standby'
  usage: number
  params: Array<{
    label: string
    value: string | number
    unit: string
  }>
}

interface ShipInfo {
  name: string
  nationality: string
  tonnage: string
  dockingTime: string
  departureTime: string
  progress: number
  remaining: number
  estimatedCompletion: string
}

const alerts = ref<Alert[]>([
  {
    type: 'warning',
    icon: '●',
    message: '2 号卸船臂流量偏低'
  }
])

const overviewCards = ref<OverviewCard[]>([
  {
    title: '累计卸船量',
    status: 'normal',
    statusText: '正常',
    value: '125,680 m³',
    label: '本次卸船',
    time: '2024-01-20 14:30'
  },
  {
    title: '瞬时流量',
    status: 'normal',
    statusText: '稳定',
    value: '8,500 m³/h',
    label: '总流量',
    time: '实时'
  },
  {
    title: '卸船压力',
    status: 'normal',
    statusText: '正常',
    value: '0.85 MPa',
    label: '总管压力',
    time: '实时'
  },
  {
    title: '卸船温度',
    status: 'normal',
    statusText: '正常',
    value: '-161.5 ℃',
    label: '平均温度',
    time: '实时'
  }
])

const unloadingArms = ref<UnloadingArm[]>([
  {
    tagCode: 'arm-001',
    name: '1 号卸船臂',
    status: 'normal',
    usage: 85,
    params: [
      { label: '流量', value: 4200, unit: 'm³/h' },
      { label: '压力', value: 0.86, unit: 'MPa' },
      { label: '温度', value: -161.2, unit: '℃' },
      { label: '使用率', value: 85, unit: '%' }
    ]
  },
  {
    tagCode: 'arm-002',
    name: '2 号卸船臂',
    status: 'warning',
    usage: 62,
    params: [
      { label: '流量', value: 3100, unit: 'm³/h' },
      { label: '压力', value: 0.84, unit: 'MPa' },
      { label: '温度', value: -161.5, unit: '℃' },
      { label: '使用率', value: 62, unit: '%' }
    ]
  },
  {
    tagCode: 'arm-003',
    name: '3 号卸船臂',
    status: 'normal',
    usage: 83,
    params: [
      { label: '流量', value: 4150, unit: 'm³/h' },
      { label: '压力', value: 0.85, unit: 'MPa' },
      { label: '温度', value: -161.3, unit: '℃' },
      { label: '使用率', value: 83, unit: '%' }
    ]
  },
  {
    tagCode: 'arm-004',
    name: '4 号卸船臂',
    status: 'standby',
    usage: 0,
    params: [
      { label: '流量', value: 0, unit: 'm³/h' },
      { label: '压力', value: 0, unit: 'MPa' },
      { label: '温度', value: -160, unit: '℃' },
      { label: '使用率', value: 0, unit: '%' }
    ]
  }
])

const shipInfo = ref<ShipInfo>({
  name: 'LNG 运输船 "AL SAHLA"',
  nationality: '卡塔尔',
  tonnage: '17.4 万 m³',
  dockingTime: '2024-01-20 08:30',
  departureTime: '2024-01-21 18:00',
  progress: 68,
  remaining: 55320,
  estimatedCompletion: '2024-01-21 16:30'
})

const shipDetails = computed(() => [
  { label: '国籍', value: shipInfo.value.nationality },
  { label: '吨位', value: shipInfo.value.tonnage },
  { label: '靠泊时间', value: shipInfo.value.dockingTime },
  { label: '预计离港', value: shipInfo.value.departureTime }
])
</script>
