<template>
  <div class="scheme-page">
    <cm-panel style="height: 56px; display: flex; align-items: center;" class="mb10">
      <div class="toolbar-inline">
        <span class="label">方案名称</span>
        <el-input
          v-model="planName"
          class="cm-inline-control"
          placeholder="请输入方案名称"
          size="small"
          style="width: 220px"
          @input="onPlanNameChange"
        />
        <el-button type="primary" :loading="saving" @click="handleConfirm">确认</el-button>
        <el-button type="primary" plain @click="handleExport">导出</el-button>
      </div>
    </cm-panel>

    <pagePanelNew class="scheme-content">
      <div class="content-layout">
        <div class="leftBox">
          <div class="view-switch">
            <el-segmented v-model="activeView" :options="viewOptions" class="scheme-segmented" />
          </div>

          <div class="chart-scroll">
            <div class="chart-stack">
              <pagePanel
                v-for="chart in activeCharts"
                :key="chart.key"
                :header-title="chart.title"
                class="chart-panel"
                :class="chart.panelClass"
              >
                <div v-if="chart.key === 'energy'" class="energy-display">
                  <span class="energy-value">{{ totalEnergyCost.toFixed(2) }}</span>
                  <span class="energy-unit">元</span>
                </div>
                <div v-else class="chart-canvas">
                  <scEcharts class="chart-echarts" height="100%" :option="chart.option" />
                </div>
              </pagePanel>
            </div>
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
  name: 'Scheme'
})

import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import scEcharts from '@/components/scEcharts/index.vue'
import { saveSchedule, toPriorityMode, type HourlyRecord, type SimulationResult } from '@/api/simulation'
import { useSchemeStore } from '@/store/modules/scheme'
import { USER_INFO_KEY } from '@/config/global'
import * as XLSX from 'xlsx'

type ViewKey = 'overview' | 'compressor' | 'tank' | 'lp' | 'hp' | 'seawater' | 'orv'

interface ChartCard {
  key: string
  title: string
  option?: Record<string, any>
  panelClass?: string
}

interface TableColumn {
  prop: string
  label: string
  minWidth?: number
  align?: 'left' | 'center' | 'right'
}

const schemeStore = useSchemeStore()
const saving = ref(false)
const activeView = ref<ViewKey>('overview')
const resultRef = ref<SimulationResult | null>(null)

const viewOptions = [
  { label: '电费', value: 'overview' },
  { label: '压缩机', value: 'compressor' },
  { label: '罐存与BOG', value: 'tank' },
  { label: '低压泵', value: 'lp' },
  { label: '高压泵', value: 'hp' },
  { label: '海水泵', value: 'seawater' },
  { label: 'ORV', value: 'orv' }
]

const planName = computed({
  get: () => schemeStore.initialCondition.planName,
  set: (val: string) => schemeStore.setInitialCondition({ planName: val })
})

const onPlanNameChange = (val: string) => {
  schemeStore.setInitialCondition({ conditionName: val.trim() })
}

const hourlyRows = computed<HourlyRecord[]>(() => resultRef.value?.hourly || [])
const bogData = computed(() => resultRef.value?.bog || { hours: [], bog_mech_kgph: [], bog_pred_kgph: [] })
const deviceLabels = computed(() => resultRef.value?.labels)
const tankLevelData = computed(() => resultRef.value?.tank_levels || { hours: [], series: [] })
const xAxisData = computed(() => hourlyRows.value.map((item) => `${item.Hour}h`))
const totalEnergyCost = computed(() => Number(resultRef.value?.summary?.estimated_total_electric_cost || 0))

const defaultCompLabels = ['BOG压缩机0330-C-01A', 'BOG压缩机0330-C-01B', 'BOG压缩机0330-C-01C']
const defaultOrvLabels = Array.from({ length: 7 }, (_, index) => `ORV#${index + 1}`)
const fallbackLabels = (preferred: string[] | undefined, matrix: number[][] | undefined, prefix: string) => {
  if (preferred?.length) {
    return preferred
  }
  const width = matrix?.[0]?.length || 0
  return Array.from({ length: width }, (_, index) => `${prefix}${index + 1}`)
}

const lpLabels = computed(() => fallbackLabels(deviceLabels.value?.LP, resultRef.value?.unitized?.LP, 'LP-'))
const hpLabels = computed(() => fallbackLabels(deviceLabels.value?.HP, resultRef.value?.unitized?.HP, 'HP-'))
const swBigLabels = computed(() => fallbackLabels(deviceLabels.value?.SW_Big, resultRef.value?.unitized?.SW_Big, '海水大泵'))
const swSmallLabels = computed(() => fallbackLabels(deviceLabels.value?.SW_Small, resultRef.value?.unitized?.SW_Small, '海水小泵'))
const orvLabels = computed(() => deviceLabels.value?.ORV?.length ? deviceLabels.value.ORV : defaultOrvLabels)
const compLabels = computed(() => {
  const labels = deviceLabels.value?.Comp?.length ? deviceLabels.value.Comp : defaultCompLabels
  return labels.slice(0, Math.max(labels.length, resultRef.value?.unitized?.Comp?.[0]?.length || 0, 3))
})

