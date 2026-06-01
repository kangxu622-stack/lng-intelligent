<template>
  <div class="equipment-monitor-page">
    <section class="tab-strip">
      <el-tabs v-model="activeTab" class="monitor-tabs" @tab-change="handleTabChange">
        <el-tab-pane v-for="tab in tabs" :key="tab.code" :label="tab.label" :name="tab.code" />
      </el-tabs>
    </section>

      <cm-panel style="height: 56px; display: flex; align-items: center;" class="mb10">
        <el-button type="primary" plain>设备监测</el-button>
        <el-button type="primary" @click="exportCurrentTable">导出报表</el-button>
        <el-button type="primary" @click="goGenerate">调度方案</el-button>
        <el-button type="primary" :loading="loading" @click="loadMonitoringData()">刷新数据</el-button>
          <span class="toolbar-label">设备</span>
          <el-select
            v-model="selectedEquipmentId"
            class="device-select cm-inline-control cm-dark-select"
            popper-class="cm-dark-select-popper"
            size="small"
            placeholder="请选择设备"
            @change="handleDeviceChange"
          >
            <el-option
              v-for="item in currentTabItems"
              :key="item.equipmentId"
              :label="item.equipmentCode"
              :value="item.equipmentId"
            />
          </el-select>
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            clearable
            range-separator="-"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            class="date-range"
            @change="handleDateRangeChange"
          />
          <el-button type="primary" :loading="trendLoading" @click="loadTrendData" style="margin-left: 15px;">查询趋势</el-button>

    </cm-panel>

    <pagePanelNew class="monitor-content">
      <div class="content-layout">
        <div class="leftBox">
          <pagePanel
            :header-title="selectedEquipment?.equipmentCode || '请选择设备'"
            class="chart-panel"
          >
            <template #default>
              <div class="panel-subtitle chart-subtitle">
                {{ selectedEquipment?.equipmentName || '选择设备后可查看实时与历史监测趋势' }}
              </div>

              <div class="chart-list">
                <div v-for="metric in metricCards" :key="metric.key" class="chart-shell">
                  <div class="chart-card-header">{{ metric.label }}</div>
                  <scEcharts height="100%" :option="metric.option" />
                </div>
              </div>
            </template>
          </pagePanel>
        </div>

        <div class="rightBox">
          <div class="panel-surface table-panel">
            <div class="panel-header panel-header-inline">
              <div class="panel-title">{{ currentTabLabel }}</div>
              <div class="table-summary">
                <span>在线 {{ onlineCount }}</span>
                <span>离线 {{ offlineCount }}</span>
              </div>
            </div>

            <el-table
              v-loading="loading"
              :data="tableRows"
              :border="true"
              height="100%"
              class="monitor-table"
              highlight-current-row
              :span-method="spanMethod"
              @row-click="handleRowClick"
            >
              <el-table-column prop="processArea" label="工艺区块" min-width="120" />
              <el-table-column prop="equipmentCode" label="设备编号" min-width="120" />
              <el-table-column label="状态" width="90" align="center">
                <template #default="{ row }">
                  <span class="status-dot" :class="row.isOnline ? 'online' : 'offline'" />
                </template>
              </el-table-column>
              <el-table-column label="流量(m3/h)" min-width="120" align="right">
                <template #default="{ row }">{{ formatMetric(row.flowRate) }}</template>
              </el-table-column>
              <el-table-column label="压力(MPa)" min-width="120" align="right">
                <template #default="{ row }">{{ formatMetric(row.pressure) }}</template>
              </el-table-column>
              <el-table-column label="功率(kW)" min-width="110" align="right">
                <template #default="{ row }">{{ formatMetric(row.currentPower) }}</template>
              </el-table-column>
              <el-table-column prop="remark" label="备注" min-width="140" show-overflow-tooltip />
            </el-table>
          </div>
        </div>
      </div>
    </pagePanelNew>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: 'EquipmentStatus'
})

import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { HubConnection, HubConnectionBuilder, HubConnectionState, HttpTransportType, LogLevel } from '@microsoft/signalr'
import { ElMessage } from 'element-plus'
import * as XLSX from 'xlsx'
import { saveAs } from 'file-saver'
import scEcharts from '@/components/scEcharts/index.vue'
import { getRealtimeStatus, type TagSnapshot } from '@/api/realtime'
import {
  getEquipmentMonitoring,
  getEquipmentMonitoringTrend,
  type EquipmentMonitoringGroup,
  type EquipmentMonitoringItem,
  type EquipmentMonitoringTrendPoint
} from '@/api/equipment'

