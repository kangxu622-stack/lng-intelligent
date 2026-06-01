<template>
  <div class="one-key-start-container">
    <div class="control-panels">
      <pagePanel headerTitle="一键启停控制" class="full-width-panel">
        <!-- 顶部操作栏 -->
        <cm-panel 
          style="height: 56px; display: flex; align-items: center; justify-content: space-between;" 
          class="mb10"
        >
      <div style="display: flex; gap: 12px; align-items: center;">
        <el-button 
          type="primary"
          @click="handleStart"
          :disabled="isRunning"
        >
          启动
        </el-button>
        <el-button 
          type="primary"
          @click="handlePause"
          :disabled="!isRunning || isPaused"
        >
          暂停
        </el-button>
        <el-button 
          type="danger"
          @click="handleStop"
          :disabled="!isRunning && !isPaused"
        >
          停止
        </el-button>
        <el-divider direction="vertical" />
        <div class="scheme-select">
          <span class="label">选择方案：</span>
          <el-select v-model="selectedScheme" placeholder="请选择调度方案" size="small" style="width: 250px" @change="onSchemeChange">
            <el-option
              v-for="item in schemeOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </div>
      </div>
      <div class="status-display" v-if="statusInfo.show">
        <span class="status-label">当前状态：</span>
        <span :class="['status-value', statusInfo.type]">{{ statusInfo.text }}</span>
      </div>
    </cm-panel>

    <!-- 主内容区域 -->
    <div class="main-content">
      <!-- 左侧：进度和日志 -->
      <div class="left-section">
        <!-- 进度面板 -->
        <pagePanelNew showBtn style="margin-bottom: 10px;">
          <div class="progress-panel">
            <div class="progress-title">执行进度</div>
            <div class="progress-bar-wrapper">
              <el-progress 
                :percentage="progress" 
                :stroke-width="12"
                :status="progressStatus"
                :show-text="false"
              />
              <div class="progress-text">{{ progressText }}</div>
            </div>
          </div>
        </pagePanelNew>

        <!-- 执行日志 -->
        <pagePanelNew showBtn>
          <div class="log-panel">
            <div class="panel-header">
              <span class="header-title">执行日志</span>
              <el-button type="text" size="small" @click="clearLogs">清空日志</el-button>
            </div>
            <div class="log-content" ref="logContainer">
              <div v-if="logs.length === 0" class="empty-log">
                <p>暂无日志记录</p>
              </div>
              <div 
                v-for="(log, index) in logs" 
                :key="index" 
                :class="['log-item', log.type]"
              >
                <span class="log-time">{{ log.time }}</span>
                <span class="log-icon">{{ getLogIcon(log.type) }}</span>
                <span class="log-text">{{ log.message }}</span>
              </div>
            </div>
          </div>
        </pagePanelNew>
      </div>

      <!-- 右侧：设备状态监控 -->
      <div class="right-section">
        <pagePanelNew showBtn>
          <div class="device-monitor">
            <div class="panel-header">
              <span class="header-title">设备启停状态</span>
              <div class="legend">
                <span class="legend-item">
                  <span class="legend-dot running"></span>
                  <span>运行</span>
                </span>
                <span class="legend-item">
                  <span class="legend-dot stopped"></span>
                  <span>停止</span>
                </span>
                <span class="legend-item">
                  <span class="legend-dot fault"></span>
                  <span>故障</span>
                </span>
              </div>
            </div>
            
            <div class="device-grid">
              <!-- 低压泵 -->
              <div class="device-group">
                <div class="group-title">低压泵</div>
                <div class="device-cards">
                  <div 
                    v-for="device in devices.lpPumps" 
                    :key="device.tag"
                    :class="['device-card', device.status]"
                    @click="toggleDevice(device)"
                  >
                    <div class="device-tag">{{ device.tag }}</div>
                    <div class="device-status">{{ getStatusText(device.status) }}</div>
                  </div>
                </div>
              </div>

              <!-- 高压泵 -->
              <div class="device-group">
                <div class="group-title">高压泵</div>
                <div class="device-cards">
                  <div 
                    v-for="device in devices.hpPumps" 
                    :key="device.tag"
                    :class="['device-card', device.status]"
                    @click="toggleDevice(device)"
                  >
                    <div class="device-tag">{{ device.tag }}</div>
                    <div class="device-status">{{ getStatusText(device.status) }}</div>
                  </div>
                </div>
              </div>

              <!-- ORV -->
              <div class="device-group">
                <div class="group-title">ORV</div>
                <div class="device-cards">
                  <div 
                    v-for="device in devices.orv" 
                    :key="device.tag"
                    :class="['device-card', device.status]"
                    @click="toggleDevice(device)"
                  >
                    <div class="device-tag">{{ device.tag }}</div>
                    <div class="device-status">{{ getStatusText(device.status) }}</div>
                  </div>
                </div>
              </div>

              <!-- 海水泵 -->
              <div class="device-group">
                <div class="group-title">海水泵</div>
                <div class="device-cards">
                  <div 
                    v-for="device in devices.swPumps" 
                    :key="device.tag"
                    :class="['device-card', device.status]"
                    @click="toggleDevice(device)"
                  >
                    <div class="device-tag">{{ device.tag }}</div>
                    <div class="device-status">{{ getStatusText(device.status) }}</div>
                  </div>
                </div>
              </div>

              <!-- 海水消防泵 -->
              <div class="device-group">
                <div class="group-title">海水消防泵</div>
                <div class="device-cards">
                  <div 
                    v-for="device in devices.firePumps" 
                    :key="device.tag"
                    :class="['device-card', device.status]"
                    @click="toggleDevice(device)"
                  >
                    <div class="device-tag">{{ device.tag }}</div>
                    <div class="device-status">{{ getStatusText(device.status) }}</div>
                  </div>
                </div>
              </div>

              <!-- 压缩机 -->
              <div class="device-group">
                <div class="group-title">压缩机</div>
                <div class="device-cards">
                  <div 
                    v-for="device in devices.compressors" 
                    :key="device.tag"
                    :class="['device-card', device.status]"
                    @click="toggleDevice(device)"
                  >
                    <div class="device-tag">{{ device.tag }}</div>
                    <div class="device-status">{{ getStatusText(device.status) }}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </pagePanelNew>
      </div>
    </div>
      </pagePanel>
    </div>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: 'OneKeyStart'
})

