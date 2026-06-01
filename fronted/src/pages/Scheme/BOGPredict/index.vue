<template>
  <div class="bog-page">
    <cm-panel style="height: 56px; display: flex; align-items: center;" class="mb10">
      <div class="toolbar-content">
        <div class="toolbar-left">
          <div class="plan-field">
            <span class="field-label">方案名称</span>
            <el-input v-model="planName" placeholder="请输入方案名称" size="small" class="cm-inline-control" />
          </div>
          <div class="toolbar-actions">
            <el-button type="primary" size="small" @click="reloadFromStore">加载结果</el-button>
            <el-button type="primary" plain size="small" @click="exportData">导出数据</el-button>
          </div>
        </div>
      </div>
    </cm-panel>

    <pagePanelNew class="bog-content">
      <div class="content-layout">
        <div class="leftBox">
          <div class="view-switch">
            <el-segmented v-model="activeView" :options="viewOptions" class="bog-segmented" />
          </div>

          <div class="chart-stack">
            <pagePanel
              v-for="chart in activeCharts"
              :key="chart.key"
              :header-title="chart.title"
              class="chart-panel"
            >
              <div v-if="chart.key === 'summary'" class="status-grid">
                <div v-for="item in activeSummaryCards" :key="item.label" class="status-card">
                  <div class="status-label">{{ item.label }}</div>
                  <div class="status-value" :style="{ color: item.color }">
                    {{ item.value }}
                    <span class="status-unit">{{ item.unit }}</span>
                  </div>
                </div>
              </div>
              <div v-else class="chart-canvas">
                <scEcharts class="chart-echarts" height="100%" :option="chart.option" />
              </div>
            </pagePanel>
          </div>
        </div>

        <div class="rightBox">
            <el-table :data="activeTableRows" :border="true" height="100%">
              <el-table-column
                v-for="column in activeTableColumns"
                :key="column.prop"
                :prop="column.prop"
                :label="column.label"
                :min-width="column.minWidth || 120"
                :align="column.align || 'center'"
                :show-overflow-tooltip="true"
              />
            </el-table>
        </div>
      </div>
    </pagePanelNew>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: 'BOGPredict'
})

import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import scEcharts from '@/components/scEcharts/index.vue'
import type { Bog, HourlyRecord, SimulationResult } from '@/api/simulation'
import { useSchemeStore } from '@/store/modules/scheme'

type ViewKey = 'weather' | 'trend' | 'components'

interface ChartCard {
  key: string
  title: string
  option?: Record<string, any>
}

interface TableColumn {
  prop: string
  label: string
  minWidth?: number
  align?: 'left' | 'center' | 'right'
}

interface StatusCard {
  label: string
  value: string
  unit: string
  color: string
}

const schemeStore = useSchemeStore()
const planName = ref('')
const activeView = ref<ViewKey>('weather')
const hourlyRecords = ref<HourlyRecord[]>([])
const bogData = ref<Bog>({ hours: [], bog_mech_kgph: [], bog_pred_kgph: [] })

const viewOptions = [
  { label: '天气环境', value: 'weather' },
  { label: 'BOG趋势', value: 'trend' },
  { label: '分量预测', value: 'components' }
]

const fallbackHours = Array.from({ length: 24 }, (_, index) => index)
const zeroSeries = (size: number) => Array.from({ length: size }, () => 0)

const componentSeries = computed(() => {
  const size = bogData.value.hours.length || hourlyRecords.value.length || fallbackHours.length
  return {
    staticBor: bogData.value.static_bor_kgph || zeroSeries(size),
    wallHeat: bogData.value.wall_heat_bog_kgph || zeroSeries(size),
    pumpHeat: bogData.value.pump_heat_bog_kgph || zeroSeries(size),
    pistonBog: bogData.value.piston_bog_kgph || zeroSeries(size),
    flashBog: bogData.value.flash_bog_kgph || zeroSeries(size)
  }
})

const xAxisData = computed(() => {
  const hours = bogData.value.hours?.length ? bogData.value.hours : hourlyRecords.value.map((item) => item.Hour)
  return (hours.length ? hours : fallbackHours).map((hour) => `${hour}:00`)
})

const unloadSeries = computed(() =>
  hourlyRecords.value.length
    ? hourlyRecords.value.map((item) => Number(item.Unload_m3h || 0))
    : xAxisData.value.map(() => 0)
)

