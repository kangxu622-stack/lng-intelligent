<template>
  <div class="cm-container-wrapper p10 initial-condition-page">
    <div class="flex1 ovh">
      <cm-panel style="height: 56px; display: flex; align-items: center;" class="mb10">
        <div class="toolbar-inline">
          <el-button type="primary" @click="handleReset">清空重置</el-button>
          <el-button type="primary" @click="handleConfirm">计算</el-button>

          <div class="toolbar-field">
            <span class="label">方案名称</span>
            <el-input
              v-model="planName"
              class="cm-inline-control"
              placeholder="请输入方案名称"
              size="small"
              style="width: 220px"
              @input="onPlanNameChange"
            />
          </div>

          <div class="toolbar-field">
            <span class="label">开始时间</span>
            <el-date-picker
              v-model="startTime"
              class="cm-inline-control"
              type="datetime"
              size="small"
              style="width: 220px"
              value-format="YYYY-MM-DD HH:mm:ss"
              format="YYYY-MM-DD HH:mm:ss"
              placeholder="请选择开始时间"
            />
          </div>
        </div>
      </cm-panel>

      <pagePanelNew style="padding: 10px;">
        <div class="content-layout">
          <div class="leftBox">
            <pagePanel headerTitle="设备初始状态" class="status-panel" showBtn>
              <scEcharts height="100%" :option="option1" />
            </pagePanel>

            <pagePanel headerTitle="方案参数" class="config-panel">
              <div class="config-content">
                <el-table
                  ref="paramTable"
                  v-loading="loading"
                  :data="paramTableData"
                  :border="true"
                  height="240"
                  :span-method="handleParamSpanMethod"
                >
                  <el-table-column label="类型" align="center" prop="type" min-width="120" />
                  <el-table-column label="参数" align="center" prop="param" min-width="180" />
                  <el-table-column label="值" header-align="center" align="right" min-width="220">
                    <template #default="{ row }">
                      <el-input
                        v-if="row.key === 'remark'"
                        :model-value="formValues.remark"
                        :disabled="!row.editable"
                        :placeholder="row.placeholder"
                        class="cm-inline-control"
                        size="small"
                        @update:model-value="updateTextValue('remark', $event)"
                      />
                      <el-input
                        v-else
                        :model-value="formatNumericValue(row.key)"
                        :disabled="!row.editable"
                        :placeholder="row.placeholder"
                        class="cm-inline-control"
                        size="small"
                        @update:model-value="updateNumericValue(row.key, $event)"
                      />
                    </template>
                  </el-table-column>
                </el-table>
              </div>
            </pagePanel>
          </div>

          <div class="rightBox">
            <pagePanel headerTitle="24小时日需求曲线" class="curve-panel">
              <div class="curve-section">
                <div class="curve-head">
                  <div>
                    <div class="curve-title">外输分配权重</div>
                    <div class="curve-subtitle">按小时设置外输分配权重，系统会按日外输目标自动归一化。</div>
                  </div>
                  <div class="curve-actions">
                    <el-button type="primary" @click="applyAverageCurve">平均分配</el-button>
                    <el-button type="primary" @click="resetDailyDemandCurve">恢复默认</el-button>
                  </div>
                </div>

                <div class="curve-summary">
                  <span>当前权重合计：{{ dailyCurveTotal.toFixed(1) }}</span>
                  <span>日外输目标：{{ formValues.targetOutputM3 || 0 }} m3/day</span>
                </div>

                <div class="curve-grid">
                  <div v-for="(_, index) in dailyDemandCurve" :key="index" class="curve-grid-item">
                    <span class="hour-label">{{ `${String(index).padStart(2, '0')}:00` }}</span>
                    <el-input-number
                      v-model="dailyDemandCurve[index]"
                      :min="0"
                      :step="10"
                      :precision="1"
                      size="small"
                      controls-position="right"
                    />
                  </div>
                </div>

                <div class="curve-chart">
                  <scEcharts height="100%" :option="curveOption" />
                </div>
              </div>
            </pagePanel>
          </div>
        </div>
      </pagePanelNew>
    </div>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: 'InitialCondition'
})

import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import scEcharts from '@/components/scEcharts/index.vue'
import { getUserInfo, type UserInfo } from '@/api/auth'
import { TOKEN_NAME, USER_INFO_KEY, USER_NAME } from '@/config/global'
import { useCalculationStore } from '@/store/modules/calculation'
import { useSchemeStore } from '@/store/modules/scheme'

