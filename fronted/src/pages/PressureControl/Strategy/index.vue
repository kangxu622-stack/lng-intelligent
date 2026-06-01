<template>
  <div class="pressure-control-container">
    <div class="control-panels">
      <pagePanel headerTitle="压力控制策略" class="full-width-panel">
        <!-- 顶部监控卡片 -->
        <div class="monitor-cards mb10">
          <div class="monitor-card" v-for="(card, index) in monitorCards" :key="index">
            <div class="card-header">
              <span class="card-title">{{ card.title }}</span>
              <span class="card-status" :class="`status-${card.status}`">{{ card.statusText }}</span>
            </div>
            <div class="card-value">{{ card.value }}</div>
            <div class="card-chart">
              <div class="progress-bar" :class="card.color">
                <div class="progress-fill" :style="{ width: card.progress + '%' }"></div>
              </div>
              <div class="progress-labels">
                <span>{{ card.labelLeft }}</span>
                <span>{{ card.labelCenter }}</span>
                <span>{{ card.labelRight }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 工艺流程标题 -->
        <div class="section-title mb10">
          <span>低压总管 H 工艺流程</span>
          <div class="fullscreen-btn" @click="toggleFullscreen">⛶</div>
        </div>

        <!-- 工艺流程区域 -->
        <div class="process-flow mb10">
          <div class="flow-placeholder">
            <p>工艺流程图显示区域</p>
          </div>
        </div>

        <!-- 底部控制面板 -->
        <div class="control-panels-inner">
          <!-- A/B 阀前压力智能控制 -->
          <div class="control-panel">
            <div class="panel-header">
              <h3>A/B 阀前压力智能控制</h3>
              <p class="control-range">控制范围：1.15-1.25MPa</p>
            </div>
            <div class="panel-content">
              <div class="current-pressure">
                <span class="label">当前压力：<strong>1.2MPa</strong></span>
              </div>
              <div class="slider-container">
                <div class="slider">
                  <div class="slider-track">
                    <div class="slider-fill" :style="{ width: slider1Value + '%' }"></div>
                  </div>
                  <div class="slider-labels">
                    <span>1.1</span>
                    <span>1.2</span>
                    <span>1.3</span>
                  </div>
                </div>
              </div>
              <div class="variables">
                <div class="variable">
                  <span class="var-label">操纵变量</span>
                  <span class="var-name">低压泵出口阀开度</span>
                  <span class="var-code">0330-HV-1101</span>
                </div>
              </div>
            </div>
          </div>

          <!-- A/B 阀后压力复杂控制 -->
          <div class="control-panel">
            <div class="panel-header">
              <h3>A/B 阀后压力复杂控制</h3>
              <p class="control-range">目标稳压：0.75MPa</p>
            </div>
            <div class="panel-content">
              <div class="pressure-info">
                <div class="pressure-item">
                  <span class="label">阀后压力：<strong>0.75MPa</strong></span>
                </div>
                <div class="valve-opening">
                  <span class="label">B 阀开度</span>
                  <span class="value">35%</span>
                </div>
              </div>
              <div class="slider-container">
                <div class="slider blue">
                  <div class="slider-track">
                    <div class="slider-fill" :style="{ width: slider2Value + '%' }"></div>
                  </div>
                </div>
              </div>
              <div class="variables">
                <div class="variable">
                  <span class="var-label">操纵变量</span>
                  <div class="var-details">
                    <span>A 阀控制器设定值（SP）：40%</span>
                    <span>B 阀控制器设定值（PV）：38%</span>
                  </div>
                </div>
                <div class="pid-params">
                  <span class="pid-label">PID 参数</span>
                  <span class="pid-values">Kc:-0.01 Ti:30</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </pagePanel>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'

interface MonitorCard {
  title: string
  status: string
  statusText: string
  value: string
  progress: number
  color: string
  labelLeft: string
  labelCenter: string
  labelRight: string
}

const monitorCards = reactive<MonitorCard[]>([])
const slider1Value = ref(50)
const slider2Value = ref(40)
const loading = ref(false)

void loading.value

// 加载监控卡片数据 - 使用本地模拟数据
const loadMonitorCards = () => {
  // 使用本地模拟数据，避免 API 调用失败
  monitorCards.splice(0, monitorCards.length, ...[
    {
      title: '低压总管压力',
      status: 'normal',
      statusText: '正常',
      value: '0.45MPa',
      progress: 75,
      color: 'blue',
      labelLeft: '0.3MPa',
      labelCenter: '0.45MPa',
      labelRight: '0.6MPa'
    },
    {
      title: '阀前压力 H1',
      status: 'stable',
      statusText: '稳定',
      value: '1.20MPa',
      progress: 60,
      color: 'cyan',
      labelLeft: '1.0MPa',
      labelCenter: '1.2MPa',
      labelRight: '1.4MPa'
    },
    {
      title: '阀后压力 H2',
      status: 'stable',
      statusText: '稳定',
      value: '0.75MPa',
      progress: 50,
      color: 'cyan',
      labelLeft: '0.5MPa',
      labelCenter: '0.75MPa',
      labelRight: '1.0MPa'
    },
    {
      title: 'BOG 压力',
      status: 'normal',
      statusText: '正常',
      value: '0.32MPa',
      progress: 40,
      color: 'orange',
      labelLeft: '0.2MPa',
      labelCenter: '0.32MPa',
      labelRight: '0.5MPa'
    }
  ])
}

const toggleFullscreen = () => {
  console.log('切换全屏')
}

// 组件挂载时加载数据
onMounted(() => {
  loadMonitorCards()
})
</script>

<style lang="less" scoped>
.pressure-control-container {
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

.monitor-cards {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;

  .monitor-card {
    background: rgba(20, 50, 100, 0.3);
    border: 1px solid #2a5a8c;
    border-radius: 8px;
    padding: 15px;

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 10px;

      .card-title {
        color: #aaccee;
        font-size: 14px;
      }

      .card-status {
        padding: 2px 8px;
        border-radius: 4px;
        font-size: 12px;

        &.status-normal {
          background: rgba(74, 204, 74, 0.2);
          color: #4acc4a;
          border: 1px solid #4acc4a;
        }

        &.status-stable {
          background: rgba(74, 204, 204, 0.2);
          color: #4acccc;
          border: 1px solid #4acccc;
        }

        &.status-adjusting {
          background: rgba(255, 170, 0, 0.2);
          color: #ffaa00;
          border: 1px solid #ffaa00;
        }
      }
    }

    .card-value {
      font-size: 28px;
      font-weight: 600;
      color: #fff;
      margin-bottom: 15px;
    }

    .card-chart {
      .progress-bar {
        height: 6px;
        background: #1a3a5c;
        border-radius: 3px;
        overflow: hidden;
        margin-bottom: 8px;

        .progress-fill {
          height: 100%;
          background: linear-gradient(90deg, #ff6600, #ffaa00);
          border-radius: 3px;
        }

        &.blue .progress-fill {
          background: linear-gradient(90deg, #0066cc, #0099ff);
        }

        &.orange .progress-fill {
          background: linear-gradient(90deg, #ff8800, #ffaa00);
        }

        &.cyan .progress-fill {
          background: linear-gradient(90deg, #0099cc, #00ccff);
        }
      }

      .progress-labels {
        display: flex;
        justify-content: space-between;
        font-size: 12px;
        color: #88aacc;
      }
    }
  }
}

.section-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: rgba(20, 80, 120, 0.5);
  border: 1px solid #2a5a8c;
  padding: 10px 15px;
  border-radius: 4px;

  span {
    color: #fff;
    font-size: 16px;
    font-weight: 600;
  }

  .fullscreen-btn {
    color: #aaccee;
    cursor: pointer;
    font-size: 18px;
  }
}

.process-flow {
  height: 300px;
  background: rgba(20, 50, 100, 0.2);
  border: 1px solid #2a5a8c;
  border-radius: 8px;

  .flow-placeholder {
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #88aacc;
    font-size: 16px;
  }
}

.control-panels-inner {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20px;

  .control-panel {
    background: rgba(20, 50, 100, 0.3);
    border: 1px solid #2a5a8c;
    border-radius: 8px;
    padding: 20px;

    .panel-header {
      margin-bottom: 20px;

      h3 {
        color: #fff;
        font-size: 18px;
        margin-bottom: 5px;
      }

      .control-range {
        color: #88aacc;
        font-size: 14px;
      }
    }

    .panel-content {
      .current-pressure {
        margin-bottom: 15px;

        .label {
          color: #aaccee;
          font-size: 14px;

          strong {
            color: #ffaa00;
            font-size: 18px;
          }
        }
      }

      .pressure-info {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 15px;

        .label {
          color: #aaccee;
          font-size: 14px;

          strong {
            color: #0099ff;
            font-size: 18px;
          }
        }

        .valve-opening {
          text-align: right;

          .label {
            display: block;
            margin-bottom: 5px;
          }

          .value {
            color: #fff;
            font-size: 24px;
            font-weight: 600;
          }
        }
      }

      .slider-container {
        margin-bottom: 20px;

        .slider {
          .slider-track {
            height: 6px;
            background: #1a3a5c;
            border-radius: 3px;
            overflow: hidden;

            .slider-fill {
              height: 100%;
              background: linear-gradient(90deg, #ff6600, #ffaa00);
              border-radius: 3px;
            }
          }

          .slider-labels {
            display: flex;
            justify-content: space-between;
            margin-top: 8px;
            font-size: 12px;
            color: #88aacc;
          }

          &.blue .slider-fill {
            background: linear-gradient(90deg, #0066cc, #0099ff);
          }
        }
      }

      .variables {
        .variable {
          display: flex;
          flex-direction: column;
          gap: 5px;
          margin-bottom: 15px;

          .var-label {
            color: #88aacc;
            font-size: 12px;
          }

          .var-name {
            color: #fff;
            font-size: 14px;
            font-weight: 600;
          }

          .var-code {
            color: #88aacc;
            font-size: 12px;
          }

          .var-details {
            display: flex;
            flex-direction: column;
            gap: 5px;
            color: #fff;
            font-size: 14px;
          }
        }

        .pid-params {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding-top: 10px;
          border-top: 1px solid rgba(42, 90, 140, 0.3);

          .pid-label {
            color: #88aacc;
            font-size: 12px;
          }

          .pid-values {
            color: #fff;
            font-size: 14px;
          }
        }
      }
    }
  }
}
</style>
