<template>
  <div class="report-page">
    <cm-panel style="height: 56px; display: flex; align-items: center;" class="mb10">
      <div class="toolbar-actions">
        <el-button type="primary" :disabled="!selectedPlan" @click="downloadCsvArchive">下载 CSV 压缩包</el-button>
        <el-button :disabled="!selectedPlan || !reportSheets.length" @click="exportWorkbook">下载 Excel</el-button>
        <el-button type="danger" plain :disabled="!selectedPlan" @click="handleDelete">删除方案</el-button>
      </div>
    </cm-panel>

    <pagePanelNew class="content-shell">
      <section class="content-layout">
        <aside class="left-panel">
          <div class="panel-surface">
            <div class="panel-header">
              <div class="panel-title">方案列表</div>
              <div class="panel-subtitle">共 {{ planList.length }} 个方案</div>
            </div>

            <el-tree
              v-loading="listLoading"
              :data="treeData"
              node-key="key"
              class="plan-tree"
              :expand-on-click-node="false"
              default-expand-all
              highlight-current
              @node-click="handleNodeClick"
            >
              <template #default="{ data }">
                <div v-if="data.type === 'group'" class="tree-group-node">
                  <span>{{ data.label }}</span>
                  <span class="tree-count">{{ data.count }}</span>
                </div>
                <div v-else class="tree-plan-node">
                  <span class="tree-plan-title">{{ data.label }}</span>
                </div>
              </template>
            </el-tree>
          </div>
        </aside>

        <main class="right-panel">
          <div class="panel-surface">
            <div class="panel-header panel-header-inline">
              <div class="header-copy">
                <div class="panel-title">{{ selectedPlan?.planName || selectedPlan?.planCode || '请选择一个方案' }}</div>
                <div class="panel-subtitle">
                  {{
                    selectedPlan
                      ? `创建时间：${formatDateTime(selectedPlan.createdAt)}`
                      : '请先在左侧选择方案，再下载 CSV 或 Excel。'
                  }}
                </div>
              </div>
            </div>

            <div class="summary-grid">
              <div class="summary-item">
                <span class="summary-label">状态</span>
                <span class="summary-value">{{ selectedPlan?.status || '-' }}</span>
              </div>
              <div class="summary-item">
                <span class="summary-label">总外输量</span>
                <span class="summary-value">{{ formatNumber(selectedPlan?.totalOutputM3) }}</span>
              </div>
              <div class="summary-item">
                <span class="summary-label">总耗电量</span>
                <span class="summary-value">{{ formatNumber(selectedPlan?.totalPowerKwh) }}</span>
              </div>
              <div class="summary-item">
                <span class="summary-label">优化得分</span>
                <span class="summary-value">{{ formatNumber(selectedPlan?.optimizationScore) }}</span>
              </div>
              <div class="summary-item">
                <span class="summary-label">适应度值</span>
                <span class="summary-value">{{ formatNumber(selectedPlan?.fitnessValue) }}</span>
              </div>
            </div>

            <div class="table-card export-card">
              <div class="table-title">报表导出</div>
              <div class="export-actions">
                <el-button type="primary" :disabled="!selectedPlan || detailLoading" @click="downloadCsvArchive">下载 CSV 压缩包</el-button>
                <el-button :disabled="!selectedPlan || detailLoading || !reportSheets.length" @click="exportWorkbook">下载 Excel 工作簿</el-button>
              </div>
              <div class="export-hint">
                {{ selectedPlan ? `已生成 ${reportSheets.length} 份报表数据，可直接下载。` : '请选择一个方案后再进行导出。' }}
              </div>
            </div>
          </div>
        </main>
      </section>
    </pagePanelNew>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: 'SchemeReport'
})

import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import * as XLSX from 'xlsx'
import type { SchedulePlan, ScheduleReportSheet } from '@/api/schedules'
import { deleteSchedulePlan, downloadSchedulePlanCsv, getSchedulePlanDetail, getSchedulePlans } from '@/api/schedules'

interface TreeNode {
  key: string
  label: string
  type: 'group' | 'plan'
  count?: number
  plan?: SchedulePlan
  children?: TreeNode[]
}