type NumericParamKey = 'targetOutputM3' | 'lpTargetPressure' | 'hpTargetPressure'
type ParamKey = NumericParamKey | 'remark'

interface ParamRow {
  type: string
  param: string
  key: ParamKey
  editable: boolean
  placeholder: string
}

interface DeviceRow {
  device: string
  tag: string
  status: boolean
}

const DEFAULT_DAILY_DEMAND_CURVE = [
  956.0, 917.8, 975.1, 1051.6, 1147.2, 1434.0,
  2294.4, 3441.6, 3824.0, 3250.4, 2485.6, 2390.0,
  2581.2, 2447.3, 2332.6, 2256.1, 2485.6, 4206.4,
  4800.7, 4588.8, 3824.0, 3059.2, 1912.0, 1338.4
]

const router = useRouter()
const schemeStore = useSchemeStore()
const loading = ref(false)

const planName = ref('')
const conditionName = ref('')
const startTime = ref('2026-05-06 00:00:00')
const currentRoleCode = ref('')
const dailyDemandCurve = ref<number[]>([...DEFAULT_DAILY_DEMAND_CURVE])

const defaultValues = reactive({
  targetOutputM3: 16000,
  lpTargetPressure: 1.2,
  hpTargetPressure: 12,
  initialLiquidLevel: 0
})

const formValues = reactive({
  targetOutputM3: 16000,
  lpTargetPressure: 1.2,
  hpTargetPressure: 12,
  initialLiquidLevel: 0,
  remark: ''
})

const canEditAdvancedParams = computed(() => {
  const roleCode = currentRoleCode.value.trim().toUpperCase()
  return !!roleCode && roleCode !== 'VISITOR'
})

const paramTableData = computed<ParamRow[]>(() => [
  {
    type: '目标参数',
    param: '日外输目标 (m3/day)',
    key: 'targetOutputM3',
    editable: true,
    placeholder: '请输入日外输目标'
  },
  {
    type: '工艺参数',
    param: '低压泵目标压力 (MPa)',
    key: 'lpTargetPressure',
    editable: canEditAdvancedParams.value,
    placeholder: canEditAdvancedParams.value ? '请输入低压泵目标压力' : '当前账号无编辑权限'
  },
  {
    type: '工艺参数',
    param: '高压泵目标压力 (MPa)',
    key: 'hpTargetPressure',
    editable: canEditAdvancedParams.value,
    placeholder: canEditAdvancedParams.value ? '请输入高压泵目标压力' : '当前账号无编辑权限'
  },
  {
    type: '其他',
    param: '备注',
    key: 'remark',
    editable: true,
    placeholder: '请输入备注'
  }
])

const table1 = ref<DeviceRow[]>([
  { device: '低压泵', tag: '0331P01A', status: false },
  { device: '低压泵', tag: '0331P01B', status: false },
  { device: '低压泵', tag: '0331P01C', status: false },
  { device: '低压泵', tag: '0331P01D', status: false },
  { device: '低压泵', tag: '0331P02A', status: false },
  { device: '低压泵', tag: '0331P02B', status: false },
  { device: '低压泵', tag: '0331P02C', status: true },
  { device: '低压泵', tag: '0331P02D', status: false },
  { device: '低压泵', tag: '0331P03A', status: false },
  { device: '低压泵', tag: '0331P03B', status: false },
  { device: '低压泵', tag: '0331P03C', status: false },
  { device: '低压泵', tag: '0331P03D', status: false },
  { device: '低压泵', tag: '0331P04A', status: false },
  { device: '低压泵', tag: '0331P04B', status: false },
  { device: '低压泵', tag: '0331P04C', status: false },
  { device: '低压泵', tag: '0331P04D', status: false },
  { device: '海水大泵', tag: 'A', status: false },
  { device: '海水大泵', tag: 'B', status: false },
  { device: '海水大泵', tag: 'C', status: false },
  { device: '海水小泵', tag: 'A', status: false },
  { device: '海水小泵', tag: 'B', status: false },
  { device: '海水小泵', tag: 'C', status: false },
  { device: '海水小泵', tag: 'D', status: false },
  { device: '高压泵', tag: '0330P01A', status: false },
  { device: '高压泵', tag: '0330P01B', status: false },
  { device: '高压泵', tag: '0330P01C', status: false },
  { device: '高压泵', tag: '0330P01D', status: false },
  { device: '高压泵', tag: '0330P01E', status: false },
  { device: '高压泵', tag: '0330P01F', status: false },
  { device: 'ORV', tag: 'A', status: false },
  { device: 'ORV', tag: 'B', status: false },
  { device: 'ORV', tag: 'C', status: false },
  { device: 'ORV', tag: 'D', status: false },
  { device: 'ORV', tag: 'E', status: false },
  { device: 'ORV', tag: 'F', status: false },
  { device: 'ORV', tag: 'G', status: false }
])