import { ref, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useSchemeStore } from '@/store/modules/scheme'

const schemeStore = useSchemeStore()

void schemeStore

// 状态管理
const isRunning = ref(false)
const isPaused = ref(false)
const progress = ref(0)
const progressStatus = ref<'normal' | 'success' | 'exception'>('normal')
const progressText = ref('等待启动')

// 选中的方案
const selectedScheme = ref('')
const schemeOptions = ref([
  { label: '方案 A - 节能模式', value: 'scheme_a' },
  { label: '方案 B - 快速模式', value: 'scheme_b' },
  { label: '方案 C - 平衡模式', value: 'scheme_c' },
])

// 状态显示
const statusInfo = ref({
  show: false,
  text: '',
  type: 'info'
})

// 日志
const logs = ref<Array<{
  time: string
  message: string
  type: 'info' | 'success' | 'warning' | 'error'
}>>([])

const logContainer = ref<HTMLElement | null>(null)

// 设备数据
const devices = ref({
  lpPumps: [
    { tag: '0301P01A', status: 'stopped' },
    { tag: '0301P01B', status: 'stopped' },
    { tag: '0301P01C', status: 'stopped' },
    { tag: '0301P01D', status: 'stopped' },
  ],
  hpPumps: [
    { tag: '0330P01A', status: 'stopped' },
    { tag: '0330P01B', status: 'stopped' },
    { tag: '0330P01C', status: 'stopped' },
    { tag: '0330P01D', status: 'stopped' },
    { tag: '0330P01E', status: 'stopped' },
    { tag: '0330P01F', status: 'stopped' },
  ],
  orv: [
    { tag: '0330-VP-01A', status: 'stopped' },
    { tag: '0330-VP-01B', status: 'stopped' },
    { tag: '0330-VP-01C', status: 'stopped' },
    { tag: '0330-VP-01D', status: 'stopped' },
    { tag: '0330-VP-01E', status: 'stopped' },
    { tag: '0330-VP-01F', status: 'stopped' },
    { tag: '0330-VP-01G', status: 'stopped' },
  ],
  swPumps: [
    { tag: '0511-P-01A', status: 'stopped' },
    { tag: '0511-P-01B', status: 'stopped' },
    { tag: '0511-P-01C', status: 'stopped' },
    { tag: '0511-P-01D', status: 'stopped' },
    { tag: '0511-P-01E', status: 'stopped' },
    { tag: '0511-P-01F', status: 'stopped' },
  ],
  firePumps: [
    { tag: '0515-P-103', status: 'stopped' },
    { tag: '0515-P-104A', status: 'stopped' },
    { tag: '0515-P-104B', status: 'stopped' },
  ],
  compressors: [
    { tag: '0330-C-01A', status: 'stopped' },
    { tag: '0330-C-01B', status: 'stopped' },
  ]
})