const parseStartTime = () => {
  const raw = schemeStore.initialCondition.startTime
  if (!raw) {
    return null
  }
  const parsed = new Date(raw.replace(' ', 'T'))
  return Number.isNaN(parsed.getTime()) ? null : parsed
}

const getStepsPerHour = (matrixLength: number) => {
  const hourCount = hourlyRows.value.length
  if (!matrixLength || !hourCount) {
    return 1
  }
  const quotient = matrixLength / hourCount
  return Number.isInteger(quotient) && quotient > 0 ? quotient : 1
}

const getHeatmapXAxisData = (matrixLength: number) => {
  const start = parseStartTime()
  const stepsPerHour = getStepsPerHour(matrixLength)
  const stepMinutes = 60 / stepsPerHour
  if (!start || !Number.isFinite(stepMinutes) || stepMinutes <= 0) {
    return Array.from({ length: matrixLength }, (_, index) => `${index + 1}`)
  }

  return Array.from({ length: matrixLength }, (_, index) => {
    const current = new Date(start.getTime() + index * stepMinutes * 60 * 1000)
    const hour = String(current.getHours()).padStart(2, '0')
    const minute = String(current.getMinutes()).padStart(2, '0')
    return `${hour}:${minute}`
  })
}

const getHourlyActiveCount = (matrix: number[][] | undefined, hourIndex: number) => {
  if (!matrix?.length) {
    return 0
  }
  const stepsPerHour = getStepsPerHour(matrix.length)
  const start = hourIndex * stepsPerHour
  const slice = matrix.slice(start, start + stepsPerHour)
  if (!slice.length || !slice[0]?.length) {
    return 0
  }

  let total = 0
  for (let deviceIndex = 0; deviceIndex < slice[0].length; deviceIndex += 1) {
    let sum = 0
    for (const row of slice) {
      sum += Number(row[deviceIndex] || 0)
    }
    total += sum / slice.length
  }

  return Math.round(total)
}

const buildStepFieldKey = (index: number) => `device_${index}`

const buildStepTableData = (
  matrix: number[][] | undefined,
  labels: string[],
  valueFormatter: (value: number) => string
) => {
  const rowsMatrix = matrix || []
  const timeAxis = getHeatmapXAxisData(rowsMatrix.length)
  const columns: TableColumn[] = [
    { prop: 'time', label: '时间', minWidth: 100 }
  ]

  labels.forEach((label, index) => {
    columns.push({
      prop: buildStepFieldKey(index),
      label,
      minWidth: 140,
      align: 'center'
    })
  })

  const rows = rowsMatrix.map((row, rowIndex) => {
    const record: Record<string, string | number> = {
      time: timeAxis[rowIndex] || `${rowIndex + 1}`
    }
    labels.forEach((_, labelIndex) => {
      record[buildStepFieldKey(labelIndex)] = valueFormatter(Number(row[labelIndex] || 0))
    })
    return record
  })

  return { columns, rows }
}

const buildCombinedStepTableData = (
  groups: Array<{ matrix: number[][] | undefined; labels: string[] }>,
  valueFormatter: (value: number) => string
) => {
  const matrixLength = Math.max(...groups.map((group) => group.matrix?.length || 0), 0)
  const timeAxis = getHeatmapXAxisData(matrixLength)
  const columns: TableColumn[] = [
    { prop: 'time', label: '时间', minWidth: 100 }
  ]

  const rows = Array.from({ length: matrixLength }, (_, rowIndex) => {
    const record: Record<string, string | number> = {
      time: timeAxis[rowIndex] || `${rowIndex + 1}`
    }
    return record
  })

  let fieldIndex = 0
  groups.forEach((group) => {
    group.labels.forEach((label, labelIndex) => {
      const field = buildStepFieldKey(fieldIndex)
      columns.push({
        prop: field,
        label,
        minWidth: 140,
        align: 'center'
      })
      rows.forEach((row, rowIndex) => {
        const value = Number(group.matrix?.[rowIndex]?.[labelIndex] || 0)
        row[field] = valueFormatter(value)
      })
      fieldIndex += 1
    })
  })

  return { columns, rows }
}

const formatStatusValue = (value: number) => (value >= 0.5 ? '运行' : '停止')
const formatCompLevelValue = (value: number) => formatNumber(value)