const curveHours = computed(() => dailyDemandCurve.value.map((_, index) => `${String(index).padStart(2, '0')}:00`))
const dailyCurveTotal = computed(() => dailyDemandCurve.value.reduce((sum, item) => sum + Number(item || 0), 0))

const option1 = computed(() => {
  const deviceTypes = ['高压泵', '低压泵', 'ORV', '海水小泵', '海水大泵']
  const columns = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P']
  const deviceData: Record<string, Array<number | null>> = {}

  deviceTypes.forEach((type) => {
    deviceData[type] = new Array(16).fill(null)
  })

  table1.value.forEach((item) => {
    const currentDeviceData = deviceData[item.device]
    if (!currentDeviceData) return

    let colIndex = -1
    if (item.tag.length === 1) {
      colIndex = columns.indexOf(item.tag)
    } else if (item.tag.includes('P')) {
      const match = item.tag.match(/P(\d+)([A-Z])$/)
      if (match?.[1] && match?.[2]) {
        const groupNum = Number.parseInt(match[1], 10)
        const letterIndex = columns.indexOf(match[2])
        colIndex = (groupNum - 1) * 4 + letterIndex
      } else {
        colIndex = columns.indexOf(item.tag.slice(-1))
      }
    }

    if (colIndex >= 0 && colIndex < 16) {
      currentDeviceData[colIndex] = item.status ? 1 : 0
    }
  })

  const data: number[][] = []
  deviceTypes.forEach((type, rowIndex) => {
    deviceData[type]?.forEach((value, colIndex) => {
      if (value !== null) {
        data.push([colIndex, rowIndex, value])
      }
    })
  })

  return {
    backgroundColor: 'transparent',
    grid: {
      left: '90px',
      right: '20px',
      top: '40px',
      bottom: '50px',
      containLabel: false
    },
    xAxis: {
      type: 'category',
      data: columns,
      position: 'bottom',
      axisLine: { show: true, lineStyle: { color: '#8FA4CC' } },
      axisTick: { show: false },
      axisLabel: { color: '#8FA4CC', fontSize: 12 }
    },
    yAxis: {
      type: 'category',
      data: deviceTypes,
      axisLine: { show: true, lineStyle: { color: '#8FA4CC' } },
      axisTick: { show: false },
      axisLabel: { color: '#8FA4CC', fontSize: 12, margin: 10 }
    },
    visualMap: {
      min: 0,
      max: 1,
      calculable: false,
      show: false,
      inRange: {
        color: ['#ffffff', '#13ce66']
      }
    },
    series: [{
      name: '设备状态',
      type: 'heatmap',
      data,
      label: {
        show: false
      },
      itemStyle: {
        borderColor: '#e0e0e0',
        borderWidth: 1
      }
    }],
    tooltip: {
      trigger: 'item',
      formatter: (params: { value: [number, number, number] }) => {
        const row = deviceTypes[params.value[1]]
        const col = columns[params.value[0]]
        const status = params.value[2] === 1 ? '运行' : '停止'
        return `${row} ${col}<br/>状态：${status}`
      }
    },
    legend: {
      show: true,
      bottom: 10,
      left: 'center',
      data: [
        { name: '停止', itemStyle: { color: '#ffffff' } },
        { name: '运行', itemStyle: { color: '#13ce66' } }
      ],
      itemGap: 20,
      textStyle: {
        color: '#8FA4CC',
        fontSize: 12
      }
    }
  }
})

