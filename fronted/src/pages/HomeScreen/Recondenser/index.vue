<template>
  <DeviceDetailLayout summary-title="再冷凝器概览" device-title="设备状态">
    <template #summary>
      <div class="device-metric-grid">
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">进口压力</span>
            <span class="device-metric-tag">正常</span>
          </div>
          <div class="device-metric-value">0.08<span class="device-metric-unit">MPa</span></div>
          <div class="device-metric-foot">入口压力稳定</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">液位负荷</span>
            <span class="device-metric-tag">正常</span>
          </div>
          <div class="device-metric-value">65<span class="device-metric-unit">%</span></div>
          <div class="device-metric-foot">液位处于推荐区间</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">出口温度</span>
            <span class="device-metric-tag">正常</span>
          </div>
          <div class="device-metric-value">-155.2<span class="device-metric-unit">℃</span></div>
          <div class="device-metric-foot">换热过程平稳</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">换热效率</span>
            <span class="device-metric-tag">优良</span>
          </div>
          <div class="device-metric-value">92.5<span class="device-metric-unit">%</span></div>
          <div class="device-metric-foot">当前效率表现良好</div>
        </div>
      </div>
    </template>

    <div class="device-list-grid">
      <DevicePanel
        :svg-name="'recondenser'"
        :device-name="device.name"
        :tag-code="device.tagCode"
        :params="device.params"
        :status="device.status"
      />
    </div>
  </DeviceDetailLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import DevicePanel from '@/pages/HomeScreen/components/DevicePanel.vue'
import DeviceDetailLayout from '@/pages/HomeScreen/components/DeviceDetailLayout.vue'

interface RecondenserDevice {
  tagCode: string
  name: string
  status: 'normal' | 'warning' | 'error' | 'standby'
  params: Array<{
    label: string
    value: string | number
    unit: string
  }>
}

const device = ref<RecondenserDevice>({
  tagCode: '0330-V-01',
  name: '再冷凝器',
  status: 'normal',
  params: [
    { label: '进口压力', value: 0.08, unit: 'MPa' },
    { label: '进口温度', value: -145, unit: '℃' },
    { label: '液位负荷', value: 65, unit: '%' },
    { label: '回流温度', value: -158, unit: '℃' },
    { label: '出口温度', value: -155.2, unit: '℃' },
    { label: '出口压力', value: 11.8, unit: 'MPa' },
    { label: 'BOG 入口流量', value: 12500, unit: 'Nm³/h' },
    { label: '换热效率', value: 92.5, unit: '%' }
  ]
})
</script>