const tankTable = computed(() => {
  const columns: TableColumn[] = [
    { prop: 'time', label: '时间', minWidth: 100 }
  ]

  tankLevelData.value.series.forEach((series) => {
    columns.push({
      prop: series.name,
      label: `${series.name}(m)`,
      minWidth: 140,
      align: 'center'
    })
  })

  const rowCount = Math.max(
    tankLevelData.value.hours.length,
    ...tankLevelData.value.series.map((series) => series.values.length),
    0
  )

  const rows = Array.from({ length: rowCount }, (_, index) => {
    const row: Record<string, string | number> = {
      time: `${tankLevelData.value.hours[index] ?? index}h`
    }
    tankLevelData.value.series.forEach((series) => {
      row[series.name] = formatNumber(series.values[index])
    })
    return row
  })

  return { columns, rows }
})

const chartBase = {
  backgroundColor: 'transparent',
  textStyle: { color: '#fff' },
  tooltip: { trigger: 'axis' },
  grid: { left: 92, right: 42, top: 56, bottom: 40, containLabel: true }
}

const lineXAxis = computed(() => ({
  type: 'category',
  data: xAxisData.value,
  axisLabel: { color: '#fff' },
  axisLine: { lineStyle: { color: '#8fa4cc' } }
}))

const lineYAxis = (name: string) => ({
  type: 'value',
  name,
  nameLocation: 'middle',
  nameGap: 68,
  nameTextStyle: { color: '#fff' },
  axisLabel: { color: '#fff' },
  axisLine: { lineStyle: { color: '#8fa4cc' } },
  splitLine: { lineStyle: { color: 'rgba(255,255,255,0.12)' } }
})

const linePowerOption = computed(() => ({
  ...chartBase,
  grid: { left: 96, right: 56, top: 64, bottom: 40, containLabel: true },
  legend: { data: ['线路1负荷', '线路2负荷', '线路1最大需量', '线路2最大需量'], textStyle: { color: '#fff' } },
  xAxis: lineXAxis.value,
  yAxis: lineYAxis('有功功率 (kW)'),
  series: [
    {
      name: '线路1负荷',
      type: 'line',
      smooth: true,
      data: hourlyRows.value.map((item) => item.Line1_kW ?? item.Hourly_Power_kW ?? 0),
      lineStyle: { color: '#d62728', width: 2.5 }
    },
    {
      name: '线路2负荷',
      type: 'line',
      smooth: true,
      data: hourlyRows.value.map((item) => item.Line2_kW ?? 0),
      lineStyle: { color: '#9467bd', width: 2.5 }
    },
    {
      name: '线路1最大需量',
      type: 'line',
      symbol: 'none',
      data: hourlyRows.value.map((item) => item.Line1_Peak_kW ?? 0),
      lineStyle: { color: '#d62728', type: 'dashed', width: 2 }
    },
    {
      name: '线路2最大需量',
      type: 'line',
      symbol: 'none',
      data: hourlyRows.value.map((item) => item.Line2_Peak_kW ?? 0),
      lineStyle: { color: '#9467bd', type: 'dashed', width: 2 }
    }
  ]
}))

const costBreakdownOption = computed(() => {
  const energy = totalEnergyCost.value
  const line1Peak = Math.max(...hourlyRows.value.map((item) => item.Line1_Peak_kW ?? 0), 0)
  const line2Peak = Math.max(...hourlyRows.value.map((item) => item.Line2_Peak_kW ?? 0), 0)
  const demandPrice = 46.75
  const demand = Number((line1Peak + line2Peak) * demandPrice)
  return {
    backgroundColor: 'transparent',
    tooltip: { trigger: 'item' },
    xAxis: {
      type: 'category',
      data: ['电度电费', '基本电费'],
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#8fa4cc' } }
    },
    yAxis: {
      type: 'value',
      name: '费用 (元)',
      nameLocation: 'middle',
      nameGap: 56,
      nameTextStyle: { color: '#fff' },
      axisLabel: { color: '#fff' },
      splitLine: { lineStyle: { color: 'rgba(255,255,255,0.12)' } }
    },
    grid: { left: '8%', right: '4%', top: '10%', bottom: '12%', containLabel: true },
    series: [
      {
        type: 'bar',
        data: [
          { value: energy, itemStyle: { color: '#1f77b4' } },
          { value: demand, itemStyle: { color: '#ff7f0e' } }
        ],
        barWidth: '42%'
      }
    ]
  }
})

const compressorSeriesMeta = computed(() => [
  { key: 'Comp1_Level' as const, color: '#2E86AB' },
  { key: 'Comp2_Level' as const, color: '#A23B72' },
  { key: 'Comp3_Level' as const, color: '#5bb85d' }
].filter((_, index) => compLabels.value.length > index))