const summary = computed(() => {
  const mech = bogData.value.bog_mech_kgph || []
  const pred = bogData.value.bog_pred_kgph || []
  if (!mech.length || !pred.length) {
    return { avgMech: 0, avgPred: 0, maxPred: 0, deviation: 0 }
  }

  const avgMech = mech.reduce((sum, val) => sum + val, 0) / mech.length
  const avgPred = pred.reduce((sum, val) => sum + val, 0) / pred.length
  const maxPred = Math.max(...pred)
  const deviation = avgMech === 0 ? 0 : Math.abs((avgPred - avgMech) / avgMech) * 100
  return { avgMech, avgPred, maxPred, deviation }
})

const weatherCards = computed<StatusCard[]>(() => [
  {
    label: '方案开始时间',
    value: schemeStore.initialCondition.startTime ? schemeStore.initialCondition.startTime.slice(5, 16) : '--',
    unit: '',
    color: '#53d8ff'
  },
  {
    label: '预测时长',
    value: String(xAxisData.value.length || 0),
    unit: 'h',
    color: '#37ff65'
  },
  {
    label: '目标外输量',
    value: formatNumber(schemeStore.initialCondition.targetOutputM3, 0),
    unit: 'm3',
    color: '#c86cff'
  },
  {
    label: '当前电价均值',
    value: formatNumber(avg(hourlyRecords.value.map((item) => Number(item.Elec_Price || 0))), 4),
    unit: '',
    color: '#ffd533'
  }
])

const statusCards = computed<StatusCard[]>(() => [
  {
    label: '平均预测BOG',
    value: formatNumber(summary.value.avgPred),
    unit: 'kg/h',
    color: '#37ff65'
  },
  {
    label: '平均机理BOG',
    value: formatNumber(summary.value.avgMech),
    unit: 'kg/h',
    color: '#4f88ff'
  },
  {
    label: '预测峰值',
    value: formatNumber(summary.value.maxPred),
    unit: 'kg/h',
    color: '#c86cff'
  },
  {
    label: '平均偏差',
    value: formatNumber(summary.value.deviation, 2),
    unit: '%',
    color: '#ffd533'
  }
])

const weatherSeries = computed(() => {
  const size = xAxisData.value.length || fallbackHours.length
  const temp = Array.from({ length: size }, (_, index) => 22 + Math.sin(index / 3) * 4)
  const pressure = Array.from({ length: size }, (_, index) => 101.2 + Math.cos(index / 4) * 0.6)
  return { temp, pressure }
})

const weatherChartOption = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'axis',
    backgroundColor: 'rgba(8, 21, 46, 0.94)',
    borderColor: 'rgba(50, 155, 255, 0.35)',
    textStyle: { color: '#fff' }
  },
  legend: {
    top: 6,
    left: 'center',
    textStyle: { color: '#cde5ff', fontSize: 11 },
    data: ['环境温度', '大气压']
  },
  grid: { left: 52, right: 56, top: 48, bottom: 30 },
  xAxis: {
    type: 'category',
    data: xAxisData.value,
    axisLine: { lineStyle: { color: 'rgba(118, 171, 255, 0.32)' } },
    axisLabel: { color: '#8fb8ea', fontSize: 10 }
  },
  yAxis: [
    {
      type: 'value',
      name: '温度(℃)',
      axisLabel: { color: '#53d8ff', fontSize: 10 },
      splitLine: { lineStyle: { color: 'rgba(118, 171, 255, 0.1)' } }
    },
    {
      type: 'value',
      name: '压力(kPa)',
      axisLabel: { color: '#ffd25a', fontSize: 10 },
      splitLine: { show: false }
    }
  ],
  series: [
    {
      name: '环境温度',
      type: 'line',
      smooth: true,
      symbol: 'none',
      data: weatherSeries.value.temp,
      lineStyle: { color: '#53d8ff', width: 2.5 }
    },
    {
      name: '大气压',
      type: 'line',
      yAxisIndex: 1,
      smooth: true,
      symbol: 'none',
      data: weatherSeries.value.pressure,
      lineStyle: { color: '#ffd533', width: 2.5 }
    }
  ]
}))