type MonitorRow = EquipmentMonitoringItem & {
  processAreaRowSpan: number
}

type MetricKey = 'flowRate' | 'pressure' | 'currentPower' | 'temperature' | 'liquidLevel'

const router = useRouter()

const tabs = [
  { code: 'LP_PUMP', label: '低压泵' },
  { code: 'HP_PUMP', label: '高压泵' },
  { code: 'ORV_CLUSTER', label: 'ORV 气化器' },
  { code: 'BOG_COMPRESSOR', label: 'BOG 压缩机' },
  { code: 'RECONDENSER', label: '再冷凝器' },
  { code: 'SEAWATER_PUMP', label: '海水泵' }
]

const metricConfigs: Array<{ key: MetricKey; label: string; unit: string; color: string }> = [
  { key: 'flowRate', label: '流量', unit: 'm3/h', color: '#18d2ff' },
  { key: 'pressure', label: '压力', unit: 'MPa', color: '#42f59b' },
  { key: 'currentPower', label: '功率', unit: 'kW', color: '#ffd166' },
  { key: 'temperature', label: '温度', unit: '°C', color: '#ff8c69' },
  { key: 'liquidLevel', label: '液位', unit: '%', color: '#9b8cff' }
]

const activeTab = ref('LP_PUMP')
const groups = ref<EquipmentMonitoringGroup[]>([])
const loading = ref(false)
const trendLoading = ref(false)
const selectedEquipmentId = ref<number | null>(null)
const trendPoints = ref<EquipmentMonitoringTrendPoint[]>([])
const realtimePoints = ref<EquipmentMonitoringTrendPoint[]>([])
const dateRange = ref<[string, string] | [] | null>([])

let timer: number | null = null
let realtimeConnection: HubConnection | null = null
let realtimeFlushTimer: number | null = null
let pendingRealtimeTimestamp: string | undefined
let realtimeWarningShown = false
const realtimeSnapshots = new Map<string, TagSnapshot>()
const maxRealtimePoints = 60

const formatDateTime = (value: string, withSeconds = false) => {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  const hour = `${date.getHours()}`.padStart(2, '0')
  const minute = `${date.getMinutes()}`.padStart(2, '0')
  const second = `${date.getSeconds()}`.padStart(2, '0')
  return withSeconds
    ? `${year}-${month}-${day} ${hour}:${minute}:${second}`
    : `${hour}:${minute}:${second}`
}

const formatMetric = (value: number | null | undefined) => {
  if (value === null || value === undefined) return '-'
  return Number(value).toFixed(2)
}

const currentTabGroups = computed(() => groups.value.filter((group) => group.groupCode === activeTab.value))
const currentTabItems = computed(() => currentTabGroups.value.flatMap((group) => group.items))
const hasDateRange = computed(() => {
  if (!Array.isArray(dateRange.value)) {
    return false
  }

  return dateRange.value.length === 2 && Boolean(dateRange.value[0]) && Boolean(dateRange.value[1])
})
const currentTabLabel = computed(() => tabs.find((item) => item.code === activeTab.value)?.label || '设备状态')

const selectedEquipment = computed(
  () => currentTabItems.value.find((item) => item.equipmentId === selectedEquipmentId.value) || null
)
const chartPoints = computed(() => (hasDateRange.value ? trendPoints.value : realtimePoints.value))

const onlineCount = computed(() => currentTabItems.value.filter((item) => item.isOnline).length)
const offlineCount = computed(() => currentTabItems.value.length - onlineCount.value)

const tableRows = computed<MonitorRow[]>(() => {
  const rows: MonitorRow[] = []

  currentTabGroups.value.forEach((group) => {
    group.items.forEach((item, index) => {
      rows.push({
        ...item,
        processAreaRowSpan: index === 0 ? group.items.length : 0
      })
    })
  })

  return rows
})