const curveOption = computed(() => ({
  backgroundColor: 'transparent',
  grid: { left: 52, right: 20, top: 24, bottom: 34, containLabel: true },
  tooltip: { trigger: 'axis' },
  xAxis: {
    type: 'category',
    data: curveHours.value,
    axisLabel: { color: '#8FA4CC', fontSize: 11, interval: 1, rotate: 45 },
    axisLine: { lineStyle: { color: '#8FA4CC' } }
  },
  yAxis: {
    type: 'value',
    name: '权重',
    nameTextStyle: { color: '#8FA4CC' },
    axisLabel: { color: '#8FA4CC' },
    splitLine: { lineStyle: { color: 'rgba(143, 164, 204, 0.18)' } }
  },
  series: [{
    name: '日需求曲线',
    type: 'line',
    smooth: true,
    symbol: 'circle',
    symbolSize: 6,
    lineStyle: { color: '#29b4ff', width: 2 },
    areaStyle: { color: 'rgba(41, 180, 255, 0.16)' },
    itemStyle: { color: '#29b4ff' },
    data: dailyDemandCurve.value
  }]
}))

const onPlanNameChange = (value: string) => {
  conditionName.value = value.trim()
}

const formatNumericValue = (key: NumericParamKey) => {
  const value = formValues[key]
  return Number.isFinite(value) ? String(value) : ''
}

const updateNumericValue = (key: NumericParamKey, value: string) => {
  const normalized = value.trim()
  if (!normalized) {
    formValues[key] = Number.NaN
    return
  }

  const parsed = Number(normalized)
  formValues[key] = Number.isNaN(parsed) ? Number.NaN : parsed
}

const updateTextValue = (key: 'remark', value: string) => {
  formValues[key] = value
}

const resetDailyDemandCurve = () => {
  dailyDemandCurve.value = [...DEFAULT_DAILY_DEMAND_CURVE]
}

const applyAverageCurve = () => {
  dailyDemandCurve.value = new Array(24).fill(1)
}

const applyDefaultValues = (useStoreSnapshot: boolean) => {
  const storeValues = schemeStore.initialCondition
  const useStore = useStoreSnapshot && !!storeValues.planName

  planName.value = useStore ? storeValues.planName : ''
  conditionName.value = useStore ? (storeValues.conditionName || storeValues.planName) : ''
  startTime.value = useStore ? storeValues.startTime : '2026-05-06 00:00:00'
  formValues.targetOutputM3 = useStore ? storeValues.targetOutputM3 : defaultValues.targetOutputM3
  formValues.lpTargetPressure = useStore ? storeValues.lpTargetPressure : defaultValues.lpTargetPressure
  formValues.hpTargetPressure = useStore ? storeValues.hpTargetPressure : defaultValues.hpTargetPressure
  formValues.initialLiquidLevel = useStore ? storeValues.initialLiquidLevel : defaultValues.initialLiquidLevel
  formValues.remark = useStore ? storeValues.remark : ''
  dailyDemandCurve.value = useStore && storeValues.dailyDemandCurve.length === 24
    ? [...storeValues.dailyDemandCurve]
    : [...DEFAULT_DAILY_DEMAND_CURVE]
}

const getCachedUserInfo = (): UserInfo | null => {
  const raw = localStorage.getItem(USER_INFO_KEY)
  if (!raw) return null

  try {
    return JSON.parse(raw) as UserInfo
  } catch {
    return null
  }
}

const loadUserPermission = async () => {
  const cached = getCachedUserInfo()
  if (cached?.roleCode) {
    currentRoleCode.value = cached.roleCode
    return
  }

  const userId = Number(localStorage.getItem(TOKEN_NAME) || '')
  const username = localStorage.getItem(USER_NAME) || ''
  if (!userId && !username) return

  try {
    const response = await getUserInfo({
      userId: Number.isFinite(userId) && userId > 0 ? userId : undefined,
      username: username || undefined
    })

    if (response.data.code === 200 && response.data.data) {
      currentRoleCode.value = response.data.data.roleCode || ''
      localStorage.setItem(USER_INFO_KEY, JSON.stringify(response.data.data))
    }
  } catch (error) {
    console.warn('加载用户权限失败，将按只读处理高级参数。', error)
  }
}