const overviewChartOption = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'axis',
    backgroundColor: 'rgba(8, 21, 46, 0.94)',
    borderColor: 'rgba(50, 155, 255, 0.35)',
    textStyle: { color: '#fff' }
  },
  legend: {
    top: 6,
    left: 'center',
    textStyle: { color: '#cde5ff', fontSize: 11 },
    data: ['卸船量', '机理BOG', '预测BOG']
  },
  grid: { left: 50, right: 70, top: 48, bottom: 30 },
  xAxis: {
    type: 'category',
    data: xAxisData.value,
    axisLine: { lineStyle: { color: 'rgba(118, 171, 255, 0.32)' } },
    axisLabel: { color: '#8fb8ea', fontSize: 10 }
  },
  yAxis: [
    {
      type: 'value',
      name: '卸船量',
      axisLabel: { color: '#53d8ff', fontSize: 10 },
      splitLine: { lineStyle: { color: 'rgba(118, 171, 255, 0.1)' } }
    },
    {
      type: 'value',
      name: 'BOG',
      axisLabel: { color: '#ffd25a', fontSize: 10 },
      splitLine: { show: false }
    }
  ],
  series: [
    {
      name: '卸船量',
      type: 'bar',
      data: unloadSeries.value,
      barWidth: '52%',
      itemStyle: {
        color: 'rgba(88, 217, 255, 0.75)',
        borderRadius: [4, 4, 0, 0]
      }
    },
    {
      name: '机理BOG',
      type: 'line',
      yAxisIndex: 1,
      smooth: true,
      symbol: 'none',
      data: bogData.value.bog_mech_kgph || [],
      lineStyle: { color: '#20e0a2', width: 2.5, type: 'dashed' }
    },
    {
      name: '预测BOG',
      type: 'line',
      yAxisIndex: 1,
      smooth: true,
      symbol: 'none',
      data: bogData.value.bog_pred_kgph || [],
      lineStyle: { color: '#3f8cff', width: 3 }
    }
  ]
}))

const componentChartOption = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'axis',
    backgroundColor: 'rgba(10, 24, 52, 0.96)',
    borderColor: 'rgba(0, 208, 255, 0.35)',
    textStyle: { color: '#dff6ff' }
  },
  legend: {
    top: 4,
    left: 'center',
    textStyle: { color: '#cde5ff', fontSize: 11 },
    data: ['静态蒸发', '壁面漏热', '泵热输入', '活塞效应', '闪蒸', 'BOG预测']
  },
  grid: { left: 56, right: 24, top: 48, bottom: 32 },
  xAxis: {
    type: 'category',
    boundaryGap: false,
    data: xAxisData.value,
    axisLabel: { color: '#8fb8ea', fontSize: 10 },
    axisLine: { lineStyle: { color: 'rgba(118, 171, 255, 0.22)' } }
  },
  yAxis: {
    type: 'value',
    name: 'BOG (kg/h)',
    nameTextStyle: { color: '#8fb8ea' },
    axisLabel: { color: '#8fb8ea', fontSize: 10 },
    splitLine: { lineStyle: { color: 'rgba(118, 171, 255, 0.08)' } }
  },
  series: [
    {
      name: '静态蒸发',
      type: 'line',
      stack: 'bog',
      areaStyle: {},
      symbol: 'none',
      data: componentSeries.value.staticBor,
      lineStyle: { width: 0.5, color: '#60a5fa' },
      itemStyle: { color: '#60a5fa' }
    },
    {
      name: '壁面漏热',
      type: 'line',
      stack: 'bog',
      areaStyle: {},
      symbol: 'none',
      data: componentSeries.value.wallHeat,
      lineStyle: { width: 0.5, color: '#34d399' },
      itemStyle: { color: '#34d399' }
    },
    {
      name: '泵热输入',
      type: 'line',
      stack: 'bog',
      areaStyle: {},
      symbol: 'none',
      data: componentSeries.value.pumpHeat,
      lineStyle: { width: 0.5, color: '#fbbf24' },
      itemStyle: { color: '#fbbf24' }
    },
    {
      name: '活塞效应',
      type: 'line',
      stack: 'bog',
      areaStyle: {},
      symbol: 'none',
      data: componentSeries.value.pistonBog,
      lineStyle: { width: 0.5, color: '#fb7185' },
      itemStyle: { color: '#fb7185' }
    },
    {
      name: '闪蒸',
      type: 'line',
      stack: 'bog',
      areaStyle: {},
      symbol: 'none',
      data: componentSeries.value.flashBog,
      lineStyle: { width: 0.5, color: '#a78bfa' },
      itemStyle: { color: '#a78bfa' }
    },
    {
      name: 'BOG预测',
      type: 'line',
      smooth: true,
      symbol: 'none',
      data: bogData.value.bog_pred_kgph || [],
      lineStyle: { color: '#111827', width: 2.6 }
    }
  ]
}))

const chartGroups = computed<Record<ViewKey, ChartCard[]>>(() => ({
  weather: [
    { key: 'summary', title: '天气环境概览' },
    { key: 'weather', title: '天气环境趋势', option: weatherChartOption.value }
  ],
  trend: [
    { key: 'summary', title: 'BOG状态概览' },
    { key: 'overview', title: '卸船量与BOG趋势', option: overviewChartOption.value }
  ],
  components: [
    { key: 'component', title: 'BOG分量构成', option: componentChartOption.value }
  ]
}))