const listLoading = ref(false)
const detailLoading = ref(false)
const planList = ref<SchedulePlan[]>([])
const selectedPlan = ref<SchedulePlan | null>(null)
const reportSheets = ref<ScheduleReportSheet[]>([])

const buildTreeData = (items: SchedulePlan[]): TreeNode[] => {
  const groups = new Map<string, SchedulePlan[]>()

  items.forEach((item) => {
    const groupKey = item.status || '未分类'
    if (!groups.has(groupKey)) {
      groups.set(groupKey, [])
    }
    groups.get(groupKey)!.push(item)
  })

  return Array.from(groups.entries()).map(([status, plans]) => ({
    key: `group-${status}`,
    label: status,
    type: 'group',
    count: plans.length,
    children: plans.map((item) => ({
      key: `plan-${item.planId}`,
      label: item.planName || item.planCode,
      type: 'plan',
      plan: item
    }))
  }))
}

const treeData = computed(() => buildTreeData(planList.value))

const loadPlanList = async () => {
  listLoading.value = true
  try {
    const res = await getSchedulePlans({ pageIndex: 1, pageSize: 200 })
    if (res.code !== 200 || !res.data) {
      throw new Error(res.message || '加载方案列表失败')
    }

    planList.value = res.data.items || []

    if (!planList.value.length) {
      selectedPlan.value = null
      reportSheets.value = []
      return
    }

    const currentId = selectedPlan.value?.planId
    const target = planList.value.find((item) => item.planId === currentId) || planList.value[0]
    if (target) {
      await loadPlanDetail(target.planId)
    }
  } catch (error) {
    ElMessage.error((error as Error).message)
  } finally {
    listLoading.value = false
  }
}

const loadPlanDetail = async (planId: number) => {
  detailLoading.value = true
  try {
    const res = await getSchedulePlanDetail(planId)
    if (res.code !== 200 || !res.data) {
      throw new Error(res.message || '加载方案详情失败')
    }

    selectedPlan.value = res.data.plan
    reportSheets.value = res.data.reportSheets || []
  } catch (error) {
    ElMessage.error((error as Error).message)
  } finally {
    detailLoading.value = false
  }
}

const handleNodeClick = (node: TreeNode) => {
  if (node.type !== 'plan' || !node.plan) return
  void loadPlanDetail(node.plan.planId)
}

const exportWorkbook = () => {
  if (!selectedPlan.value || !reportSheets.value.length) {
    ElMessage.warning('当前没有可导出的报表数据')
    return
  }

  const workbook = XLSX.utils.book_new()
  reportSheets.value.forEach((sheet) => {
    const worksheet = XLSX.utils.json_to_sheet(sheet.rows || [], { header: sheet.columns })
    XLSX.utils.book_append_sheet(workbook, worksheet, sheet.sheetName)
  })

  XLSX.writeFile(workbook, `${selectedPlan.value.planName || selectedPlan.value.planCode || 'schedule-report'}.xlsx`)
}

const downloadCsvArchive = async () => {
  if (!selectedPlan.value) return

  try {
    const response = await downloadSchedulePlanCsv(selectedPlan.value.planId)
    const blob = new Blob([response.data], { type: 'application/zip' })
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url

    const disposition = response.headers?.['content-disposition'] as string | undefined
    const encodedName = disposition?.match(/filename\*=UTF-8''([^;]+)/)?.[1]
    const plainName = disposition?.match(/filename="?([^";]+)"?/)?.[1]
    link.download = decodeURIComponent(encodedName || plainName || `${selectedPlan.value.planCode || 'schedule-report'}-csv.zip`)

    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error((error as Error).message || 'CSV 导出失败')
  }
}

const handleDelete = async () => {
  if (!selectedPlan.value) return

  try {
    await ElMessageBox.confirm(`确认删除方案“${selectedPlan.value.planName || selectedPlan.value.planCode}”吗？删除后不可恢复。`, '删除方案', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })

    const res = await deleteSchedulePlan(selectedPlan.value.planId)
    if (res.code !== 200) {
      throw new Error(res.message || '删除方案失败')
    }

    ElMessage.success('方案已删除')
    selectedPlan.value = null
    reportSheets.value = []
    await loadPlanList()
  } catch (error: any) {
    if (error !== 'cancel' && error !== 'close') {
      ElMessage.error(error.message || '删除方案失败')
    }
  }
}

