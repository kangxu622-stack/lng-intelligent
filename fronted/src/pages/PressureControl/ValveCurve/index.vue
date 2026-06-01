<template>
  <div class="valve-control-container">
    <div class="control-panels">
      <pagePanel headerTitle="A 阀敏捷控制" class="control-panel">
        <div class="panel-content">
          <!-- 交互图表容器 -->
          <div 
            class="chart-container" 
            :class="{ 'is-dragging': isDragging }"
            @mousedown="startDrag" 
            ref="chartContainer"
          >
            <div class="chart-background">
              <svg class="grid-lines" width="100%" height="100%">
                <line v-for="i in 5" :key="'v'+i" 
                  :x1="i * 20 + '%'" y1="0" 
                  :x2="i * 20 + '%'" y2="100%" 
                  stroke="rgba(100, 150, 255, 0.15)" stroke-width="1"/>
                <line v-for="i in 4" :key="'h'+i" 
                  x1="0" :y1="i * 25 + '%'" 
                  x2="100%" :y2="i * 25 + '%'" 
                  stroke="rgba(100, 150, 255, 0.15)" stroke-width="1"/>
              </svg>
              
              <svg class="curve-svg" width="100%" height="100%" viewBox="0 0 500 250" preserveAspectRatio="none">
                <path :d="curvePath" 
                  fill="none" 
                  stroke="#a855f7" 
                  stroke-width="3"
                  stroke-linecap="round"
                  stroke-linejoin="round" />
                  
                <circle :cx="workPointX" :cy="workPointY" r="8" fill="#fbbf24" class="work-point"/>
                
                <line x1="10" :y1="workPointY" :x2="workPointX" :y2="workPointY" 
                  stroke="#fbbf24" stroke-width="2" stroke-dasharray="6,4"/>
                  
                <line :x1="workPointX" :y1="workPointY" :x2="workPointX" y2="240" 
                  stroke="#fbbf24" stroke-width="2" stroke-dasharray="6,4"/>
              </svg>
            </div>
          </div>

          <!-- 底部数据区域 -->
          <div class="bottom-data-section">
            <div class="data-row">
              <div class="data-item">
                <div class="data-label">高压泵流量</div>
                <div class="data-value">
                  <span class="value-number">{{ displayFlow }}</span>
                  <span class="value-unit">m³/h</span>
                </div>
              </div>
              <div class="data-item">
                <div class="data-label">计算开度</div>
                <div class="data-value">
                  <span class="value-number">{{ displayOpening }}</span>
                  <span class="value-unit">%</span>
                </div>
              </div>
            </div>

            <div class="formula-section">
              <div class="formula-label">流量特性曲线公式</div>
              <div class="formula-value">y = -0.0053L² + 0.8095L - 0.3068</div>
            </div>

            <div class="variable-section">
              <div class="variable-column">
                <div class="variable-label">操纵变量</div>
                <div class="variable-value">A 阀开度</div>
                <div class="variable-value">B 阀控制器设定值</div>
              </div>
              <div class="variable-column">
                <div class="variable-label">被控变量</div>
                <div class="variable-value">A/B 阀后 y 压力</div>
              </div>
            </div>
          </div>
        </div>
      </pagePanel>

      <pagePanel headerTitle="再冷凝器液位控制" class="control-panel">
        <div class="panel-content">
          <div class="device-info">
            <span class="device-code">0330-V-01</span>
            <span class="divider">|</span>
            <span class="level-range">液位范围：50%-75%</span>
          </div>

          <div class="material-balance">
            <h3 class="section-title">物料衡算</h3>
            <div class="balance-item">
              <span class="balance-label">进入再冷凝器 LNG：</span>
              <span class="balance-value positive">+3400kg/h</span>
            </div>
            <div class="balance-item">
              <span class="balance-label">进入再冷凝器 BOG：</span>
              <span class="balance-value positive">+1250kg/h</span>
            </div>
            <div class="balance-item">
              <span class="balance-label">流出至高压泵：</span>
              <span class="balance-value negative">-4650kg/h</span>
            </div>
          </div>

          <div class="valve-control-section">
            <div class="valve-item">
              <span class="valve-label">控制阀 0330-LV-0004</span>
              <div class="valve-opening">
                <span class="opening-number">45</span>
                <span class="opening-unit">%</span>
              </div>
            </div>
            <div class="valve-item">
              <span class="valve-label">入口流量阀 0330-FV-0001</span>
              <div class="valve-opening">
                <span class="opening-number">38</span>
                <span class="opening-unit">%</span>
              </div>
            </div>
          </div>
        </div>
      </pagePanel>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted } from 'vue'

