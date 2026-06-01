<template>
  <div class="cm-container-wrapper p10 generate-container">
    <!-- 主内容区域 -->
    <div class="main-content">
      <!-- 进度条 -->
      <div class="progress-section">
        <el-progress :percentage="progress" :stroke-width="8" :show-text="false"></el-progress>
      </div>

      <!-- 状态信息 -->
      <div class="status-section">
        <div class="status-text" v-if="statusMessage">{{ statusMessage }}</div>
        <div class="status-text" v-if="finishMessage">{{ finishMessage }}</div>
        <div class="status-text success-text" v-if="showViewLink">
          <a @click="goToScheme" class="scheme-link">进入调度方案展示内查看相关调度方案</a>
        </div>
      </div>
    </div>
    <!-- 图表区域 -->
    <div class="chart-section" v-if="showChart">
      <scEcharts ref="chartRef" height="100%" :option="chartOption"></scEcharts>
    </div>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: '调度方案生成'
})

import { ref, onBeforeUnmount, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import scEcharts from '@/components/scEcharts/index.vue'
import { calculate, toPriorityMode, type SimulationResult } from '@/api/simulation'
import { useSchemeStore } from '@/store/modules/scheme'
import { useCalculationStore } from '@/store/modules/calculation'

const router = useRouter()
const schemeStore = useSchemeStore()
const calcStore = useCalculationStore()

// 跳转到调度方案展示页面
const goToScheme = () => {
  router.push('/scheme/export-optimization')
}

const isRunning = ref(false)
const isPaused = ref(false)
const isError = ref(false)
const progress = ref(0)
const statusMessage = ref('')
const finishMessage = ref('')
const showChart = ref(false)
const showViewLink = ref(false)
const timer = ref<number | null>(null)
const startedAt = ref<number | null>(null)
const chartOption = ref({})
// 以下 ref 用于模板引用，虽然在 script 中未直接使用，但在模板中通过 ref="chartRef" 使用
// eslint-disable-next-line @typescript-eslint/no-unused-vars
// @ts-ignore - 模板引用，在模板中使用
const chartRef = ref()

onBeforeUnmount(() => {
  if (timer.value) {
    clearInterval(timer.value)
  }
})

// 页面加载后检查是否需要自动开始计算
onMounted(() => {
  if (calcStore.shouldCalculate) {
    handleStart()
  }
})

watch(
  () => calcStore.shouldCalculate,
  (shouldCalculate) => {
    if (shouldCalculate) {
      handleStart()
    }
  }
)

// 开始
const handleStart = async () => {
  if (isRunning.value) {
    return
  }

  calcStore.reset()
  isRunning.value = true
  isPaused.value = false
  isError.value = false
  progress.value = 0
  statusMessage.value = '优化程序开始执行,请等待...'
  finishMessage.value = ''
  showChart.value = false
  showViewLink.value = false
  startedAt.value = Date.now()

  // 模拟进度更新 - 让进度条走到90%等待实际完成
  timer.value = window.setInterval(() => {
    if (progress.value < 90 && !isPaused.value && !isError.value) {
      progress.value += 1
    }
  }, 50)

  // 从 Pinia store 获取初始条件参数
  const {
    planName,
    conditionName,
    remark,
    targetOutputM3,
    lpTargetPressure,
    hpTargetPressure,
    initialLiquidLevel,
    startTime,
    dailyDemandCurve,
    calculationMode
  } = schemeStore.schemeParams
  
  if (!planName || !conditionName) {
    isError.value = true
    isRunning.value = false
    statusMessage.value = '【错误】缺少方案名称或条件名称，请从初始条件页面进入'
    return
  }

  try {
    const response = await calculate({
      planName,
      conditionName,
      remark: remark || '',
      targetOutputM3,
      lpTargetPressure,
      hpTargetPressure,
      initialLiquidLevel,
      startTime,
      dailyDemandCurve,
      calculationMode: toPriorityMode(calculationMode)
    })
    console.log('后端返回:', response.data)

    // 根据后端业务状态码判断
    if (response.data.code === 200) {
      const result = response.data.data as SimulationResult
      schemeStore.setSimulationResult(result)
      schemeStore.setBogData(result.bog || { hours: [], bog_mech_kgph: [], bog_pred_kgph: [] })

      // 请求完成，进度条走到100%
      if (timer.value) {
        clearInterval(timer.value)
        timer.value = null
      }
      progress.value = 100
      statusMessage.value = '计算成功'
      handleFinish()
    } else {
      isError.value = true
      isRunning.value = false
      statusMessage.value = `【计算失败】${response.data.message}`
      if (timer.value) {
        clearInterval(timer.value)
        timer.value = null
      }
    }
  } catch (error) {
    console.error('网络请求失败:', error)
    isError.value = true
    isRunning.value = false
    statusMessage.value = '【网络错误】' + (error as Error).message
    if (timer.value) {
      clearInterval(timer.value)
      timer.value = null
    }
  }
}

// 完成
const handleFinish = () => {
  isRunning.value = false
  isPaused.value = false
  const elapsedTime = startedAt.value ? ((Date.now() - startedAt.value) / 1000).toFixed(0) : '0'
  statusMessage.value = ''
  finishMessage.value = `优化任务结束!用时${elapsedTime}s`
  showChart.value = true
  showViewLink.value = true
  // 初始化图表
  initChart()
}

// 初始化图表
const initChart = () => {
  // 这里可以添加实际的图表配置
  chartOption.value = {
    backgroundColor: 'transparent',
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: []
    },
    yAxis: {
      type: 'value'
    },
    series: [{
      data: [],
      type: 'line'
    }]
  }
}
</script>

<style lang="scss" scoped>
.generate-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  // background: #0a1929;
  position: relative;

  // 网格背景
  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    // background-image:
    //   radial-gradient(circle, rgba(143, 164, 204, 0.1) 1px, transparent 1px);
    // background-size: 20px 20px;
    // pointer-events: none;
    // z-index: 0;
  }
}

.button-bar {
  padding: 20px;
  display: flex;
  gap: 10px;
  z-index: 1;
  position: relative;
  // background: rgba(10, 25, 41, 0.8);

  .el-button {
    min-width: 80px;
  }
}

.main-content {
  flex: 1;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  z-index: 1;
  position: relative;
  overflow: hidden;
}

.progress-section {
  width: 100%;
  z-index: 1;
  position: relative;

  :deep(.el-progress-bar__outer) {
    // background-color: rgba(255, 255, 255, 0.1);
    border-radius: 4px;
  }

  :deep(.el-progress-bar__inner) {
    // background-color: #8fa4cc;
    border-radius: 4px;
  }
}

.status-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
  z-index: 1;
  position: relative;
}

.status-text {
  color: #ffffff;
  font-size: 14px;
  line-height: 1.5;
}

.success-text {
  margin-top: 10px;
  
  a {
    color: #67c23a;
    text-decoration: underline;
    cursor: pointer;
    
    &:hover {
      color: #85ce61;
    }
  }
}

.chart-section {
  flex: 1;
  min-height: 0;
  z-index: 1;
  position: relative;
}
</style>