const handleConfirm = () => {
  if (!planName.value.trim()) {
    ElMessage.warning('请输入方案名称')
    return
  }

  if (!Number.isFinite(formValues.targetOutputM3) || formValues.targetOutputM3 <= 0) {
    ElMessage.warning('请填写日外输目标，且必须大于 0')
    return
  }

  if (!startTime.value.trim()) {
    ElMessage.warning('请选择开始时间')
    return
  }

  if (dailyDemandCurve.value.length !== 24 || dailyDemandCurve.value.some((item) => !Number.isFinite(item) || item < 0)) {
    ElMessage.warning('请完整填写 24 小时日需求曲线，且数值不能为负')
    return
  }

  if (dailyCurveTotal.value <= 0) {
    ElMessage.warning('日需求曲线合计必须大于 0')
    return
  }

  schemeStore.setInitialCondition({
    planName: planName.value.trim(),
    conditionName: conditionName.value.trim() || planName.value.trim(),
    remark: formValues.remark.trim(),
    targetOutputM3: formValues.targetOutputM3,
    lpTargetPressure: formValues.lpTargetPressure,
    hpTargetPressure: formValues.hpTargetPressure,
    initialLiquidLevel: formValues.initialLiquidLevel,
    startTime: startTime.value,
    dailyDemandCurve: dailyDemandCurve.value.map((item) => Number(item || 0))
  })

  const calcStore = useCalculationStore()
  calcStore.setShouldCalculate(true)
  router.push({
    path: '/generate',
    query: {
      run: String(Date.now())
    }
  })
}

const handleReset = () => {
  ElMessageBox.confirm('确定要清空并恢复默认初始条件吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    schemeStore.clearInitialCondition()
    applyDefaultValues(false)
    table1.value.forEach((item) => {
      item.status = false
    })
    ElMessage.success('初始条件已恢复默认值')
  }).catch(() => {})
}

const handleParamSpanMethod = ({ columnIndex, rowIndex }: { row: ParamRow; columnIndex: number; rowIndex: number }) => {
  if (columnIndex !== 0) {
    return { rowspan: 1, colspan: 1 }
  }

  const rows = paramTableData.value
  const prevRow = rowIndex > 0 ? rows[rowIndex - 1] : null
  const isFirstRow = rowIndex === 0 || !prevRow || prevRow.type !== rows[rowIndex]?.type
  if (!isFirstRow) {
    return { rowspan: 0, colspan: 0 }
  }

  let rowspan = 1
  for (let i = rowIndex + 1; i < rows.length; i += 1) {
    if (rows[i]?.type === rows[rowIndex]?.type) {
      rowspan += 1
    } else {
      break
    }
  }

  return { rowspan, colspan: 1 }
}

onMounted(async () => {
  applyDefaultValues(true)
  await loadUserPermission()
})
</script>

<style lang="scss" scoped>
.initial-condition-page {
  display: flex;
  flex-direction: row;
  height: 100%;
}

.toolbar-panel {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.toolbar-inline {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.toolbar-field {
  display: flex;
  align-items: center;

  .label {
    color: #fff;
    font-size: 14px;
    margin-right: 8px;
    white-space: nowrap;
  }
}

.content-layout {
  display: flex;
  height: 100%;
  width: 100%;
  gap: 10px;
}

.leftBox,
.rightBox {
  height: 100%;
  min-width: 0;
}

.leftBox {
  width: 50%;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.rightBox {
  width: 50%;
}

.status-panel {
  height: 52%;
  flex-shrink: 0;
}

.config-panel {
  flex: 1;
  min-height: 0;
}

.config-panel :deep(.panelBody) {
  padding: 12px;
}

.config-content {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.curve-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 0;
}

.curve-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.curve-title {
  color: #fff;
  font-size: 15px;
  font-weight: 600;
}

.curve-subtitle,
.curve-summary {
  color: #8fa4cc;
  font-size: 12px;
}

.curve-summary {
  display: flex;
  gap: 20px;
  flex-wrap: wrap;
}

.curve-actions {
  display: flex;
  gap: 8px;
}

.curve-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px 12px;
}

.curve-grid-item {
  display: flex;
  align-items: center;
  gap: 10px;
}

.hour-label {
  width: 48px;
  color: #8fa4cc;
  font-size: 12px;
  flex-shrink: 0;
}

.curve-chart {
  height: 220px;
  margin-top: 4px;
}

.curve-panel,
.curve-panel :deep(.panelBody) {
  height: 100%;
}

.curve-panel :deep(.panelBody) {
  padding: 12px;
}

:deep(.pagePanel) {
  padding: 0 !important;
}

@media (max-width: 1400px) {
  .curve-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 1200px) {
  .content-layout {
    flex-direction: column;
  }

  .leftBox,
  .rightBox {
    width: 100%;
  }
}
</style>