const chartContainer = ref<HTMLElement | null>(null)
const isDragging = ref(false)

// X 坐标对应的开度 0% ~ 100% 映射到 SVG 宽度 10 ~ 490
const workPointX = ref(152)

// 根据 X 坐标计算对应的 Y 坐标（利用实际公式）
const calculateY = (x: number): number => {
  // 1. 将 SVG X 坐标 (10~490) 转化为开度百分比 L (0~100)
  const L = ((x - 10) / 480) * 100
  
  // 2. 代入公式 y = -0.0053L² + 0.8095L - 0.3068
  const yVal = -0.0053 * Math.pow(L, 2) + 0.8095 * L - 0.3068
  
  // 3. 将计算出的理论 yVal (大约最大 31 左右) 映射回 SVG Y 坐标
  // SVG 的 Y 轴是从上往下的，230是最低点，40是最高点
  const maxYVal = 32 // 预估的最大阈值，用于按比例缩小
  const mappedY = 230 - (yVal / maxYVal) * 190
  
  // 限制在视图范围内
  return Math.max(40, Math.min(230, mappedY))
}

// 实时响应的 Y 坐标
const workPointY = computed(() => calculateY(workPointX.value))

// 动态生成完美贴合公式的 SVG 路径
const curvePath = computed(() => {
  let path = ''
  for (let x = 10; x <= 490; x += 5) { // 每隔 5 个像素取样一次
    const y = calculateY(x)
    if (x === 10) {
      path += `M ${x},${y} `
    } else {
      path += `L ${x},${y} `
    }
  }
  return path
})

// 根据 X 计算显示开度 (保留一位小数)
const displayOpening = computed(() => {
  const L = ((workPointX.value - 10) / 480) * 100
  return L.toFixed(1)
})

// 根据 X 计算显示流量 (流量 = 开度 * 5，根据截图数据推导)
const displayFlow = computed(() => {
  const L = ((workPointX.value - 10) / 480) * 100
  return Math.round(L * 5)
})

// 鼠标位置更新逻辑
const updatePosition = (clientX: number) => {
  if (!chartContainer.value) return
  const rect = chartContainer.value.getBoundingClientRect()
  
  // 将鼠标的屏幕物理 X 坐标映射到 SVG 的 0~500 坐标系中
  let svgX = (clientX - rect.left) * (500 / rect.width)
  
  // 限制点只能在 10 ~ 490 范围内移动
  svgX = Math.max(10, Math.min(490, svgX))
  workPointX.value = svgX
}

// 开始拖动 (点击任意位置也可以直接跳过去)
const startDrag = (e: MouseEvent) => {
  isDragging.value = true
  updatePosition(e.clientX)
  
  // 绑定在 window 上，这样就算鼠标拖到容器外面，点也依然能跟随滑动
  window.addEventListener('mousemove', handleDrag)
  window.addEventListener('mouseup', stopDrag)
}

const handleDrag = (e: MouseEvent) => {
  if (isDragging.value) {
    updatePosition(e.clientX)
  }
}

const stopDrag = () => {
  isDragging.value = false
  window.removeEventListener('mousemove', handleDrag)
  window.removeEventListener('mouseup', stopDrag)
}

// 组件卸载时清理全局事件
onUnmounted(() => {
  window.removeEventListener('mousemove', handleDrag)
  window.removeEventListener('mouseup', stopDrag)
})
</script>