// 获取状态文本
const getStatusText = (status: string) => {
  switch (status) {
    case 'running': return '运行'
    case 'stopped': return '停止'
    case 'fault': return '故障'
    default: return status
  }
}

// 获取日志图标
const getLogIcon = (type: string) => {
  switch (type) {
    case 'info': return 'ℹ'
    case 'success': return '✓'
    case 'warning': return '⚠'
    case 'error': return '✗'
    default: return '•'
  }
}

// 切换设备状态（手动控制）
const toggleDevice = (device: any) => {
  if (isRunning.value) {
    ElMessage.warning('程序运行中，无法手动控制设备')
    return
  }
  
  // 简单切换状态
  device.status = device.status === 'stopped' ? 'running' : 'stopped'
  addLog(`手动切换设备 ${device.tag} 为${getStatusText(device.status)}`, 'info')
}

// 添加日志
const addLog = (message: string, type: 'info' | 'success' | 'warning' | 'error' = 'info') => {
  const now = new Date()
  const time = now.toLocaleTimeString('zh-CN', { hour12: false })
  logs.value.push({ time, message, type })
  
  // 自动滚动到底部
  nextTick(() => {
    if (logContainer.value) {
      logContainer.value.scrollTop = logContainer.value.scrollHeight
    }
  })
}

// 清空日志
const clearLogs = () => {
  logs.value = []
  ElMessage.success('日志已清空')
}

// 方案变化
const onSchemeChange = (val: string) => {
  console.log('选择方案:', val)
  addLog(`选择调度方案：${schemeOptions.value.find(o => o.value === val)?.label}`, 'info')
}

// 启动
const handleStart = async () => {
  if (!selectedScheme.value) {
    ElMessage.warning('请先选择调度方案')
    return
  }
  
  isRunning.value = true
  isPaused.value = false
  progress.value = 0
  progressStatus.value = 'normal'
  progressText.value = '启动中...'
  
  statusInfo.value = {
    show: true,
    text: '运行中',
    type: 'running'
  }
  
  addLog('一键启停程序启动', 'success')
  addLog('开始按方案执行设备启停操作...', 'info')
  
  // 模拟执行过程
  simulateExecution()
}

// 暂停
const handlePause = () => {
  isPaused.value = true
  progressStatus.value = 'normal'
  progressText.value = '已暂停'
  
  statusInfo.value = {
    show: true,
    text: '已暂停',
    type: 'paused'
  }
  
  addLog('程序已暂停', 'warning')
}

// 停止
const handleStop = () => {
  ElMessageBox.confirm('确定要停止当前执行吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    isRunning.value = false
    isPaused.value = false
    progressStatus.value = 'exception'
    progressText.value = '已停止'
    
    statusInfo.value = {
      show: true,
      text: '已停止',
      type: 'stopped'
    }
    
    addLog('程序已手动停止', 'error')
  }).catch(() => {
    // 取消操作
  })
}

// 模拟执行过程
const simulateExecution = () => {
  let currentProgress = 0
  const steps = [
    { progress: 10, message: '校验方案参数...' },
    { progress: 20, message: '启动低压泵组...' },
    { progress: 35, message: '启动高压泵组...' },
    { progress: 50, message: '启动 ORV 系统...' },
    { progress: 65, message: '启动海水泵组...' },
    { progress: 80, message: '启动压缩机...' },
    { progress: 90, message: '系统联调中...' },
    { progress: 100, message: '执行完成' }
  ]
  
  let stepIndex = 0
  
  const timer = setInterval(() => {
    if (isPaused.value) {
      clearInterval(timer)
      return
    }
    
    if (stepIndex >= steps.length) {
      clearInterval(timer)
      isRunning.value = false
      progressStatus.value = 'success'
      progressText.value = '已完成'
      
      statusInfo.value = {
        show: true,
        text: '已完成',
        type: 'success'
      }
      
      addLog('一键启停程序执行完成', 'success')
      return
    }
    
    const step = steps[stepIndex]!
    currentProgress = step.progress
    progress.value = currentProgress
    addLog(step.message, 'info')
    stepIndex++
    
  }, 800)
}
</script>

