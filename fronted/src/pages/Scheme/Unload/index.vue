<template>
  <div class="unload-page">
    <cm-panel style="height: 56px; display: flex; align-items: center; justify-content: space-between;" class="mb10">
      <div class="toolbar-content">
        <div class="toolbar-left">
          <div class="plan-field">
            <span class="field-label">方案名称</span>
            <el-input v-model="planName" placeholder="请输入方案名称" size="small" class="cm-inline-control" />
          </div>
          <div class="plan-field">
            <span class="field-label">开始时间</span>
            <span class="inline-value">{{ displayStartTime }}</span>
          </div>
          <div class="toolbar-actions">
            <el-button type="primary" size="small" @click="reloadFromStore">加载方案</el-button>
          </div>
        </div>
      </div>
    </cm-panel>

    <pagePanelNew class="screen-shell">
      <div class="screen-grid">
        <section class="screen-card summary-card">
          <div class="section-title">
            <span class="title-bar"></span>
            <span>卸船概览</span>
          </div>

          <div class="param-list">
            <div class="param-row">
              <span class="param-label">调度模板</span>
              <span class="param-value">标准卸船阶段计划</span>
            </div>
            <div class="param-row">
              <span class="param-label">计划时长</span>
              <span class="param-value">{{ totalDurationHours.toFixed(2) }} h</span>
            </div>
            <div class="param-row">
              <span class="param-label">作业阶段</span>
              <span class="param-value">{{ unloadScheduleRows.length }} 段</span>
            </div>
            <div class="param-row">
              <span class="param-label">卸船总量</span>
              <span class="param-value">{{ formatNumber(totalUnloadM3, 0) }} m3</span>
            </div>
            <div class="param-row">
              <span class="param-label">峰值卸量</span>
              <span class="param-value">{{ formatNumber(maxFlowM3h, 0) }} m3/h</span>
            </div>
            <div class="param-row">
              <span class="param-label">当前用途</span>
              <span class="param-value">页面展示与调度说明</span>
            </div>
          </div>
        </section>

        <section class="screen-card timeline-card">
          <div class="card-head">
            <div class="section-title compact">
              <span class="title-bar"></span>
              <span>卸船阶段时间轴</span>
            </div>
          </div>

          <div class="timeline-shell">
            <div v-for="item in unloadScheduleRows" :key="item.id" class="timeline-row">
              <div class="timeline-meta">
                <div class="timeline-title">{{ item.stage }}</div>
                <div class="timeline-time">{{ item.startLabel }} - {{ item.endLabel }}</div>
              </div>
              <div class="timeline-track">
                <div class="timeline-bar" :style="buildTimelineStyle(item)">
                  <span class="bar-label">{{ formatNumber(item.flowM3h, 0) }}</span>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section class="screen-card table-card">
          <div class="card-head trend-head">
            <div class="section-title compact">
              <span class="title-bar"></span>
              <span>卸船阶段明细</span>
            </div>
          </div>

          <div class="table-box">
            <el-table :data="unloadScheduleRows" height="100%" class="trend-table" fit>
              <el-table-column prop="stageIndex" label="序号" min-width="72" align="center" />
              <el-table-column prop="stage" label="阶段" min-width="160" />
              <el-table-column prop="startLabel" label="开始" min-width="120" />
              <el-table-column prop="endLabel" label="结束" min-width="120" />
              <el-table-column prop="durationHoursLabel" label="时长(h)" min-width="100" align="right" />
              <el-table-column prop="flowLabel" label="卸船量(m3/h)" min-width="128" align="right" />
              <el-table-column prop="unloadVolumeLabel" label="阶段卸量(m3)" min-width="128" align="right" />
            </el-table>
          </div>
        </section>

        <div class="metrics-column">
          <div v-for="item in metricCards" :key="item.label" class="metric-card">
            <div class="metric-top">
              <div class="metric-label">{{ item.label }}</div>
              <span class="metric-dot" :style="{ background: item.color }"></span>
            </div>
            <div class="metric-desc">{{ item.desc }}</div>
            <div class="metric-number" :style="{ color: item.color }">
              {{ item.value }}
              <span class="metric-unit">{{ item.unit }}</span>
            </div>
          </div>
        </div>
      </div>
    </pagePanelNew>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: 'UnloadScheduling'
})

import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useSchemeStore } from '@/store/modules/scheme'