const compChartOption = computed(() => ({
  ...chartBase,
  legend: { data: compLabels.value.slice(0, 3), textStyle: { color: '#fff' } },
  xAxis: lineXAxis.value,
  yAxis: lineYAxis('压缩机挡位 (-)'),
  series: compressorSeriesMeta.value.map((meta, index) => ({
    name: compLabels.value[index] || `Comp${index + 1}`,
    type: 'line',
    smooth: true,
    data: hourlyRows.value.map((item) => item[meta.key] ?? 0),
    lineStyle: { color: meta.color, width: 2.5 },
    itemStyle: { color: meta.color },
    areaStyle: { opacity: 0.22 }
  }))
}))

const tankLevelOption = computed(() => ({
  ...chartBase,
  legend: { data: tankLevelData.value.series.map((item) => item.name), textStyle: { color: '#fff' } },
  xAxis: lineXAxis.value,
  yAxis: lineYAxis('液位 (m)'),
  series: tankLevelData.value.series.map((item, index) => ({
    name: item.name,
    type: 'line',
    smooth: true,
    data: item.values,
    lineStyle: { color: ['#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de', '#3ba272'][index % 6], width: 2.2 },
    itemStyle: { color: ['#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de', '#3ba272'][index % 6] }
  }))
}))

const bogOption = computed(() => ({
  ...chartBase,
  legend: { data: ['机理BOG', '预测BOG'], textStyle: { color: '#fff' } },
  xAxis: lineXAxis.value,
  yAxis: lineYAxis('BOG (kg/h)'),
  series: [
    {
      name: '机理BOG',
      type: 'line',
      smooth: true,
      data: bogData.value.bog_mech_kgph || [],
      lineStyle: { color: '#1f77b4', type: 'dashed', width: 2 }
    },
    {
      name: '预测BOG',
      type: 'line',
      smooth: true,
      data: bogData.value.bog_pred_kgph || [],
      lineStyle: { color: '#ff7f0e', width: 2.5 }
    }
  ]
}))

const lpPressureOption = computed(() => ({
  ...chartBase,
  grid: { left: 112, right: 36, top: 44, bottom: 40, containLabel: true },
  xAxis: lineXAxis.value,
  yAxis: lineYAxis('低压目标压力 (MPa)'),
  series: [
    {
      name: '低压目标压力',
      type: 'line',
      smooth: true,
      data: hourlyRows.value.map((item) => item.Ideal_LP_Pressure_MPa ?? 0),
      lineStyle: { color: '#5470c6', width: 2.5 }
    }
  ]
}))

const hpFlowOption = computed(() => ({
  ...chartBase,
  grid: { left: 84, right: 108, top: 64, bottom: 40, containLabel: true },
  legend: { data: ['实际外输', '单台高压泵流量'], textStyle: { color: '#fff' } },
  xAxis: lineXAxis.value,
  yAxis: [lineYAxis('外输量 (m3/h)'), lineYAxis('单台高压泵流量 (m3/h)')],
  series: [
    {
      name: '实际外输',
      type: 'line',
      smooth: true,
      data: hourlyRows.value.map((item) => item.Actual_LNG_Output_m3h ?? 0),
      lineStyle: { color: '#22c55e', width: 2.5 }
    },
    {
      name: '单台高压泵流量',
      type: 'line',
      yAxisIndex: 1,
      smooth: true,
      data: hourlyRows.value.map((item) => item.HP_Flow_perPump_m3h ?? 0),
      lineStyle: { color: '#91cc75', width: 2.5 }
    }
  ]
}))

const seawaterCountOption = computed(() => ({
  ...chartBase,
  legend: { data: ['大海水泵', '小海水泵'], textStyle: { color: '#fff' } },
  xAxis: lineXAxis.value,
  yAxis: lineYAxis('运行台数'),
  series: [
    {
      name: '大海水泵',
      type: 'line',
      smooth: true,
      data: hourlyRows.value.map((item) => item.SW_Big ?? 0),
      lineStyle: { color: '#4f88ff', width: 2.5 }
    },
    {
      name: '小海水泵',
      type: 'line',
      smooth: true,
      data: hourlyRows.value.map((item) => item.SW_Small ?? 0),
      lineStyle: { color: '#ff8a24', width: 2.5 }
    }
  ]
}))

const orvCountOption = computed(() => ({
  ...chartBase,
  xAxis: lineXAxis.value,
  yAxis: lineYAxis('运行台数'),
  series: [
    {
      name: 'ORV运行台数',
      type: 'line',
      smooth: true,
      data: hourlyRows.value.map((item) => item.ORV_Count ?? 0),
      lineStyle: { color: '#2df2b2', width: 2.5 }
    }
  ]
}))