<style lang="scss" scoped>
.one-key-start-container {
  width: 100%;
  height: 100%;
  padding: 20px;
  background: rgba(20, 50, 100, 0.3);
}

.control-panels {
  width: 100%;
  height: 100%;
}

.full-width-panel {
  width: 100%;
  height: 100%;
  overflow-y: auto;
}

.main-content {
  flex: 1;
  display: flex;
  gap: 10px;
  overflow: hidden;
  min-height: 0;
}

.left-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.right-section {
  flex: 1.5;
  min-width: 0;
}

// 方案选择样式
.scheme-select {
  display: flex;
  align-items: center;
  .label {
    color: #fff;
    font-size: 14px;
    margin-right: 8px;
    white-space: nowrap;
  }
}

// 状态显示
.status-display {
  display: flex;
  align-items: center;
  gap: 8px;
  
  .status-label {
    color: rgba(255, 255, 255, 0.7);
    font-size: 13px;
  }
  
  .status-value {
    padding: 4px 12px;
    border-radius: 4px;
    font-size: 13px;
    font-weight: 500;
    
    &.running {
      background: rgba(19, 206, 102, 0.2);
      color: #13ce66;
      border: 1px solid #13ce66;
    }
    
    &.paused {
      background: rgba(255, 170, 0, 0.2);
      color: #ffaa00;
      border: 1px solid #ffaa00;
    }
    
    &.stopped {
      background: rgba(255, 68, 68, 0.2);
      color: #ff4444;
      border: 1px solid #ff4444;
    }
    
    &.success {
      background: rgba(103, 194, 58, 0.2);
      color: #67c23a;
      border: 1px solid #67c23a;
    }
    
    &.info {
      background: rgba(64, 158, 255, 0.2);
      color: #409eff;
      border: 1px solid #409eff;
    }
  }
}

// 进度面板
.progress-panel {
  padding: 15px;
  
  .progress-title {
    color: #fff;
    font-size: 14px;
    font-weight: 500;
    margin-bottom: 15px;
  }
  
  .progress-bar-wrapper {
    position: relative;
    
    .progress-text {
      color: rgba(255, 255, 255, 0.6);
      font-size: 12px;
      margin-top: 8px;
      text-align: center;
    }
  }
}

// 日志面板
.log-panel {
  height: 100%;
  display: flex;
  flex-direction: column;
  
  .panel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 15px;
    border-bottom: 1px solid rgba(143, 164, 204, 0.2);
    
    .header-title {
      color: #fff;
      font-size: 14px;
      font-weight: 500;
    }
  }
  
  .log-content {
    flex: 1;
    overflow-y: auto;
    padding: 10px;
    
    &::-webkit-scrollbar {
      width: 6px;
    }
    
    &::-webkit-scrollbar-thumb {
      background: rgba(143, 164, 204, 0.3);
      border-radius: 3px;
      
      &:hover {
        background: rgba(143, 164, 204, 0.5);
      }
    }
    
    .empty-log {
      text-align: center;
      color: rgba(255, 255, 255, 0.4);
      padding: 40px 20px;
      font-size: 13px;
    }
    
    .log-item {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      padding: 8px 10px;
      margin-bottom: 6px;
      background: rgba(20, 50, 100, 0.2);
      border-left: 3px solid rgba(143, 164, 204, 0.5);
      border-radius: 4px;
      font-size: 13px;
      
      .log-time {
        color: rgba(143, 164, 204, 0.8);
        font-family: 'Courier New', monospace;
        font-size: 12px;
        white-space: nowrap;
      }
      
      .log-icon {
        color: rgba(143, 164, 204, 0.8);
        font-size: 14px;
        flex-shrink: 0;
      }
      
      .log-text {
        color: rgba(255, 255, 255, 0.8);
        line-height: 1.5;
        flex: 1;
      }
      
      &.info {
        border-left-color: #409eff;
        background: rgba(64, 158, 255, 0.05);
        
        .log-icon {
          color: #409eff;
        }
      }
      
      &.success {
        border-left-color: #67c23a;
        background: rgba(103, 194, 58, 0.05);
        
        .log-icon, .log-text {
          color: #67c23a;
        }
      }
      
      &.warning {
        border-left-color: #ffaa00;
        background: rgba(255, 170, 0, 0.05);
        
        .log-icon, .log-text {
          color: #ffaa00;
        }
      }
      
      &.error {
        border-left-color: #ff4444;
        background: rgba(255, 68, 68, 0.05);
        
        .log-icon, .log-text {
          color: #ff4444;
        }
      }
    }
  }
}

