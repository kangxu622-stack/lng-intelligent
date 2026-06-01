const fs = require('fs');

const content = `<template>
  <div class="cm-container-wrapper p10 scheme-container">
    <cm-panel class="mb10 flex toolbar-panel">
      <el-button type="primary" @click="loadData">加载调度数据</el-button>
      <el-button type="success" @click="exportData">导出结果</el-button>
    </cm-panel>
    
    <div class="charts-grid">
      <pagePanel headerTitle="BOG预测对比" class="chart-panel">
        <scEcharts height="100%" :option="bogChartOption" />
      </pagePanel>
      
      <pagePanel headerTitle="压缩机档位" class="chart-panel">
        <scEcharts height="100%" :option="compChartOption" />
      </pagePanel>
      
      <pagePanel headerTitle="低压泵理想压力" class="chart-panel">
        <scEcharts height="100%" :option="lpPressureOption" />
      </pagePanel>
      
      <pagePanel headerTitle="高压泵理想压力" class="chart-panel">
        <scEcharts height="100%" :option="hpPressureOption" />
      </pagePanel>
      
      <pagePanel headerTitle="储罐液位变化" class="chart-panel">
        <scEcharts height="100%" :option="tankLevelOption" />
      </pagePanel>
      
      <pagePanel headerTitle="高压泵台数与电价" class="chart-panel">
        <scEcharts height="100%" :option="hpPriceOption" />
      </pagePanel>
      
      <pagePanel headerTitle="低压泵启停状态" class="chart-panel heatmap-panel">
        <scEcharts height="100%" :option="lpHeatmapOption" />
      </pagePanel>
      
      <pagePanel headerTitle="高压泵启停状态" class="chart-panel heatmap-panel">
        <scEcharts height="100%" :option="hpHeatmapOption" />
      </pagePanel>
      
      <pagePanel headerTitle="海水大泵启停状态" class="chart-panel heatmap-panel">
        <scEcharts height="100%" :option="swBigHeatmapOption" />
      </pagePanel>
      
      <pagePanel headerTitle="海水小泵启停状态" class="chart-panel heatmap-panel">
        <scEcharts height="100%" :option="swSmallHeatmapOption" />
      </pagePanel>
      
      <pagePanel headerTitle="ORV启停状态" class="chart-panel heatmap-panel">
        <scEcharts height="100%" :option="orvHeatmapOption" />
      </pagePanel>
      
      <pagePanel headerTitle="压缩机档位热力图" class="chart-panel heatmap-panel">
        <scEcharts height="100%" :option="compHeatmapOption" />
      </pagePanel>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import scEcharts from '@/components/scEcharts/index.vue'
import { calculate } from '@/api/simulation'

const simulationData = ref({
  hours: Array.from({ length: 24 }, (_, i) => i),
  bog: {
    bog_mech_kgph: Array.from({ length: 24 }, () => 1000 + Math.random() * 500),
    bog_pred_kgph: Array.from({ length: 24 }, () => 1100 + Math.random() * 400)
  },
  df_hours: {
    Ideal_LP_Pressure_MPa: Array.from({ length: 24 }, () => 1.15 + Math.random() * 0.1),
    Ideal_HP_Pressure_MPa: Array.from({ length: 24 }, () => 9 + Math.random() * 4),
    Tank_Level_m: Array.from({ length: 24 }, () => 15 + Math.random() * 15),
    HP_Num: Array.from({ length: 24 }, () => Math.floor(3 + Math.random() * 5)),
    Elec_Price: [0.45, 0.45, 0.45, 0.45, 0.45, 0.45, 0.45, 0.60, 0.60, 0.60, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.60, 0.60, 0.60, 0.60, 0.45, 0.45]
  },
  unitized: {
    LP: Array.from({ length: 24 }, () => Array.from({ length: 16 }, () => Math.random() > 0.5 ? 1 : 0)),
    HP: Array.from({ length: 24 }, () => Array.from({ length: 8 }, () => Math.random() > 0.5 ? 1 : 0)),
    SW_Big: Array.from({ length: 24 }, () => Array.from({ length: 2 }, () => Math.random() > 0.5 ? 1 : 0)),
    SW_Small: Array.from({ length: 24 }, () => Array.from({ length: 4 }, () => Math.random() > 0.5 ? 1 : 0)),
    ORV: Array.from({ length: 24 }, () => Array.from({ length: 7 }, () => Math.random() > 0.5 ? 1 : 0)),
    Comp: Array.from({ length: 24 }, () => [Math.random(), Math.random()])
  },
  params: {
    Level_Max: 35.311,
    Level_Min: 3
  }
})

const loading = ref(false)

const loadData = async () => {
  loading.value = true
  try {
    ElMessage.success('数据加载成功')
  } catch (error) {
    ElMessage.error('数据加载失败')
  } finally {
    loading.value = false
  }
}

const exportData = () => {
  ElMessage.info('导出功能开发中...')
}

const bogChartOption = computed(() => {
  const data = simulationData.value
  return {
    tooltip: { trigger: 'axis' },
    legend: { data: ['BOG mech', 'BOG pred'], textStyle: { color: '#fff' } },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: data.hours.map(h => h + 'h'),
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: {
      type: 'value',
      name: 'BOG (kg/h)',
      nameTextStyle: { color: '#fff' },
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } },
      splitLine: { lineStyle: { color: '#333' } }
    },
    series: [
      {
        name: 'BOG mech',
        type: 'line',
        data: data.bog.bog_mech_kgph,
        lineStyle: { type: 'dashed', width: 1.5 },
        itemStyle: { color: '#1f77b4' }
      },
      {
        name: 'BOG pred',
        type: 'line',
        data: data.bog.bog_pred_kgph,
        lineStyle: { width: 2 },
        itemStyle: { color: '#ff7f0e' }
      }
    ]
  }
})

const compChartOption = computed(() => {
  const data = simulationData.value
  return {
    tooltip: { trigger: 'axis' },
    legend: { data: ['Comp1', 'Comp2'], textStyle: { color: '#fff' } },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: data.hours.map(h => h + 'h'),
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: {
      type: 'value',
      min: 0,
      max: 1,
      name: '档位',
      nameTextStyle: { color: '#fff' },
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } },
      splitLine: { lineStyle: { color: '#333' } }
    },
    series: [
      {
        name: 'Comp1',
        type: 'line',
        data: data.unitized.Comp.map(c => c[0]),
        itemStyle: { color: '#2E86AB' },
        areaStyle: { opacity: 0.3 }
      },
      {
        name: 'Comp2',
        type: 'line',
        data: data.unitized.Comp.map(c => c[1]),
        itemStyle: { color: '#A23B72' },
        areaStyle: { opacity: 0.3 }
      }
    ]
  }
})

const lpPressureOption = computed(() => {
  const data = simulationData.value
  return {
    tooltip: { trigger: 'axis' },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: data.hours.map(h => h + 'h'),
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: {
      type: 'value',
      name: 'Pressure (MPa)',
      nameTextStyle: { color: '#fff' },
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } },
      splitLine: { lineStyle: { color: '#333' } }
    },
    series: [{
      type: 'line',
      data: data.df_hours.Ideal_LP_Pressure_MPa,
      symbol: 'rect',
      symbolSize: 8,
      itemStyle: { color: '#5470c6' },
      lineStyle: { width: 2 }
    }]
  }
})

const hpPressureOption = computed(() => {
  const data = simulationData.value
  return {
    tooltip: { trigger: 'axis' },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: data.hours.map(h => h + 'h'),
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: {
      type: 'value',
      name: 'Pressure (MPa)',
      nameTextStyle: { color: '#fff' },
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } },
      splitLine: { lineStyle: { color: '#333' } }
    },
    series: [{
      type: 'line',
      data: data.df_hours.Ideal_HP_Pressure_MPa,
      symbol: 'triangle',
      symbolSize: 10,
      itemStyle: { color: '#91cc75' },
      lineStyle: { width: 2 }
    }]
  }
})

const tankLevelOption = computed(() => {
  const data = simulationData.value
  return {
    tooltip: { trigger: 'axis' },
    legend: { data: ['Tank Level', 'High-High', 'Low-Low'], textStyle: { color: '#fff' } },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: data.hours.map(h => h + 'h'),
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: {
      type: 'value',
      name: 'Level (m)',
      nameTextStyle: { color: '#fff' },
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } },
      splitLine: { lineStyle: { color: '#333' } }
    },
    series: [
      {
        name: 'Tank Level',
        type: 'line',
        data: data.df_hours.Tank_Level_m,
        symbol: 'circle',
        symbolSize: 8,
        itemStyle: { color: '#5470c6' },
        lineStyle: { width: 2 }
      },
      {
        name: 'High-High',
        type: 'line',
        data: Array(24).fill(data.params.Level_Max),
        lineStyle: { type: 'dashed', color: '#ff0000' },
        symbol: 'none',
        itemStyle: { color: '#ff0000' }
      },
      {
        name: 'Low-Low',
        type: 'line',
        data: Array(24).fill(data.params.Level_Min),
        lineStyle: { type: 'dashed', color: '#000000' },
        symbol: 'none',
        itemStyle: { color: '#000000' }
      }
    ]
  }
})

const hpPriceOption = computed(() => {
  const data = simulationData.value
  return {
    tooltip: { trigger: 'axis' },
    legend: { data: ['HP Count', 'Price'], textStyle: { color: '#fff' } },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: data.hours.map(h => h + 'h'),
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: [
      {
        type: 'value',
        name: 'HP Count',
        nameTextStyle: { color: '#fff' },
        axisLabel: { color: '#fff' },
        axisLine: { lineStyle: { color: '#fff' } },
        splitLine: { lineStyle: { color: '#333' } }
      },
      {
        type: 'value',
        name: 'Price (CNY/kWh)',
        nameTextStyle: { color: '#fff' },
        axisLabel: { color: '#fff' },
        axisLine: { lineStyle: { color: '#fff' } },
        splitLine: { show: false }
      }
    ],
    series: [
      {
        name: 'HP Count',
        type: 'bar',
        data: data.df_hours.HP_Num,
        itemStyle: { color: '#5470c6', opacity: 0.7 }
      },
      {
        name: 'Price',
        type: 'line',
        yAxisIndex: 1,
        data: data.df_hours.Elec_Price,
        symbol: 'circle',
        lineStyle: { color: '#ff0000' },
        itemStyle: { color: '#ff0000' }
      }
    ]
  }
})

const createHeatmapOption = (data: number[][], title: string, yLabels: string[]) => {
  const hours = Array.from({ length: 24 }, (_, i) => i + 'h')
  const heatmapData: [number, number, number][] = []
  
  data.forEach((row, hourIndex) => {
    row.forEach((value, deviceIndex) => {
      heatmapData.push([hourIndex, deviceIndex, value])
    })
  })
  
  return {
    tooltip: {
      position: 'top',
      formatter: (params: any) => {
        return '时间: ' + hours[params.value[0]] + '<br/>设备: ' + yLabels[params.value[1]] + '<br/>状态: ' + (params.value[2] ? '运行' : '停止')
      }
    },
    grid: { left: '15%', right: '5%', top: '10%', bottom: '15%' },
    xAxis: {
      type: 'category',
      data: hours,
      axisLabel: { color: '#fff', interval: 2 },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: {
      type: 'category',
      data: yLabels,
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    visualMap: {
      min: 0,
      max: 1,
      calculable: false,
      orient: 'horizontal',
      left: 'center',
      bottom: '0%',
      show: true,
      inRange: {
        color: ['#313695', '#a50026']
      },
      textStyle: { color: '#fff' }
    },
    series: [{
      type: 'heatmap',
      data: heatmapData,
      label: { show: false },
      itemStyle: {
        borderColor: '#fff',
        borderWidth: 0.5
      }
    }]
  }
}

const lpHeatmapOption = computed(() => {
  const labels = Array.from({ length: 16 }, (_, i) => 'LP' + String.fromCharCode(65 + i))
  return createHeatmapOption(simulationData.value.unitized.LP, '低压泵', labels)
})

const hpHeatmapOption = computed(() => {
  const labels = Array.from({ length: 8 }, (_, i) => 'HP' + String.fromCharCode(65 + i))
  return createHeatmapOption(simulationData.value.unitized.HP, '高压泵', labels)
})

const swBigHeatmapOption = computed(() => {
  return createHeatmapOption(simulationData.value.unitized.SW_Big, '海水大泵', ['A', 'B'])
})

const swSmallHeatmapOption = computed(() => {
  return createHeatmapOption(simulationData.value.unitized.SW_Small, '海水小泵', ['A', 'B', 'C', 'D'])
})

const orvHeatmapOption = computed(() => {
  return createHeatmapOption(simulationData.value.unitized.ORV, 'ORV', ['A', 'B', 'C', 'D', 'E', 'F', 'G'])
})

const compHeatmapOption = computed(() => {
  const data = simulationData.value
  const hours = Array.from({ length: 24 }, (_, i) => i + 'h')
  const heatmapData: [number, number, number][] = []
  
  data.unitized.Comp.forEach((row, hourIndex) => {
    row.forEach((value, compIndex) => {
      heatmapData.push([hourIndex, compIndex, value])
    })
  })
  
  return {
    tooltip: {
      position: 'top',
      formatter: (params: any) => '时间: ' + hours[params.value[0]] + '<br/>压缩机: Comp' + (params.value[1] + 1) + '<br/>档位: ' + Math.round(params.value[2] * 100) + '%'
    },
    grid: { left: '15%', right: '5%', top: '10%', bottom: '15%' },
    xAxis: {
      type: 'category',
      data: hours,
      axisLabel: { color: '#fff', interval: 2 },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    yAxis: {
      type: 'category',
      data: ['Comp1', 'Comp2'],
      axisLabel: { color: '#fff' },
      axisLine: { lineStyle: { color: '#fff' } }
    },
    visualMap: {
      min: 0,
      max: 1,
      calculable: false,
      orient: 'horizontal',
      left: 'center',
      bottom: '0%',
      show: true,
      inRange: {
        color: ['#440154', '#31688e', '#35b779', '#fde725']
      },
      textStyle: { color: '#fff' }
    },
    series: [{
      type: 'heatmap',
      data: heatmapData,
      label: { show: false },
      itemStyle: {
        borderColor: '#fff',
        borderWidth: 0.5
      }
    }]
  }
})

onMounted(() => {
  loadData()
})
</script>

<style scoped lang="scss">
.scheme-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}
.toolbar-panel {
  height: 56px;
  display: flex;
  align-items: center;
  padding: 0 10px;
}
.charts-grid {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  grid-template-rows: repeat(4, 280px);
  gap: 10px;
  overflow-y: auto;
  padding: 10px;
}
.chart-panel {
  min-height: 280px;
}
.heatmap-panel {
  min-height: 280px;
}
@media (max-width: 1400px) {
  .charts-grid {
    grid-template-columns: repeat(2, 1fr);
    grid-template-rows: repeat(6, 280px);
  }
}
@media (max-width: 900px) {
  .charts-grid {
    grid-template-columns: 1fr;
    grid-template-rows: repeat(11, 280px);
  }
}
</style>
`;

fs.writeFileSync(process.argv[2], content, 'utf-8');
console.log('File written successfully');
