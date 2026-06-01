<template>
  <DeviceDetailLayout summary-title="阀门概览" device-title="阀门状态">
    <template #summary>
      <div class="device-metric-grid">
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">开启阀门</span>
            <span class="device-metric-tag">正常</span>
          </div>
          <div class="device-metric-value">5<span class="device-metric-unit">个</span></div>
          <div class="device-metric-foot">主流程阀门处于工作状态</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">关闭阀门</span>
            <span class="device-metric-tag is-neutral">隔离</span>
          </div>
          <div class="device-metric-value">3<span class="device-metric-unit">个</span></div>
          <div class="device-metric-foot">处于停运或隔离状态</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">调节阀门</span>
            <span class="device-metric-tag">跟随</span>
          </div>
          <div class="device-metric-value">2<span class="device-metric-unit">个</span></div>
          <div class="device-metric-foot">参与流量与压力控制</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">关键控制阀</span>
            <span class="device-metric-tag is-warning">重点</span>
          </div>
          <div class="device-metric-value">PV005<span class="device-metric-unit">A/B</span></div>
          <div class="device-metric-foot">与压力控制策略联动</div>
        </div>
      </div>
    </template>

    <div class="device-inline-note is-warning">
      0330PV005A/B 为压力控制关键阀，建议与压力控制策略页面联动查看。
    </div>
    <div class="device-list-grid">
      <DevicePanel
        v-for="valve in valves"
        :key="valve.tagCode"
        :svg-name="'valve'"
        :device-name="valve.name"
        :tag-code="valve.tagCode"
        :params="valve.params"
        :status="valve.status"
      />
    </div>
  </DeviceDetailLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import DevicePanel from '@/pages/HomeScreen/components/DevicePanel.vue'
import DeviceDetailLayout from '@/pages/HomeScreen/components/DeviceDetailLayout.vue'

interface Valve {
  tagCode: string
  name: string
  status: 'normal' | 'warning' | 'error' | 'standby'
  params: Array<{
    label: string
    value: string | number
    unit: string
  }>
}

const valves = ref<Valve[]>([
  {
    tagCode: '0330-V-001',
    name: '入口切断阀',
    status: 'normal',
    params: [
      { label: '上游压力', value: 1.2, unit: 'MPa' },
      { label: '下游压力', value: 1.18, unit: 'MPa' },
      { label: '上游温度', value: -161, unit: '℃' },
      { label: '下游温度', value: -161, unit: '℃' },
      { label: '阀门开度', value: 100, unit: '%' },
      { label: '通过流量', value: 8500, unit: 'm³/h' }
    ]
  },
  {
    tagCode: '0330-V-002',
    name: '出口切断阀',
    status: 'normal',
    params: [
      { label: '上游压力', value: 12.5, unit: 'MPa' },
      { label: '下游压力', value: 12.3, unit: 'MPa' },
      { label: '上游温度', value: -158, unit: '℃' },
      { label: '下游温度', value: -158, unit: '℃' },
      { label: '阀门开度', value: 100, unit: '%' },
      { label: '通过流量', value: 8500, unit: 'm³/h' }
    ]
  },
  {
    tagCode: '0330-V-003',
    name: '放空阀',
    status: 'standby',
    params: [
      { label: '上游压力', value: 1.2, unit: 'MPa' },
      { label: '下游压力', value: 0, unit: 'MPa' },
      { label: '上游温度', value: -161, unit: '℃' },
      { label: '下游温度', value: 25, unit: '℃' },
      { label: '阀门开度', value: 0, unit: '%' },
      { label: '通过流量', value: 0, unit: 'm³/h' }
    ]
  },
  {
    tagCode: '0330-V-004',
    name: '旁通阀',
    status: 'standby',
    params: [
      { label: '上游压力', value: 1.2, unit: 'MPa' },
      { label: '下游压力', value: 0, unit: 'MPa' },
      { label: '上游温度', value: -161, unit: '℃' },
      { label: '下游温度', value: 25, unit: '℃' },
      { label: '阀门开度', value: 0, unit: '%' },
      { label: '通过流量', value: 0, unit: 'm³/h' }
    ]
  },
  {
    tagCode: '0330-V-005',
    name: '调节阀 A',
    status: 'normal',
    params: [
      { label: '上游压力', value: 1.5, unit: 'MPa' },
      { label: '下游压力', value: 1.2, unit: 'MPa' },
      { label: '上游温度', value: -160, unit: '℃' },
      { label: '下游温度', value: -161, unit: '℃' },
      { label: '阀门开度', value: 65, unit: '%' },
      { label: '通过流量', value: 5500, unit: 'm³/h' }
    ]
  },
  {
    tagCode: '0330-V-006',
    name: '调节阀 B',
    status: 'normal',
    params: [
      { label: '上游压力', value: 1.5, unit: 'MPa' },
      { label: '下游压力', value: 1.2, unit: 'MPa' },
      { label: '上游温度', value: -160, unit: '℃' },
      { label: '下游温度', value: -161, unit: '℃' },
      { label: '阀门开度', value: 58, unit: '%' },
      { label: '通过流量', value: 4900, unit: 'm³/h' }
    ]
  },
  {
    tagCode: '0330-V-007',
    name: '回流阀',
    status: 'normal',
    params: [
      { label: '上游压力', value: 1.2, unit: 'MPa' },
      { label: '下游压力', value: 0, unit: 'MPa' },
      { label: '上游温度', value: -161, unit: '℃' },
      { label: '下游温度', value: 25, unit: '℃' },
      { label: '阀门开度', value: 0, unit: '%' },
      { label: '通过流量', value: 0, unit: 'm³/h' }
    ]
  },
  {
    tagCode: '0330-V-008',
    name: '进料阀',
    status: 'normal',
    params: [
      { label: '上游压力', value: 1.2, unit: 'MPa' },
      { label: '下游压力', value: 1.18, unit: 'MPa' },
      { label: '上游温度', value: -161, unit: '℃' },
      { label: '下游温度', value: -161, unit: '℃' },
      { label: '阀门开度', value: 100, unit: '%' },
      { label: '通过流量', value: 8500, unit: 'm³/h' }
    ]
  }
])
</script>