const activeCharts = computed(() => chartGroups.value[activeView.value])
const activeSummaryCards = computed(() => (activeView.value === 'weather' ? weatherCards.value : statusCards.value))

const weatherRows = computed(() =>
  xAxisData.value.map((time, index) => ({
    time,
    temp: formatNumber(weatherSeries.value.temp[index], 2),
    pressure: formatNumber(weatherSeries.value.pressure[index], 2),
    unload: formatNumber(unloadSeries.value[index], 2)
  }))
)

const overviewRows = computed(() =>
  xAxisData.value.map((time, index) => {
    const bogPred = bogData.value.bog_pred_kgph?.[index] ?? 0
    const bogMech = bogData.value.bog_mech_kgph?.[index] ?? 0
    const deviation = bogMech === 0 ? 0 : ((bogPred - bogMech) / bogMech) * 100
    return {
      time,
      unload: formatNumber(unloadSeries.value[index], 2),
      bogMech: formatNumber(bogMech, 2),
      bogPred: formatNumber(bogPred, 2),
      deviation: formatNumber(deviation, 2)
    }
  })
)

const componentRows = computed(() =>
  xAxisData.value.map((time, index) => ({
    time,
    staticBor: formatNumber(componentSeries.value.staticBor[index], 2),
    wallHeat: formatNumber(componentSeries.value.wallHeat[index], 2),
    pumpHeat: formatNumber(componentSeries.value.pumpHeat[index], 2),
    pistonBog: formatNumber(componentSeries.value.pistonBog[index], 2),
    flashBog: formatNumber(componentSeries.value.flashBog[index], 2),
    bogPred: formatNumber(bogData.value.bog_pred_kgph?.[index], 2)
  }))
)

const tableConfigMap = computed<Record<ViewKey, { title: string; columns: TableColumn[]; rows: Array<Record<string, string | number>> }>>(() => ({
  weather: {
    title: '天气环境明细',
    columns: [
      { prop: 'time', label: '时间', minWidth: 88 },
      { prop: 'temp', label: '环境温度(℃)', minWidth: 118, align: 'right' },
      { prop: 'pressure', label: '大气压(kPa)', minWidth: 118, align: 'right' },
      { prop: 'unload', label: '卸船量(m3/h)', minWidth: 118, align: 'right' }
    ],
    rows: weatherRows.value
  },
  trend: {
    title: 'BOG趋势明细',
    columns: [
      { prop: 'time', label: '时间', minWidth: 88 },
      { prop: 'unload', label: '卸船量(m3/h)', minWidth: 118, align: 'right' },
      { prop: 'bogMech', label: '机理BOG(kg/h)', minWidth: 120, align: 'right' },
      { prop: 'bogPred', label: '预测BOG(kg/h)', minWidth: 120, align: 'right' },
      { prop: 'deviation', label: '偏差(%)', minWidth: 100, align: 'right' }
    ],
    rows: overviewRows.value
  },
  components: {
    title: 'BOG分量明细',
    columns: [
      { prop: 'time', label: '时间', minWidth: 88 },
      { prop: 'staticBor', label: '静态蒸发', minWidth: 110, align: 'right' },
      { prop: 'wallHeat', label: '壁面漏热', minWidth: 110, align: 'right' },
      { prop: 'pumpHeat', label: '泵热输入', minWidth: 110, align: 'right' },
      { prop: 'pistonBog', label: '活塞效应', minWidth: 110, align: 'right' },
      { prop: 'flashBog', label: '闪蒸', minWidth: 100, align: 'right' },
      { prop: 'bogPred', label: '预测BOG', minWidth: 110, align: 'right' }
    ],
    rows: componentRows.value
  }
}))

const activeTableColumns = computed(() => tableConfigMap.value[activeView.value].columns)
const activeTableRows = computed(() => tableConfigMap.value[activeView.value].rows)

const hydrateFromResult = (result: SimulationResult | null) => {
  hourlyRecords.value = result?.hourly || []
  bogData.value = result?.bog || { hours: [], bog_mech_kgph: [], bog_pred_kgph: [] }
}

const reloadFromStore = () => {
  planName.value = schemeStore.initialCondition.planName || ''
  hydrateFromResult(schemeStore.simulationResult as SimulationResult | null)
  if (!bogData.value.bog_pred_kgph?.length) {
    ElMessage.warning('当前还没有可展示的 BOG 结果')
  }
}