<style lang="scss" scoped>
.valve-control-container {
  width: 100%;
  height: 100%;
  padding: 20px;
  box-sizing: border-box;
  background: rgba(20, 50, 100, 0.3);
  background-image: 
    radial-gradient(circle at 50% 50%, rgba(20, 40, 80, 0.5) 0%, transparent 100%),
    repeating-linear-gradient(0deg, transparent, transparent 19px, rgba(100, 150, 255, 0.05) 20px),
    repeating-linear-gradient(90deg, transparent, transparent 19px, rgba(100, 150, 255, 0.05) 20px);
}

.control-panels {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  height: 100%;
}

.control-panel {
  height: 100%;
}

.panel-content {
  display: flex;
  flex-direction: column;
  gap: 15px;
  height: 100%;
}

/* 交互图表容器 */
.chart-container {
  height: 220px;
  background: rgba(10, 20, 40, 0.6);
  border: 1px solid rgba(100, 150, 255, 0.2);
  border-radius: 4px;
  position: relative;
  overflow: hidden;
  
  /* 鼠标手势设置 */
  cursor: grab;
  user-select: none;
  
  &.is-dragging {
    cursor: grabbing;
  }
}

.work-point {
  transition: r 0.2s ease;
}

.chart-container:hover .work-point,
.chart-container.is-dragging .work-point {
  r: 12;
}

.chart-background {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
}

.grid-lines,
.curve-svg {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
}

.bottom-data-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.data-row {
  display: flex;
  gap: 15px;
}

.data-item {
  flex: 1;
  background: rgba(30, 50, 90, 0.5);
  border-radius: 4px;
  padding: 12px;
}

.data-label {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
  margin-bottom: 6px;
}

.data-value {
  display: flex;
  align-items: baseline;
  gap: 3px;
}

.value-number {
  font-size: 24px;
  font-weight: 700;
  color: #ffffff;
}

.value-unit {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.5);
}

.formula-section {
  background: rgba(80, 60, 120, 0.3);
  border: 1px solid rgba(168, 85, 247, 0.3);
  border-radius: 4px;
  padding: 10px 12px;
}

.formula-label {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
  margin-bottom: 6px;
}

.formula-value {
  font-size: 14px;
  font-family: 'Courier New', monospace;
  color: #a855f7;
  letter-spacing: 1px;
}

.variable-section {
  display: flex;
  gap: 20px;
  background: rgba(30, 50, 90, 0.3);
  border-radius: 4px;
  padding: 10px 12px;
}

.variable-column {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.variable-label {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.5);
}

.variable-value {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.9);
}

// 右侧面板样式
.right-panel {
  .panel-content {
    gap: 20px;
  }
}

.device-info {
  display: flex;
  align-items: center;
  gap: 10px;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(100, 150, 255, 0.2);
}

.device-code {
  font-size: 14px;
  font-weight: 600;
  color: #ffffff;
}

.divider {
  color: rgba(255, 255, 255, 0.3);
}

.level-range {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
}

.material-balance {
  background: rgba(30, 50, 90, 0.3);
  border-radius: 4px;
  padding: 12px;
}

.section-title {
  font-size: 12px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.7);
  margin: 0 0 10px 0;
  text-transform: uppercase;
}

.balance-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
  border-bottom: 1px solid rgba(100, 150, 255, 0.1);
  
  &:last-child {
    border-bottom: none;
  }
}

.balance-label {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
}

.balance-value {
  font-size: 14px;
  font-weight: 600;
  
  &.positive {
    color: #10b981;
  }
  
  &.negative {
    color: #ef4444;
  }
}

.valve-control-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.valve-item {
  background: rgba(40, 60, 100, 0.4);
  border: 1px solid rgba(100, 150, 255, 0.2);
  border-radius: 4px;
  padding: 12px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.valve-label {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
}

.valve-opening {
  display: flex;
  align-items: baseline;
  gap: 2px;
}

.opening-number {
  font-size: 28px;
  font-weight: 700;
  color: #ffffff;
}

.opening-unit {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.5);
}

@media (max-width: 1200px) {
  .control-panels {
    grid-template-columns: 1fr;
  }
}
</style>