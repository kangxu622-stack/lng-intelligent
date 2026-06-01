<template>
  <DeviceDetailLayout summary-title="外输区概览" device-title="高压泵运行状态">
    <template #summary>
      <div class="device-metric-grid">
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">运行泵组</span>
            <span class="device-metric-tag">正常</span>
          </div>
          <div class="device-metric-value">4<span class="device-metric-unit">台</span></div>
          <div class="device-metric-foot">当前外输负荷稳定</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">备用泵组</span>
            <span class="device-metric-tag is-neutral">待命</span>
          </div>
          <div class="device-metric-value">3<span class="device-metric-unit">台</span></div>
          <div class="device-metric-foot">切换冗余充足</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">平均出口压力</span>
            <span class="device-metric-tag">受控</span>
          </div>
          <div class="device-metric-value">12.5<span class="device-metric-unit">MPa</span></div>
          <div class="device-metric-foot">满足外输母管压力要求</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">总外输流量</span>
            <span class="device-metric-tag">稳定</span>
          </div>
          <div class="device-metric-value">8500<span class="device-metric-unit">m³/h</span></div>
          <div class="device-metric-foot">当前负荷处于正常区间</div>
        </div>
      </div>
    </template>

    <div class="device-list-grid">
      <DevicePanel
        v-for="pump in pumps"
        :key="pump.tagCode"
        :svg-name="'high-pressure'"
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

interface HighPressurePump {
  tagCode: string
  name: string
  status: 'normal' | 'warning' | 'error' | 'standby'
  params: Array<{
    label: string
    value: string | number
    unit: string
  }>
}

const pumps = ref<HighPressurePump[]>([
  {
    tagCode: '0330P01A',
    name: 'A 高压泵',
    status: 'normal',
    params: [
      { label: '入口压力', value: 1.2, unit: 'MPa' },
      { label: '出口压力', value: 12.5, unit: 'MPa' },
      { label: '入口温度', value: -161, unit: '℃' },
      { label: '出口温度', value: -158, unit: '℃' },
      { label: '流量', value: 8500, unit: 'm³/h' },
      { label: '电流', value: 125, unit: 'A' }
    ]
  },
  {
    tagCode: '0330P01B',
    name: 'B 高压泵',
    status: 'normal',
    params: [
      { label: '入口压力', value: 1.2, unit: 'MPa' },
      { label: '出口压力', value: 12.5, unit: 'MPa' },
      { label: '入口温度', value: -161, unit: '℃' },
      { label: '出口温度', value: -158, unit: '℃' },
      { label: '流量', value: 8500, unit: 'm³/h' },
      { label: '电流', value: 128, unit: 'A' }
    ]
  },
  {
    tagCode: '0330P01C',
    name: 'C 高压泵',
    status: 'normal',
    params: [
      { label: '入口压力', value: 1.2, unit: 'MPa' },
      { label: '出口压力', value: 12.5, unit: 'MPa' },
      { label: '入口温度', value: -161, unit: '℃' },
      { label: '出口温度', value: -158, unit: '℃' },
      { label: '流量', value: 8500, unit: 'm³/h' },
      { label: '电流', value: 126, unit: 'A' }
    ]
  },
  {
    tagCode: '0330P01D',
    name: 'D 高压泵',
    status: 'normal',
    params: [
      { label: '入口压力', value: 1.2, unit: 'MPa' },
      { label: '出口压力', value: 12.5, unit: 'MPa' },
      { label: '入口温度', value: -161, unit: '℃' },
      { label: '出口温度', value: -158, unit: '℃' },
      { label: '流量', value: 8500, unit: 'm³/h' },
      { label: '电流', value: 127, unit: 'A' }
    ]
  },
  {
    tagCode: '0330P01E',
    name: 'E 高压泵',
    status: 'standby',
    params: [
      { label: '入口压力', value: 1.0, unit: 'MPa' },
      { label: '出口压力', value: 0, unit: 'MPa' },
      { label: '入口温度', value: -160, unit: '℃' },
      { label: '出口温度', value: 25, unit: '℃' },
      { label: '流量', value: 0, unit: 'm³/h' },
      { label: '电流', value: 0, unit: 'A' }
    ]
  },
  {
    tagCode: '0330P01F',
    name: 'F 高压泵',
    status: 'standby',
    params: [
      { label: '入口压力', value: 1.0, unit: 'MPa' },
      { label: '出口压力', value: 0, unit: 'MPa' },
      { label: '入口温度', value: -160, unit: '℃' },
      { label: '出口温度', value: 25, unit: '℃' },
      { label: '流量', value: 0, unit: 'm³/h' },
      { label: '电流', value: 0, unit: 'A' }
    ]
  },
  {
    tagCode: '0330P01G',
    name: 'G 高压泵',
    status: 'standby',
    params: [
      { label: '入口压力', value: 1.0, unit: 'MPa' },
      { label: '出口压力', value: 0, unit: 'MPa' },
      { label: '入口温度', value: -160, unit: '℃' },
      { label: '出口温度', value: 25, unit: '℃' },
      { label: '流量', value: 0, unit: 'm³/h' },
      { label: '电流', value: 0, unit: 'A' }
    ]
  }
])
</script>