const createTrendOption = (key: MetricKey, label: string, unit: string, color: string) => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'axis',
    formatter: (params: any[]) => {
      const first = params?.[0]
      if (!first) {
        return ''
      }

      const point = chartPoints.value[first.dataIndex]
      const value = point?.[key]
      return `${formatDateTime(point?.timestamp || '', true)}<br/>${label}: ${formatMetric(value)} ${unit}`
    }
  },
  grid: { left: '4%', right: '4%', top: 36, bottom: 44, containLabel: true },
  xAxis: {
    type: 'category',
    data: chartPoints.value.map((item) => formatDateTime(item.timestamp)),
    axisLabel: {
      color: '#8fa4cc',
      fontSize: 12,
      hideOverlap: true,
      margin: 14
    },
    axisLine: { lineStyle: { color: '#8fa4cc' } },
    splitLine: { show: true, lineStyle: { color: 'rgba(143, 164, 204, 0.14)' } }
  },
  yAxis: {
    type: 'value',
    name: `${label} (${unit})`,
    nameTextStyle: { color: '#8fa4cc' },
    axisLabel: { color: '#8fa4cc' },
    axisLine: { lineStyle: { color: '#8fa4cc' } },
    splitLine: { lineStyle: { color: 'rgba(143, 164, 204, 0.16)' } }
  },
  series: [
    {
      type: 'line',
      smooth: true,
      showSymbol: false,
      data: chartPoints.value.map((item) => item[key]),
      lineStyle: { color, width: 2.5 },
      itemStyle: { color },
      areaStyle: { color: `${color}24` }
    }
  ]
})

const metricCards = computed(() =>
  metricConfigs.map((metric) => ({
    ...metric,
    option: createTrendOption(metric.key, metric.label, metric.unit, metric.color)
  }))
)

const spanMethod = ({ row, columnIndex }: { row: MonitorRow; columnIndex: number }) => {
  if (columnIndex === 0) {
    return {
      rowspan: row.processAreaRowSpan,
      colspan: row.processAreaRowSpan > 0 ? 1 : 0
    }
  }

  return { rowspan: 1, colspan: 1 }
}

const readRealtimeMetric = (tagName?: string | null) => {
  if (!tagName) {
    return null
  }

  const snapshot = realtimeSnapshots.get(tagName)
  return snapshot ? Number(snapshot.value.toFixed(2)) : null
}

const buildRealtimePoint = (timestamp?: string): EquipmentMonitoringTrendPoint | null => {
  if (!selectedEquipment.value) {
    return null
  }

  const { tagBindings } = selectedEquipment.value

  return {
    timestamp: timestamp || selectedEquipment.value.updateTime || new Date().toISOString(),
    flowRate: readRealtimeMetric(tagBindings?.flowRateTagName) ?? selectedEquipment.value.flowRate,
    pressure: readRealtimeMetric(tagBindings?.pressureTagName) ?? selectedEquipment.value.pressure,
    currentPower: readRealtimeMetric(tagBindings?.currentPowerTagName) ?? selectedEquipment.value.currentPower,
    temperature: readRealtimeMetric(tagBindings?.temperatureTagName) ?? selectedEquipment.value.temperature,
    liquidLevel: readRealtimeMetric(tagBindings?.liquidLevelTagName) ?? selectedEquipment.value.liquidLevel,
    status: selectedEquipment.value.status
  }
}

const pushRealtimePoint = (timestamp?: string, replace = false) => {
  const point = buildRealtimePoint(timestamp)
  if (!point) {
    realtimePoints.value = []
    return
  }

  realtimePoints.value = replace
    ? [point]
    : [...realtimePoints.value.slice(-(maxRealtimePoints - 1)), point]
}

const syncRealtimeTrend = () => {
  if (hasDateRange.value) {
    return
  }

  pushRealtimePoint(undefined, realtimePoints.value.length === 0)
}

const flushRealtimePoint = (timestamp?: string) => {
  if (hasDateRange.value) {
    return
  }

  pushRealtimePoint(timestamp)
}

const resetRealtimeChart = () => {
  realtimePoints.value = []
  pendingRealtimeTimestamp = undefined

  if (realtimeFlushTimer !== null) {
    window.clearTimeout(realtimeFlushTimer)
    realtimeFlushTimer = null
  }
}

const scheduleRealtimeFlush = (timestamp?: string) => {
  pendingRealtimeTimestamp = timestamp || pendingRealtimeTimestamp

  if (realtimeFlushTimer !== null) {
    return
  }

  realtimeFlushTimer = window.setTimeout(() => {
    realtimeFlushTimer = null
    flushRealtimePoint(pendingRealtimeTimestamp)
    pendingRealtimeTimestamp = undefined
  }, 250)
}

const applyRealtimeSnapshot = (snapshot: TagSnapshot, append = true) => {
  realtimeSnapshots.set(snapshot.tagName, snapshot)
  if (!hasDateRange.value && append) {
    scheduleRealtimeFlush(snapshot.timestamp)
  }
}