function formatNumber(value: number | null | undefined, digits = 2) {
  if (value === null || value === undefined) return '-'
  const num = Number(value)
  return Number.isFinite(num) ? num.toFixed(digits) : '-'
}

function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value || '-'

  const yyyy = date.getFullYear()
  const mm = `${date.getMonth() + 1}`.padStart(2, '0')
  const dd = `${date.getDate()}`.padStart(2, '0')
  const hh = `${date.getHours()}`.padStart(2, '0')
  const mi = `${date.getMinutes()}`.padStart(2, '0')
  return `${yyyy}-${mm}-${dd} ${hh}:${mi}`
}

onMounted(async () => {
  await loadPlanList()
})
</script>
<style scoped lang="scss">
.report-page {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.toolbar-actions {
  display: flex;
  gap: 12px;
  align-items: center;
}

.content-shell {
  flex: 1;
  min-height: 0;
  overflow: hidden;
}

.content-layout {
  display: grid;
  grid-template-columns: 380px minmax(0, 1fr);
  gap: 12px;
  height: 100%;
  padding: 10px;
}

.left-panel,
.right-panel {
  min-height: 0;
}

.panel-surface {
  height: 100%;
  min-height: 0;
  display: flex;
  flex-direction: column;
  padding: 12px;
  background: rgba(6, 28, 57, 0.62);
  border: 1px solid rgba(58, 169, 255, 0.22);
}

.panel-header {
  margin-bottom: 12px;
}

.panel-header-inline {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.header-copy {
  min-width: 0;
}

.panel-title {
  color: #eaf4ff;
  font-size: 18px;
  font-weight: 600;
}

.panel-subtitle {
  margin-top: 6px;
  color: rgba(220, 238, 255, 0.72);
  font-size: 13px;
}

.plan-tree {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.plan-tree :deep(.el-tree-node__content) {
  min-height: 36px;
  padding: 6px 0;
}

.tree-group-node,
.tree-plan-node {
  width: 100%;
  min-width: 0;
}

.tree-group-node {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  color: #dff3ff;
  font-weight: 600;
}

.tree-count {
  color: #7ed8ff;
}

.tree-plan-title {
  color: #eef8ff;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sheet-segmented-wrap {
  flex: 1;
  min-width: 0;
  max-width: 100%;
  overflow-x: auto;
  padding-bottom: 4px;
}

.sheet-segmented-wrap :deep(.el-segmented) {
  width: max-content;
  max-width: none;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
  margin-bottom: 12px;
}

.summary-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 10px 12px;
  background: rgba(7, 29, 53, 0.72);
  border: 1px solid rgba(48, 188, 255, 0.16);
}

.summary-label {
  color: rgba(220, 238, 255, 0.72);
  font-size: 12px;
}

.summary-value {
  color: #eef8ff;
  font-size: 18px;
  font-weight: 600;
}

.table-card {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.table-title {
  margin-bottom: 8px;
  color: #eaf4ff;
  font-size: 15px;
  font-weight: 600;
}

.export-card {
  justify-content: center;
  gap: 16px;
}

.export-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.export-hint {
  color: rgba(220, 238, 255, 0.72);
  font-size: 13px;
}

.report-table {
  flex: 1;
  min-height: 0;
}

.report-table :deep(.el-table) {
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: rgba(7, 29, 53, 0.72);
  --el-table-border-color: rgba(48, 188, 255, 0.16);
  --el-table-text-color: #eaf4ff;
  --el-table-header-text-color: rgba(220, 238, 255, 0.82);
}

@media (max-width: 1440px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 1280px) {
  .content-layout {
    grid-template-columns: 1fr;
  }

  .panel-header-inline {
    flex-direction: column;
  }
}

@media (max-width: 768px) {
  .summary-grid {
    grid-template-columns: 1fr;
  }
}
</style>