interface RawUnloadStage {
  startHour: number
  startMinute: number
  startDayOffset: number
  endHour: number
  endMinute: number
  endDayOffset: number
  flowM3h: number
  stage: string
}

interface UnloadScheduleRow {
  id: string
  stageIndex: number
  stage: string
  startMinuteOffset: number
  endMinuteOffset: number
  durationHours: number
  durationHoursLabel: string
  flowM3h: number
  flowLabel: string
  unloadVolumeM3: number
  unloadVolumeLabel: string
  startLabel: string
  endLabel: string
}

interface MetricCard {
  label: string
  desc: string
  value: string
  unit: string
  color: string
}

const schemeStore = useSchemeStore()

const unloadScheduleTemplate: RawUnloadStage[] = [
  { startHour: 11, startMinute: 0, startDayOffset: 0, endHour: 12, endMinute: 30, endDayOffset: 0, flowM3h: 0, stage: '引航靠泊' },
  { startHour: 12, startMinute: 30, startDayOffset: 0, endHour: 14, endMinute: 30, endDayOffset: 0, flowM3h: 0, stage: '系统建立' },
  { startHour: 14, startMinute: 50, startDayOffset: 0, endHour: 15, endMinute: 50, endDayOffset: 0, flowM3h: 0, stage: 'CIQ与MSA联检' },
  { startHour: 15, startMinute: 50, startDayOffset: 0, endHour: 16, endMinute: 50, endDayOffset: 0, flowM3h: 0, stage: '安全检查及卸前' },
  { startHour: 16, startMinute: 50, startDayOffset: 0, endHour: 17, endMinute: 20, endDayOffset: 0, flowM3h: 0, stage: '吹扫置换' },
  { startHour: 17, startMinute: 20, startDayOffset: 0, endHour: 17, endMinute: 40, endDayOffset: 0, flowM3h: 0, stage: '初始计量' },
  { startHour: 17, startMinute: 40, startDayOffset: 0, endHour: 18, endMinute: 0, endDayOffset: 0, flowM3h: 0, stage: '热态ESD测试' },
  { startHour: 18, startMinute: 0, startDayOffset: 0, endHour: 19, endMinute: 30, endDayOffset: 0, flowM3h: 2000, stage: '卸料臂预冷' },
  { startHour: 19, startMinute: 30, startDayOffset: 0, endHour: 19, endMinute: 50, endDayOffset: 0, flowM3h: 0, stage: '冷态ESD测试' },
  { startHour: 19, startMinute: 50, startDayOffset: 0, endHour: 20, endMinute: 50, endDayOffset: 0, flowM3h: 6000, stage: '卸料升速' },
  { startHour: 20, startMinute: 50, startDayOffset: 0, endHour: 10, endMinute: 30, endDayOffset: 1, flowM3h: 12000, stage: '卸料全速' },
  { startHour: 10, startMinute: 30, startDayOffset: 1, endHour: 11, endMinute: 30, endDayOffset: 1, flowM3h: 4000, stage: '卸料降速' },
  { startHour: 11, startMinute: 30, startDayOffset: 1, endHour: 12, endMinute: 20, endDayOffset: 1, flowM3h: 1200, stage: '岸侧排凝' },
  { startHour: 12, startMinute: 20, startDayOffset: 1, endHour: 12, endMinute: 50, endDayOffset: 1, flowM3h: 600, stage: '船侧排凝' },
  { startHour: 12, startMinute: 50, startDayOffset: 1, endHour: 13, endMinute: 10, endDayOffset: 1, flowM3h: 0, stage: '吹扫置换' },
  { startHour: 13, startMinute: 10, startDayOffset: 1, endHour: 13, endMinute: 30, endDayOffset: 1, flowM3h: 0, stage: '末次计量' },
  { startHour: 13, startMinute: 30, startDayOffset: 1, endHour: 14, endMinute: 30, endDayOffset: 1, flowM3h: 0, stage: '拆臂' },
  { startHour: 14, startMinute: 30, startDayOffset: 1, endHour: 15, endMinute: 0, endDayOffset: 1, flowM3h: 0, stage: '卸后会议' }
]

const planName = ref('')

const toMinuteOffset = (hour: number, minute: number, dayOffset: number) => dayOffset * 24 * 60 + hour * 60 + minute

const pad = (value: number) => String(value).padStart(2, '0')