const createHeatmapOption = (matrix: number[][], labels: string[], isLevel = false, valueLabel?: string) => {
  const timeAxis = getHeatmapXAxisData(matrix.length)
  const hours = timeAxis
  const data: Array<[number, number, number]> = []
  const displayLabel = valueLabel || (isLevel ? '负载' : '状态')

  matrix.forEach((row, hourIndex) => {
    row.forEach((value, deviceIndex) => {
      data.push([hourIndex, deviceIndex, value])
    })
  })

  return {
    backgroundColor: 'transparent',
    tooltip: {
      position: 'bottom',
      formatter: (params: any) =>
        `时间：${hours[params.value[0]] || '-'}<br/>设备：${labels[params.value[1]] || '-'}<br/>${displayLabel}：${params.value[2]}`
    },
    grid: { left: 12, right: 8, top: '8%', bottom: '12%', containLabel: true },
    xAxis: {
      type: 'category',
      data: timeAxis,
      axisLabel: {
        color: '#fff',
        interval: 0,
        formatter: (value: string) => value.endsWith(':00') ? value : ''
      },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: {
      type: 'category',
      data: labels,
      axisLabel: {
        color: '#fff',
        margin: 8
      },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    visualMap: {
      min: 0,
      max: 1,
      calculable: false,
      orient: 'horizontal',
      left: 'center',
      bottom: '0%',
      show: isLevel,
      inRange: {
        color: isLevel ? ['#440154', '#3b528b', '#21918c', '#5ec962', '#fde725'] : ['#ffffff', '#1f77b4']
      },
      textStyle: { color: '#fff' }
    },
    series: [
      {
        type: 'heatmap',
        data,
        label: { show: false },
        itemStyle: { borderColor: '#666', borderWidth: 0.5 }
      }
    ]
  }
}

const lpHeatmapOption = computed(() =>
  createHeatmapOption(resultRef.value?.unitized?.LP || [], lpLabels.value)
)

const hpHeatmapOption = computed(() =>
  createHeatmapOption(resultRef.value?.unitized?.HP || [], hpLabels.value)
)

const swBigHeatmapOption = computed(() => createHeatmapOption(resultRef.value?.unitized?.SW_Big || [], swBigLabels.value))
const swSmallHeatmapOption = computed(() =>
  createHeatmapOption(resultRef.value?.unitized?.SW_Small || [], swSmallLabels.value)
)
const orvHeatmapOption = computed(() =>
  createHeatmapOption(resultRef.value?.unitized?.ORV || [], orvLabels.value)
)
const compHeatmapOption = computed(() => createHeatmapOption(resultRef.value?.unitized?.Comp || [], compLabels.value, true, '挡位'))

const chartGroups = computed<Record<ViewKey, ChartCard[]>>(() => ({
  overview: [
    { key: 'energy', title: '总电费' },
    { key: 'line-power', title: '双供电线路负荷与最大需量', option: linePowerOption.value, panelClass: 'tall-chart' },
    { key: 'cost-breakdown', title: '电费构成', option: costBreakdownOption.value, panelClass: 'tall-chart' }
  ],
  compressor: [
    { key: 'comp-line', title: '压缩机挡位趋势', option: compChartOption.value },
    { key: 'comp-heatmap', title: '压缩机档位热力图', option: compHeatmapOption.value }
  ],
  tank: [
    { key: 'tank-level', title: '储罐液位变化', option: tankLevelOption.value },
    { key: 'bog-line', title: 'BOG变化趋势', option: bogOption.value }
  ],
  lp: [
    { key: 'lp-pressure', title: '低压泵目标压力', option: lpPressureOption.value },
    { key: 'lp-heatmap', title: '低压泵启停状态', option: lpHeatmapOption.value }
  ],
  hp: [
    { key: 'hp-flow', title: '高压泵外输与单泵流量', option: hpFlowOption.value },
    { key: 'hp-heatmap', title: '高压泵启停状态', option: hpHeatmapOption.value }
  ],
  seawater: [
    { key: 'sw-count', title: '海水泵运行台数', option: seawaterCountOption.value, panelClass: 'tall-chart' },
    { key: 'sw-big-heatmap', title: '海水大泵启停状态', option: swBigHeatmapOption.value, panelClass: 'tall-chart' },
    { key: 'sw-small-heatmap', title: '海水小泵启停状态', option: swSmallHeatmapOption.value, panelClass: 'tall-chart' }
  ],
  orv: [
    { key: 'orv-count', title: 'ORV运行台数', option: orvCountOption.value },
    { key: 'orv-heatmap', title: 'ORV启停状态', option: orvHeatmapOption.value }
  ]
}))

const activeCharts = computed(() => {
  const charts = chartGroups.value[activeView.value]
  if (activeView.value === 'tank') {
    return charts.filter((chart) => chart.key !== 'bog-line')
  }
  return charts
})

const overviewRows = computed(() =>
  hourlyRows.value.map((item) => ({
    hour: `${item.Hour}h`,
    line1Kw: formatNumber(item.Line1_kW ?? item.Hourly_Power_kW),
    line2Kw: formatNumber(item.Line2_kW),
    line1Peak: formatNumber(item.Line1_Peak_kW),
    line2Peak: formatNumber(item.Line2_Peak_kW),
    hourlyCost: formatNumber(item.Hourly_Cost_CNY),
    elecPrice: formatNumber(item.Elec_Price, 4)
  }))
)

const compressorRows = computed(() =>
  hourlyRows.value.map((item) => ({
    hour: `${item.Hour}h`,
    comp1Level: formatNumber(item.Comp1_Level),
    comp2Level: formatNumber(item.Comp2_Level),
    comp3Level: formatNumber(item.Comp3_Level)
  }))
)

const tankRows = computed(() =>
  hourlyRows.value.map((item, index) => ({
    hour: `${item.Hour}h`,
    tankLevels: tankLevelData.value.series.map((series) => `${series.name}:${formatNumber(series.values[index])}`).join(' / '),
    bogMech: formatNumber(bogData.value.bog_mech_kgph?.[index]),
    bogPred: formatNumber(bogData.value.bog_pred_kgph?.[index]),
    unload: formatNumber(item.Unload_m3h)
  }))
)

const lpRows = computed(() =>
  hourlyRows.value.map((item, index) => ({
    hour: `${item.Hour}h`,
    pressure: formatNumber(item.Ideal_LP_Pressure_MPa),
    flowPerPump: formatNumber(item.LP_Flow_perPump_m3h),
    runningCount: getHourlyActiveCount(resultRef.value?.unitized?.LP, index)
  }))
)

const hpRows = computed(() =>
  hourlyRows.value.map((item) => ({
    hour: `${item.Hour}h`,
    flowPerPump: formatNumber(item.HP_Flow_perPump_m3h),
    actualOutput: formatNumber(item.Actual_LNG_Output_m3h),
    pressure: formatNumber(item.Ideal_HP_Pressure_MPa)
  }))
)

const seawaterRows = computed(() =>
  hourlyRows.value.map((item, index) => ({
    hour: `${item.Hour}h`,
    bigCount: item.SW_Big ?? getHourlyActiveCount(resultRef.value?.unitized?.SW_Big, index),
    smallCount: item.SW_Small ?? getHourlyActiveCount(resultRef.value?.unitized?.SW_Small, index)
  }))
)

const orvRows = computed(() =>
  hourlyRows.value.map((item, index) => ({
    hour: `${item.Hour}h`,
    runningCount: item.ORV_Count ?? getHourlyActiveCount(resultRef.value?.unitized?.ORV, index)
  }))
)

const compressorStepTable = computed(() =>
  buildStepTableData(resultRef.value?.unitized?.Comp, compLabels.value, formatCompLevelValue)
)

const lpStepTable = computed(() =>
  buildStepTableData(resultRef.value?.unitized?.LP, lpLabels.value, formatStatusValue)
)

const hpStepTable = computed(() =>
  buildStepTableData(resultRef.value?.unitized?.HP, hpLabels.value, formatStatusValue)
)

const seawaterStepTable = computed(() =>
  buildCombinedStepTableData(
    [
      { matrix: resultRef.value?.unitized?.SW_Big, labels: swBigLabels.value },
      { matrix: resultRef.value?.unitized?.SW_Small, labels: swSmallLabels.value }
    ],
    formatStatusValue
  )
)

const orvStepTable = computed(() =>
  buildStepTableData(resultRef.value?.unitized?.ORV, orvLabels.value, formatStatusValue)
)

const tableConfigMap = computed<Record<ViewKey, { title: string; columns: TableColumn[]; rows: Array<Record<string, string | number>> }>>(() => ({
  overview: {
    title: '电费明细',
    columns: [
      { prop: 'hour', label: '时间', minWidth: 90 },
      { prop: 'line1Kw', label: '线路1负荷(kW)', minWidth: 120 },
      { prop: 'line2Kw', label: '线路2负荷(kW)', minWidth: 120 },
      { prop: 'line1Peak', label: '线路1最大需量', minWidth: 120 },
      { prop: 'line2Peak', label: '线路2最大需量', minWidth: 120 },
      { prop: 'hourlyCost', label: '小时成本(元)', minWidth: 120 },
      { prop: 'elecPrice', label: '电价', minWidth: 90 }
    ],
    rows: overviewRows.value
  },
  compressor: {
    title: '压缩机明细',
    columns: [
      { prop: 'hour', label: '时间', minWidth: 90 },
      { prop: 'comp1Level', label: `${compLabels.value[0] || '压缩机1'}档位`, minWidth: 180 },
      { prop: 'comp2Level', label: `${compLabels.value[1] || '压缩机2'}档位`, minWidth: 180 },
      { prop: 'comp3Level', label: `${compLabels.value[2] || '压缩机3'}档位`, minWidth: 180 }
    ],
    rows: compressorRows.value
  },
  tank: {
    title: '罐存与BOG明细',
    columns: [
      { prop: 'hour', label: '时间', minWidth: 90 },
      { prop: 'tankLevels', label: '各储罐液位(m)', minWidth: 130, align: 'left' },
      { prop: 'bogMech', label: '机理BOG', minWidth: 110 },
      { prop: 'bogPred', label: '预测BOG', minWidth: 110 },
      { prop: 'unload', label: '卸船量(m3/h)', minWidth: 120 }
    ],
    rows: tankRows.value
  },
  lp: {
    title: '低压泵明细',
    columns: [
      { prop: 'hour', label: '时间', minWidth: 90 },
      { prop: 'pressure', label: '压力(MPa)', minWidth: 110 },
      { prop: 'flowPerPump', label: '单泵流量', minWidth: 110 },
      { prop: 'runningCount', label: '运行台数', minWidth: 90 }
    ],
    rows: lpRows.value
  },
  hp: {
    title: '高压泵明细',
    columns: [
      { prop: 'hour', label: '时间', minWidth: 90 },
      { prop: 'pressure', label: '压力(MPa)', minWidth: 110 },
      { prop: 'flowPerPump', label: '单泵流量', minWidth: 110 },
      { prop: 'actualOutput', label: '实际外输量', minWidth: 120 }
    ],
    rows: hpRows.value
  },
  seawater: {
    title: '海水泵明细',
    columns: [
      { prop: 'hour', label: '时间', minWidth: 90 },
      { prop: 'bigCount', label: '大泵台数', minWidth: 100 },
      { prop: 'smallCount', label: '小泵台数', minWidth: 100 }
    ],
    rows: seawaterRows.value
  },
  orv: {
    title: 'ORV明细',
    columns: [
      { prop: 'hour', label: '时间', minWidth: 90 },
      { prop: 'runningCount', label: '运行台数', minWidth: 100 }
    ],
    rows: orvRows.value
  }
}))

const ganttTableConfigMap = computed<Partial<Record<ViewKey, { columns: TableColumn[]; rows: Array<Record<string, string | number>> }>>>(() => ({
  tank: {
    columns: tankTable.value.columns,
    rows: tankTable.value.rows
  },
  compressor: {
    columns: compressorStepTable.value.columns,
    rows: compressorStepTable.value.rows
  },
  lp: {
    columns: lpStepTable.value.columns,
    rows: lpStepTable.value.rows
  },
  hp: {
    columns: hpStepTable.value.columns,
    rows: hpStepTable.value.rows
  },
  seawater: {
    columns: seawaterStepTable.value.columns,
    rows: seawaterStepTable.value.rows
  },
  orv: {
    columns: orvStepTable.value.columns,
    rows: orvStepTable.value.rows
  }
}))

const activeTableColumns = computed(() =>
  ganttTableConfigMap.value[activeView.value]?.columns || tableConfigMap.value[activeView.value].columns
)
const activeTableRows = computed(() =>
  ganttTableConfigMap.value[activeView.value]?.rows || tableConfigMap.value[activeView.value].rows
)

const handleConfirm = async () => {
  if (!planName.value.trim()) {
    ElMessage.warning('请输入方案名称')
    return
  }

  const result = schemeStore.simulationResult as SimulationResult | null
  if (!result) {
    ElMessage.warning('当前还没有可保存的调度结果')
    return
  }

  const rawUserInfo = localStorage.getItem(USER_INFO_KEY)
  const userInfo = rawUserInfo ? JSON.parse(rawUserInfo) : null
  const createdBy = Number(userInfo?.userId || 0)

  if (!createdBy) {
    ElMessage.warning('当前登录用户信息缺失，请重新登录后再保存')
    return
  }

  saving.value = true
  try {
    const {
      planName,
      conditionName,
      remark,
      targetOutputM3,
      lpTargetPressure,
      hpTargetPressure,
      initialLiquidLevel,
      startTime,
      dailyDemandCurve
    } = schemeStore.schemeParams
    const response = await saveSchedule({
      createdBy,
      planName: planName.trim(),
      constraintsJson: {
        dailyDemandCurve
      },
      calculateInput: {
        planName: planName.trim(),
        conditionName: conditionName.trim(),
        remark: remark || '',
        targetOutputM3,
        lpTargetPressure,
        hpTargetPressure,
        initialLiquidLevel,
        startTime,
        dailyDemandCurve,
        calculationMode: toPriorityMode(schemeStore.schemeParams.calculationMode)
      },
      simulationResult: result
    })

    if (response.data.code !== 200) {
      throw new Error(response.data.message || '保存调度方案失败')
    }

    const savedPlanCode = response.data.data?.planCode
    ElMessage.success(savedPlanCode ? `调度方案保存成功：${savedPlanCode}` : '调度方案保存成功')
  } catch (error: any) {
    ElMessage.error(error.message || '保存调度方案失败')
  } finally {
    saving.value = false
  }
}

const appendSheet = (workbook: XLSX.WorkBook, rows: Array<Record<string, string | number>>, sheetName: string) => {
  const sheet = XLSX.utils.json_to_sheet(rows)
  XLSX.utils.book_append_sheet(workbook, sheet, sheetName)
}

const handleExport = () => {
  const result = resultRef.value
  if (!result) {
    ElMessage.warning('当前还没有可导出的外输优化结果')
    return
  }

  const workbook = XLSX.utils.book_new()
  appendSheet(workbook, overviewRows.value, 'line_power_log_2h')
  appendSheet(workbook, tankRows.value, 'tank_timeseries')
  appendSheet(workbook, lpRows.value, 'lp_onoff')
  appendSheet(workbook, hpRows.value, 'hp_onoff')
  appendSheet(workbook, seawaterRows.value, 'sw_onoff')
  appendSheet(workbook, orvRows.value, 'orv_onoff')
  appendSheet(workbook, compressorRows.value, 'compressor_levels')
  XLSX.writeFile(workbook, '调度方案结果.xlsx')
  ElMessage.success('外输优化结果已导出')
}

const formatNumber = (value: number | null | undefined, digits = 2) =>
  value === null || value === undefined || Number.isNaN(value) ? '-' : Number(value).toFixed(digits)

const applySimulationResult = (result: SimulationResult | null) => {
  resultRef.value = result
}

onMounted(() => {
  if (schemeStore.simulationResult) {
    applySimulationResult(schemeStore.simulationResult as SimulationResult)
  }
})

watch(
  () => schemeStore.simulationResult,
  (value) => {
    if (value) {
      applySimulationResult(value as SimulationResult)
    }
  },
  { immediate: true, deep: true }
)
</script>

<style scoped lang="scss">
.scheme-page {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.toolbar-panel {
  height: 56px;
  display: flex;
  align-items: center;
  padding: 0 10px;
}

.toolbar-inline {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.toolbar-inline .label {
  color: #fff;
  font-size: 14px;
  white-space: nowrap;
}

.scheme-content {
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
  min-height: 0;
}

.rightBox {
  width: 42%;
}

.view-switch {
  margin-bottom: 10px;
}

.scheme-segmented {
  --el-segmented-bg-color: rgba(7, 31, 52, 0.92);
  --el-segmented-item-selected-bg-color: linear-gradient(90deg, #157eff, #18d2ff);
  --el-segmented-item-selected-color: #ffffff;
  --el-border-radius-base: 4px;
}

.scheme-segmented :deep(.el-segmented) {
  padding: 4px;
  border: 1px solid rgba(0, 200, 255, 0.42);
  box-shadow: 0 0 0 1px rgba(0, 200, 255, 0.12) inset, 0 0 10px rgba(0, 200, 255, 0.18);
}

.scheme-segmented :deep(.el-segmented__item) {
  color: rgba(217, 236, 255, 0.78);
  border-radius: 4px;
}

.scheme-segmented :deep(.el-segmented__item:hover) {
  color: #ffffff;
}

.scheme-segmented :deep(.el-segmented__item-selected) {
  color: #ffffff;
  box-shadow: 0 8px 18px rgba(24, 210, 255, 0.24);
}

.chart-scroll {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.chart-stack {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding-right: 4px;
}

.chart-panel {
  flex: 0 0 auto;
  height: clamp(280px, 32vh, 420px);
}

.chart-panel.tall-chart {
  height: clamp(360px, 42vh, 560px);
}

.table-panel {
  height: 100%;
}

.chart-panel :deep(.panelBox) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.chart-panel :deep(.panelBody) {
  flex: 1;
  min-height: 0;
  display: flex;
  align-items: stretch;
  justify-content: stretch;
  padding: 0;
  overflow: visible !important;
}

.table-panel :deep(.panelBox) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

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

.chart-canvas > :deep(*) {
  width: 100%;
  height: 100% !important;
}

.chart-canvas :deep(.chart-echarts) {
  width: 100%;
  height: 100% !important;
}

.energy-display {
  flex: 1;
  min-height: 0;
  width: 100%;
  height: 100%;
  display: grid;
  place-items: center;
  align-content: center;
  justify-items: center;
  gap: 10px;
  text-align: center;
}

.energy-value {
  font-size: 46px;
  font-weight: 700;
  color: #67c23a;
}

.energy-unit {
  font-size: 18px;
  color: #fff;
}
</style>
