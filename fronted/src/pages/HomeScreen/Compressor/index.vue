<template>
  <DeviceDetailLayout summary-title="运行概览" device-title="压缩机状态">
    <template #summary>
      <div class="device-metric-grid">
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">在线机组</span>
            <span class="device-metric-tag">稳定</span>
          </div>
          <div class="device-metric-value">2<span class="device-metric-unit">台</span></div>
          <div class="device-metric-foot">当前 BOG 压缩机运行正常</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">备用机组</span>
            <span class="device-metric-tag is-neutral">待命</span>
          </div>
          <div class="device-metric-value">1<span class="device-metric-unit">台</span></div>
          <div class="device-metric-foot">具备切换能力</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">平均出口压力</span>
            <span class="device-metric-tag">正常</span>
          </div>
          <div class="device-metric-value">12.5<span class="device-metric-unit">MPa</span></div>
          <div class="device-metric-foot">满足当前输送需求</div>
        </div>
        <div class="device-metric-card">
          <div class="device-metric-head">
            <span class="device-metric-label">平均振动值</span>
            <span class="device-metric-tag">受控</span>
          </div>
          <div class="device-metric-value">2.3<span class="device-metric-unit">mm/s</span></div>
          <div class="device-metric-foot">设备状态平稳</div>
        </div>
      </div>
    </template>

    <div class="device-list-grid">
      <DevicePanel
        v-for="comp in compressors"
        :key="comp.tagCode"
        :svg-name="'compressor'"
        :device-name="comp.name"
        :tag-code="comp.tagCode"
        :params="comp.params"
        :status="comp.status"
        :show-actions="true"
      >
        <template #actions>
          <div class="compressor-actions">
            <el-button size="small" type="success" @click="refreshData">刷新</el-button>
            <el-button size="small" @click="exportData">导出</el-button>
          </div>
        </template>
      </DevicePanel>
    </div>
  </DeviceDetailLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import DevicePanel from '@/pages/HomeScreen/components/DevicePanel.vue'
import DeviceDetailLayout from '@/pages/HomeScreen/components/DeviceDetailLayout.vue'

interface Compressor {
  tagCode: string
  name: string
  status: 'normal' | 'warning' | 'error' | 'standby'
  params: Array<{
    label: string
    value: string | number
    unit: string
  }>
}

const compressors = ref<Compressor[]>([
  {
    tagCode: '0330-C-01A',
    name: '1#BOG 压缩机',
    status: 'normal',
    params: [
      { label: '入口压力', value: 0.08, unit: 'MPa' },
      { label: '出口压力', value: 12.5, unit: 'MPa' },
      { label: '入口温度', value: -145.2, unit: '℃' },
      { label: '出口温度', value: 45.8, unit: '℃' },
      { label: '电机转速', value: 2850, unit: 'rpm' },
      { label: '振动值', value: 2.3, unit: 'mm/s' }
    ]
  },
  {
    tagCode: '0330-C-01B',
    name: '2#BOG 压缩机',
    status: 'standby',
    params: [
      { label: '入口压力', value: 0, unit: 'MPa' },
      { label: '出口压力', value: 0, unit: 'MPa' },
      { label: '入口温度', value: -150, unit: '℃' },
      { label: '出口温度', value: 25, unit: '℃' },
      { label: '电机转速', value: 0, unit: 'rpm' },
      { label: '振动值', value: 0, unit: 'mm/s' }
    ]
  }
])

const refreshData = () => {
  console.log('刷新数据')
}

const exportData = () => {
  console.log('导出报表')
}
</script>

<style lang="scss" scoped>
.compressor-actions {
  width: 100%;
  display: flex;
  gap: 10px;
  justify-content: flex-end;
}
</style>