const formatStageTime = (hour: number, minute: number, dayOffset: number) =>
  `D${dayOffset} ${pad(hour)}:${pad(minute)}`

const unloadScheduleRows = computed<UnloadScheduleRow[]>(() =>
  unloadScheduleTemplate.map((item, index) => {
    const startMinuteOffset = toMinuteOffset(item.startHour, item.startMinute, item.startDayOffset)
    const endMinuteOffset = toMinuteOffset(item.endHour, item.endMinute, item.endDayOffset)
    const durationHours = (endMinuteOffset - startMinuteOffset) / 60
    const unloadVolumeM3 = durationHours * item.flowM3h

    return {
      id: `${index + 1}-${item.stage}`,
      stageIndex: index + 1,
      stage: item.stage,
      startMinuteOffset,
      endMinuteOffset,
      durationHours,
      durationHoursLabel: durationHours.toFixed(2),
      flowM3h: item.flowM3h,
      flowLabel: formatNumber(item.flowM3h, 0),
      unloadVolumeM3,
      unloadVolumeLabel: formatNumber(unloadVolumeM3, 0),
      startLabel: formatStageTime(item.startHour, item.startMinute, item.startDayOffset),
      endLabel: formatStageTime(item.endHour, item.endMinute, item.endDayOffset)
    }
  })
)

const totalDurationHours = computed(() => unloadScheduleRows.value.reduce((sum, item) => sum + item.durationHours, 0))
const totalUnloadM3 = computed(() => unloadScheduleRows.value.reduce((sum, item) => sum + item.unloadVolumeM3, 0))
const maxFlowM3h = computed(() => Math.max(...unloadScheduleRows.value.map((item) => item.flowM3h), 0))
const activeUnloadHours = computed(() =>
  unloadScheduleRows.value.filter((item) => item.flowM3h > 0).reduce((sum, item) => sum + item.durationHours, 0)
)
const displayStartTime = computed(() => schemeStore.initialCondition.startTime || '-')

const metricCards = computed<MetricCard[]>(() => [
  {
    label: '满速时长',
    desc: '卸料全速阶段持续时间',
    value: unloadScheduleRows.value.find((item) => item.stage === '卸料全速')?.durationHoursLabel || '0.00',
    unit: 'h',
    color: '#f97316'
  },
  {
    label: '作业窗口',
    desc: '存在卸船流量的总时长',
    value: activeUnloadHours.value.toFixed(2),
    unit: 'h',
    color: '#0f766e'
  },
  {
    label: '预冷+升速',
    desc: '启卸前关键工况准备时间',
    value: (
      (unloadScheduleRows.value.find((item) => item.stage === '卸料臂预冷')?.durationHours || 0) +
      (unloadScheduleRows.value.find((item) => item.stage === '卸料升速')?.durationHours || 0)
    ).toFixed(2),
    unit: 'h',
    color: '#2563eb'
  }
])

const buildTimelineStyle = (item: UnloadScheduleRow) => {
  const totalMinutes = Math.max(unloadScheduleRows.value[unloadScheduleRows.value.length - 1]?.endMinuteOffset || 1, 1)
  const left = (item.startMinuteOffset / totalMinutes) * 100
  const width = ((item.endMinuteOffset - item.startMinuteOffset) / totalMinutes) * 100
  const background = item.flowM3h > 0 ? 'linear-gradient(90deg, #2563eb 0%, #38bdf8 100%)' : 'linear-gradient(90deg, #cbd5e1 0%, #94a3b8 100%)'

  return {
    left: `${left}%`,
    width: `${Math.max(width, 2.4)}%`,
    background
  }
}

const reloadFromStore = () => {
  planName.value = schemeStore.initialCondition.planName || ''
  ElMessage.success('已加载当前方案的卸船模板')
}

const formatNumber = (value: number | null | undefined, digits = 2) =>
  value === null || value === undefined || Number.isNaN(value) ? '-' : Number(value).toFixed(digits)

reloadFromStore()
</script>

<style scoped lang="less">
.unload-page {
  min-height: 100%;
}

.toolbar-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
}

.toolbar-actions {
  display: flex;
  gap: 10px;
}

.plan-field {
  display: flex;
  align-items: center;
  gap: 10px;
}

.field-label {
  color: #475569;
  font-size: 13px;
  white-space: nowrap;
}

.inline-value {
  min-width: 148px;
  color: #0f172a;
  font-size: 13px;
  font-weight: 600;
}

