<template>
  <DeviceDetailLayout summary-title="海水系统概览" device-title="海水泵运行状态">
    <template #summary>
      <div class="device-metric-grid">
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">运行泵组</span>
            <span class="device-metric-tag">正常</span>
          </div>
          <div class="device-metric-value">3<span class="device-metric-unit">台</span></div>
          <div class="device-metric-foot">冷源侧供给稳定</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">备用泵组</span>
            <span class="device-metric-tag is-neutral">待命</span>
          </div>
          <div class="device-metric-value">3<span class="device-metric-unit">台</span></div>
          <div class="device-metric-foot">具备轮换和切换空间</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">平均出口压力</span>
            <span class="device-metric-tag">稳定</span>
          </div>
          <div class="device-metric-value">0.65<span class="device-metric-unit">MPa</span></div>
          <div class="device-metric-foot">出口压力符合换热工况</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">单泵设计流量</span>
            <span class="device-metric-tag">充足</span>
          </div>
          <div class="device-metric-value">2500<span class="device-metric-unit">m³/h</span></div>
          <div class="device-metric-foot">当前流量能力充足</div>
        </div>
      </div>
    </template>

    <div class="device-list-grid">
      <DevicePanel
        v-for="pump in pumps"
        :key="pump.tagCode"
        :svg-name="'seawater-pump'"
        :device-name="pump.name"
        :tag-code="pump.tagCode"
        :params="pump.params"
        :status="pump.status"
      />
    </div>
  </DeviceDetailLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import DevicePanel from '@/pages/HomeScreen/components/DevicePanel.vue'
import DeviceDetailLayout from '@/pages/HomeScreen/components/DeviceDetailLayout.vue'

interface SeawaterPump {
  tagCode: string
  name: string
  status: 'normal' | 'warning' | 'error' | 'standby'
  params: Array<{
    label: string
    value: string | number
    unit: string
  }>
}

const pumps = ref<SeawaterPump[]>([
  {
    tagCode: '0511-P-01A',
    name: 'A 海水泵',
    status: 'normal',
    params: [
      { label: '入口压力', value: 0.25, unit: 'MPa' },
      { label: '出口压力', value: 0.65, unit: 'MPa' },
      { label: '入口温度', value: 15, unit: '℃' },
      { label: '出口温度', value: 16, unit: '℃' },
      { label: '流量', value: 2500, unit: 'm³/h' },
      { label: '电流', value: 85, unit: 'A' }
    ]
  },
  {
    tagCode: '0511-P-01B',
    name: 'B 海水泵',
    status: 'normal',
    params: [
      { label: '入口压力', value: 0.25, unit: 'MPa' },
      { label: '出口压力', value: 0.65, unit: 'MPa' },
      { label: '入口温度', value: 15, unit: '℃' },
      { label: '出口温度', value: 16, unit: '℃' },
      { label: '流量', value: 2500, unit: 'm³/h' },
      { label: '电流', value: 86, unit: 'A' }
    ]
  },
  {
    tagCode: '0511-P-01C',
    name: 'C 海水泵',
    status: 'normal',
    params: [
      { label: '入口压力', value: 0.25, unit: 'MPa' },
      { label: '出口压力', value: 0.65, unit: 'MPa' },
      { label: '入口温度', value: 15, unit: '℃' },
      { label: '出口温度', value: 16, unit: '℃' },
      { label: '流量', value: 2500, unit: 'm³/h' },
      { label: '电流', value: 84, unit: 'A' }
    ]
  },
  {
    tagCode: '0511-P-01D',
    name: 'D 海水泵',
    status: 'standby',
    params: [
      { label: '入口压力', value: 0.2, unit: 'MPa' },
      { label: '出口压力', value: 0, unit: 'MPa' },
      { label: '入口温度', value: 15, unit: '℃' },
      { label: '出口温度', value: 25, unit: '℃' },
      { label: '流量', value: 0, unit: 'm³/h' },
      { label: '电流', value: 0, unit: 'A' }
    ]
  },
  {
    tagCode: '0511-P-01E',
    name: 'E 海水泵',
    status: 'standby',
    params: [
      { label: '入口压力', value: 0.2, unit: 'MPa' },
      { label: '出口压力', value: 0, unit: 'MPa' },
      { label: '入口温度', value: 15, unit: '℃' },
      { label: '出口温度', value: 25, unit: '℃' },
      { label: '流量', value: 0, unit: 'm³/h' },
      { label: '电流', value: 0, unit: 'A' }
    ]
  },
  {
    tagCode: '0511-P-01F',
    name: 'F 海水泵',
    status: 'standby',
    params: [
      { label: '入口压力', value: 0.2, unit: 'MPa' },
      { label: '出口压力', value: 0, unit: 'MPa' },
      { label: '入口温度', value: 15, unit: '℃' },
      { label: '出口温度', value: 25, unit: '℃' },
      { label: '流量', value: 0, unit: 'm³/h' },
      { label: '电流', value: 0, unit: 'A' }
    ]
  }
])
</script>