const exportData = () => {
  if (!bogData.value.bog_pred_kgph?.length) {
    ElMessage.warning('暂无可导出的 BOG 数据')
    return
  }

  const header = ['Hour', 'Unload_m3h', 'BOG_mech_kgph', 'BOG_pred_kgph', 'static_bor_kgph', 'wall_heat_bog_kgph', 'pump_heat_bog_kgph', 'piston_bog_kgph', 'flash_bog_kgph']
  const rows = xAxisData.value.map((_, index) => [
    bogData.value.hours?.[index] ?? index,
    unloadSeries.value[index] ?? '',
    bogData.value.bog_mech_kgph?.[index] ?? '',
    bogData.value.bog_pred_kgph?.[index] ?? '',
    componentSeries.value.staticBor[index] ?? '',
    componentSeries.value.wallHeat[index] ?? '',
    componentSeries.value.pumpHeat[index] ?? '',
    componentSeries.value.pistonBog[index] ?? '',
    componentSeries.value.flashBog[index] ?? ''
  ])
  const csv = [header, ...rows].map((row) => row.join(',')).join('\n')
  const blob = new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${planName.value || 'bog_predict'}.csv`
  link.click()
  URL.revokeObjectURL(url)
}

const formatNumber = (value: number | string | null | undefined, digits = 2) => {
  const num = Number(value ?? 0)
  return Number.isFinite(num) ? num.toFixed(digits) : '0.00'
}

const avg = (values: number[]) => (values.length ? values.reduce((sum, item) => sum + item, 0) / values.length : 0)

onMounted(() => {
  reloadFromStore()
})

watch(
  () => schemeStore.simulationResult,
  (value) => {
    if (value) {
      hydrateFromResult(value as SimulationResult | null)
    }
  },
  { deep: true }
)
</script>

<style scoped lang="scss">
.bog-page {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.toolbar-content {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  width: 100%;
}

.plan-field {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 24%;
  min-width: 220px;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.field-label {
  color: #fff;
  font-size: 14px;
  white-space: nowrap;
}

.bog-content {
  flex: 1;
  min-height: 0;
  padding: 10px;
}

.content-layout {
  display: flex;
  width: 100%;
  height: 100%;
}

.leftBox,
.rightBox {
  min-width: 0;
  height: 100%;
}

.leftBox {
  width: 58%;
  margin-right: 10px;
  display: flex;
  flex-direction: column;
}

.rightBox {
  width: 42%;
}

.view-switch {
  margin-bottom: 10px;
}

.bog-segmented {
  --el-segmented-bg-color: rgba(7, 31, 52, 0.92);
  --el-segmented-item-selected-bg-color: linear-gradient(90deg, #157eff, #18d2ff);
  --el-segmented-item-selected-color: #ffffff;
  --el-border-radius-base: 4px;
}

.bog-segmented :deep(.el-segmented) {
  padding: 4px;
  border: 1px solid rgba(0, 200, 255, 0.42);
  box-shadow: 0 0 0 1px rgba(0, 200, 255, 0.12) inset, 0 0 10px rgba(0, 200, 255, 0.18);
}

.bog-segmented :deep(.el-segmented__item) {
  color: rgba(217, 236, 255, 0.78);
  border-radius: 4px;
}

.bog-segmented :deep(.el-segmented__item:hover) {
  color: #ffffff;
}

.bog-segmented :deep(.el-segmented__item-selected) {
  color: #ffffff;
  box-shadow: 0 8px 18px rgba(24, 210, 255, 0.24);
}

.chart-stack {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-rows: repeat(auto-fit, minmax(0, 1fr));
  gap: 10px;
}

.chart-panel,
.table-panel {
  height: 100%;
}

.chart-panel :deep(.panelBox),
.table-panel :deep(.panelBox) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.chart-panel :deep(.panelBody),
.table-panel :deep(.panelBody) {
  flex: 1;
  min-height: 0;
  padding: 0;
}

.chart-canvas {
  flex: 1;
  min-height: 0;
  height: 100%;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.chart-canvas :deep(.chart-echarts) {
  width: 100%;
  height: 100% !important;
}

.status-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  width: 100%;
  height: 100%;
  padding: 12px;
}

.status-card {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 10px;
  padding: 16px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(0, 200, 255, 0.12);
}

.status-label {
  color: #8fb8ea;
  font-size: 14px;
}

.status-value {
  font-size: 28px;
  font-weight: 700;
}

.status-unit {
  margin-left: 6px;
  font-size: 14px;
  color: #fff;
}
</style>