.screen-shell {
  height: calc(100% - 66px);
  padding: 12px;
}

.screen-grid {
  display: grid;
  grid-template-columns: 320px minmax(0, 1fr) 260px;
  grid-template-rows: 320px minmax(0, 1fr);
  gap: 12px;
  height: 100%;
}

.screen-card {
  border-radius: 18px;
  background: linear-gradient(180deg, #f8fbff 0%, #eef5fb 100%);
  border: 1px solid rgba(148, 163, 184, 0.18);
  box-shadow: 0 18px 42px rgba(15, 23, 42, 0.08);
  padding: 18px 18px 16px;
  min-height: 0;
}

.summary-card {
  grid-column: 1;
  grid-row: 1;
}

.timeline-card {
  grid-column: 2;
  grid-row: 1;
}

.table-card {
  grid-column: 1 / span 2;
  grid-row: 2;
}

.metrics-column {
  grid-column: 3;
  grid-row: 1 / span 2;
  display: grid;
  grid-template-rows: repeat(3, 1fr);
  gap: 12px;
  min-height: 0;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 18px;
  color: #0f172a;
  font-size: 17px;
  font-weight: 700;
}

.section-title.compact {
  margin-bottom: 0;
}

.title-bar {
  width: 6px;
  height: 18px;
  border-radius: 999px;
  background: linear-gradient(180deg, #2563eb 0%, #0ea5e9 100%);
}

.card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18px;
}

.param-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.param-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 11px 12px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.72);
}

.param-label {
  color: #64748b;
  font-size: 13px;
}

.param-value {
  color: #0f172a;
  font-size: 14px;
  font-weight: 600;
  text-align: right;
}

.timeline-shell {
  display: flex;
  flex-direction: column;
  gap: 14px;
  height: calc(100% - 28px);
  overflow: auto;
  padding-right: 4px;
}

.timeline-row {
  display: grid;
  grid-template-columns: 184px minmax(0, 1fr);
  gap: 14px;
  align-items: center;
}

.timeline-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.timeline-title {
  color: #0f172a;
  font-size: 14px;
  font-weight: 700;
}

.timeline-time {
  color: #64748b;
  font-size: 12px;
}

.timeline-track {
  position: relative;
  height: 18px;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(226, 232, 240, 0.95) 0%, rgba(241, 245, 249, 0.95) 100%);
  overflow: hidden;
}

.timeline-bar {
  position: absolute;
  top: 0;
  height: 100%;
  min-width: 28px;
  border-radius: 999px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 6px;
  box-shadow: 0 8px 20px rgba(37, 99, 235, 0.2);
}

.bar-label {
  color: #fff;
  font-size: 11px;
  font-weight: 700;
  line-height: 1;
}

.table-box {
  height: calc(100% - 30px);
}

.trend-table :deep(.el-table__header th) {
  background: rgba(226, 232, 240, 0.75);
  color: #334155;
}

.metric-card {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 18px;
  border-radius: 18px;
  background: linear-gradient(160deg, rgba(255, 255, 255, 0.95) 0%, rgba(231, 241, 250, 0.92) 100%);
  border: 1px solid rgba(148, 163, 184, 0.16);
  box-shadow: 0 16px 36px rgba(15, 23, 42, 0.08);
}

.metric-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.metric-label {
  color: #0f172a;
  font-size: 15px;
  font-weight: 700;
}

.metric-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  box-shadow: 0 0 0 6px rgba(148, 163, 184, 0.14);
}

.metric-desc {
  margin-top: 10px;
  color: #64748b;
  font-size: 13px;
  line-height: 1.6;
}

.metric-number {
  margin-top: 18px;
  font-size: 30px;
  font-weight: 800;
  line-height: 1;
}

.metric-unit {
  margin-left: 4px;
  font-size: 14px;
  font-weight: 600;
  color: #64748b;
}

@media (max-width: 1360px) {
  .screen-grid {
    grid-template-columns: 1fr;
    grid-template-rows: auto;
    height: auto;
  }

  .summary-card,
  .timeline-card,
  .table-card,
  .metrics-column {
    grid-column: auto;
    grid-row: auto;
  }

  .metrics-column {
    grid-template-rows: none;
    grid-template-columns: 1fr;
  }

  .timeline-row {
    grid-template-columns: 1fr;
  }
}
</style>