// 设备监控
.device-monitor {
  height: 100%;
  display: flex;
  flex-direction: column;
  
  .panel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 15px;
    border-bottom: 1px solid rgba(143, 164, 204, 0.2);
    
    .header-title {
      color: #fff;
      font-size: 14px;
      font-weight: 500;
    }
    
    .legend {
      display: flex;
      gap: 15px;
      
      .legend-item {
        display: flex;
        align-items: center;
        gap: 6px;
        color: rgba(255, 255, 255, 0.6);
        font-size: 12px;
        
        .legend-dot {
          width: 10px;
          height: 10px;
          border-radius: 50%;
          
          &.running {
            background: #13ce66;
            box-shadow: 0 0 6px rgba(19, 206, 102, 0.5);
          }
          
          &.stopped {
            background: rgba(255, 255, 255, 0.3);
          }
          
          &.fault {
            background: #ff4444;
            box-shadow: 0 0 6px rgba(255, 68, 68, 0.5);
          }
        }
      }
    }
  }
  
  .device-grid {
    flex: 1;
    overflow-y: auto;
    padding: 15px;
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 15px;
    
    &::-webkit-scrollbar {
      width: 6px;
    }
    
    &::-webkit-scrollbar-thumb {
      background: rgba(143, 164, 204, 0.3);
      border-radius: 3px;
    }
  }
  
  .device-group {
    background: rgba(20, 50, 100, 0.15);
    border: 1px solid rgba(143, 164, 204, 0.2);
    border-radius: 6px;
    overflow: hidden;
    
    .group-title {
      padding: 10px 15px;
      background: rgba(0, 117, 233, 0.15);
      color: #0075e9;
      font-size: 13px;
      font-weight: 500;
      border-bottom: 1px solid rgba(143, 164, 204, 0.2);
    }
    
    .device-cards {
      padding: 12px;
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
      gap: 10px;
    }
  }
  
  .device-card {
    padding: 10px 8px;
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(143, 164, 204, 0.2);
    border-radius: 4px;
    text-align: center;
    cursor: pointer;
    transition: all 0.3s;
    
    &:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 117, 233, 0.2);
    }
    
    .device-tag {
      color: #fff;
      font-size: 12px;
      font-weight: 500;
      margin-bottom: 6px;
      font-family: 'Courier New', monospace;
    }
    
    .device-status {
      color: rgba(255, 255, 255, 0.5);
      font-size: 11px;
    }
    
    &.running {
      background: rgba(19, 206, 102, 0.15);
      border-color: rgba(19, 206, 102, 0.5);
      
      .device-status {
        color: #13ce66;
      }
    }
    
    &.stopped {
      background: rgba(255, 255, 255, 0.02);
      border-color: rgba(143, 164, 204, 0.15);
      
      .device-status {
        color: rgba(255, 255, 255, 0.4);
      }
    }
    
    &.fault {
      background: rgba(255, 68, 68, 0.15);
      border-color: rgba(255, 68, 68, 0.5);
      
      .device-status {
        color: #ff4444;
      }
    }
  }
}

// 按钮样式 - 使用全局样式
// 删除自定义按钮样式，使用 Element Plus 默认样式

// 进度条样式
:deep(.el-progress) {
  .el-progress-bar__outer {
    background-color: rgba(143, 164, 204, 0.1);
    border-radius: 6px;
  }
  
  .el-progress-bar__inner {
    background: linear-gradient(90deg, #0075e9, #00ccff);
    border-radius: 6px;
    transition: width 0.3s ease;
  }
  
  &.is-success {
    .el-progress-bar__inner {
      background: linear-gradient(90deg, #13ce66, #67c23a);
    }
  }
  
  &.is-exception {
    .el-progress-bar__inner {
      background: linear-gradient(90deg, #ff4444, #ff6b6b);
    }
  }
}

// 滚动条样式
:deep(.el-scrollbar) {
  .el-scrollbar__bar {
    &.is-vertical {
      width: 6px;
      
      .el-scrollbar__thumb {
        background: rgba(143, 164, 204, 0.3);
        
        &:hover {
          background: rgba(143, 164, 204, 0.5);
        }
      }
    }
  }
}
</style>