const loadRealtimeSnapshot = async () => {
  try {
    const snapshots = await getRealtimeStatus()
    realtimeSnapshots.clear()
    snapshots.forEach((snapshot) => applyRealtimeSnapshot(snapshot, false))
    if (!hasDateRange.value) {
      pushRealtimePoint(snapshots[0]?.timestamp, true)
    }
  } catch (error) {
    ElMessage.error((error as Error).message || '实时数据快照加载失败')
  }
}

const ensureRealtimeConnection = async () => {
  if (realtimeConnection) {
    if (
      realtimeConnection.state === HubConnectionState.Connected ||
      realtimeConnection.state === HubConnectionState.Connecting
    ) {
      return
    }

    await realtimeConnection.start()
    return
  }

  realtimeConnection = new HubConnectionBuilder()
    .withUrl('/hubs/realtime', {
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  realtimeConnection.on('ReceiveTagValue', (snapshot: TagSnapshot) => {
    applyRealtimeSnapshot(snapshot)
  })

  await realtimeConnection.start()
}

const ensureRealtimeConnectionSafe = async () => {
  try {
    await ensureRealtimeConnection()
    realtimeWarningShown = false
  } catch (error) {
    if (!realtimeWarningShown) {
      realtimeWarningShown = true
      ElMessage.warning(`实时连接失败，当前仅显示快照数据：${(error as Error).message}`)
    }
  }
}

const stopRealtimeConnection = async () => {
  if (!realtimeConnection) {
    return
  }

  await realtimeConnection.stop()
}

const ensureSelectedEquipment = () => {
  if (!currentTabItems.value.length) {
    selectedEquipmentId.value = null
    trendPoints.value = []
    return
  }

  const exists = currentTabItems.value.some((item) => item.equipmentId === selectedEquipmentId.value)
  if (!exists) {
    selectedEquipmentId.value = currentTabItems.value[0]?.equipmentId || null
  }
}

const loadTrendData = async () => {
  if (!hasDateRange.value) {
    syncRealtimeTrend()
    return
  }

  if (!selectedEquipmentId.value) {
    trendPoints.value = []
    return
  }

  trendLoading.value = true
  try {
    const [start, end] = Array.isArray(dateRange.value) ? dateRange.value : []
    const res = await getEquipmentMonitoringTrend(selectedEquipmentId.value, { start, end })
    if (res.code !== 200 || !res.data) {
      throw new Error(res.message || '设备趋势数据获取失败')
    }

    trendPoints.value = res.data.items
  } catch (error) {
    ElMessage.error((error as Error).message)
  } finally {
    trendLoading.value = false
  }
}

const loadMonitoringData = async (silent = false) => {
  if (!silent) loading.value = true

  try {
    const res = await getEquipmentMonitoring()
    if (res.code !== 200 || !res.data) {
      throw new Error(res.message || '设备监测数据获取失败')
    }

    groups.value = res.data.groups
    ensureSelectedEquipment()

    if (hasDateRange.value) {
      if (!silent) {
        await loadTrendData()
      }
    } else {
      if (!silent) {
        await loadRealtimeSnapshot()
        await ensureRealtimeConnectionSafe()
      }
      syncRealtimeTrend()
    }
  } catch (error) {
    ElMessage.error((error as Error).message)
  } finally {
    if (!silent) loading.value = false
  }
}

const handleTabChange = async (tabCode: string | number) => {
  activeTab.value = `${tabCode}`
  ensureSelectedEquipment()
  resetRealtimeChart()
  await loadTrendData()
}

const handleDeviceChange = async () => {
  resetRealtimeChart()
  await loadTrendData()
}

const handleDateRangeChange = async () => {
  if (hasDateRange.value) {
    await stopRealtimeConnection()
    realtimePoints.value = []
  } else {
    await loadMonitoringData()
    await loadRealtimeSnapshot()
    await ensureRealtimeConnectionSafe()
  }

  await loadTrendData()
}

const handleRowClick = async (row: MonitorRow) => {
  selectedEquipmentId.value = row.equipmentId
  resetRealtimeChart()
  await loadTrendData()
}

const exportCurrentTable = () => {
  const workbook = XLSX.utils.book_new()
  const rows = tableRows.value.map((item) => ({
    工艺区块: item.processArea,
    设备编号: item.equipmentCode,
    设备名称: item.equipmentName,
    运行状态: item.status,
    流量_m3h: item.flowRate ?? '',
    压力_MPa: item.pressure ?? '',
    功率_kW: item.currentPower ?? '',
    温度_C: item.temperature ?? '',
    液位_pct: item.liquidLevel ?? '',
    更新时间: item.updateTime ?? '',
    备注: item.remark
  }))

  XLSX.utils.book_append_sheet(workbook, XLSX.utils.json_to_sheet(rows), currentTabLabel.value)
  const arrayBuffer = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' })

  saveAs(
    new Blob([arrayBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }),
    `${currentTabLabel.value}-设备状态.xlsx`
  )
}

const goGenerate = () => {
  router.push('/generate')
}

onMounted(async () => {
  await loadMonitoringData()
  timer = window.setInterval(() => {
    if (!hasDateRange.value) {
      void loadMonitoringData(true)
      void loadRealtimeSnapshot()
      void ensureRealtimeConnectionSafe()
      syncRealtimeTrend()
    }
  }, 5000)
})

onBeforeUnmount(() => {
  if (timer !== null) {
    window.clearInterval(timer)
  }
  if (realtimeFlushTimer !== null) {
    window.clearTimeout(realtimeFlushTimer)
  }

  void stopRealtimeConnection()
})
</script>

<style scoped lang="scss">
.equipment-monitor-page {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.tab-strip {
  padding: 0 6px 2px;
  margin-bottom: 10px;
  flex: 0 0 auto;
}

.monitor-tabs :deep(.el-tabs__header) {
  margin: 0;
}

.monitor-tabs :deep(.el-tabs__item) {
  color: #fff;
}

.monitor-tabs :deep(.el-tabs__nav-wrap) {
  overflow: hidden;
}

.monitor-tabs :deep(.el-tabs__nav-scroll) {
  overflow-x: auto;
  overflow-y: hidden;
  scrollbar-width: none;
}

.monitor-tabs :deep(.el-tabs__nav-scroll::-webkit-scrollbar) {
  display: none;
}

.monitor-tabs :deep(.el-tabs__nav) {
  flex-wrap: nowrap;
}

.toolbar-panel {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 10px;
  flex: 0 0 auto;
}

.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.toolbar-label {
  color: #fff;
  font-size: 14px;
  margin: 0 10px 0 15px;
}

.device-select {
  width: 160px;
  margin-right: 15px;
}

.monitor-content {
  flex: 1;
  min-height: 0;
  height: calc(100% - 40px);
}

.content-layout {
  display: flex;
  width: 100%;
  height: 100%;
  min-height: 0;
  padding: 6px 10px 10px;
}

.leftBox,
.rightBox {
  min-width: 0;
  min-height: 0;
  height: 100%;
}

.leftBox {
  width: 58%;
  margin-right: 10px;
}

.rightBox {
  width: 42%;
}

.panel-surface {
  height: 100%;
  min-height: 0;
  border: 1px solid rgba(41, 180, 255, 0.32);
  background: rgba(7, 29, 53, 0.52);
  padding: 12px;
  display: flex;
  flex-direction: column;
}

.panel-header {
  flex: 0 0 auto;
  margin-bottom: 10px;
}

.panel-header-inline {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.panel-title {
  color: #fff;
  font-size: 18px;
  font-weight: 600;
}

.panel-subtitle {
  margin-top: 6px;
  color: #8fa4cc;
  font-size: 13px;
}

.chart-list {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-rows: repeat(5, minmax(280px, 1fr));
  gap: 10px;
}

.chart-panel {
  height: 100%;
  min-height: 0;
}

.chart-panel :deep(.g-w100) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.chart-shell {
  min-height: 280px;
  border: 1px solid rgba(143, 164, 204, 0.18);
  border-radius: 4px;
  background: rgba(10, 20, 40, 0.08);
  display: flex;
  flex-direction: column;
}

.chart-card-header {
  padding: 10px 12px 0;
  font-size: 14px;
  font-weight: 600;
  color: #fff;
  flex: 0 0 auto;
}

.table-summary {
  display: flex;
  justify-content: flex-end;
  gap: 16px;
  color: #fff;
  font-size: 13px;
}

.monitor-table {
  flex: 1;
  min-height: 0;
}

.status-dot {
  display: inline-block;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: #d9d9d9;
}

.status-dot.online {
  background: #13ce66;
  box-shadow: 0 0 10px rgba(19, 206, 102, 0.36);
}

.status-dot.offline {
  background: #d9d9d9;
}

@media (max-width: 1280px) {
  .content-layout {
    flex-direction: column;
  }

  .leftBox,
  .rightBox {
    width: 100%;
  }

  .leftBox {
    margin-right: 0;
    margin-bottom: 10px;
  }

  .chart-list {
    grid-template-rows: repeat(5, minmax(280px, 1fr));
  }
}
</style>
