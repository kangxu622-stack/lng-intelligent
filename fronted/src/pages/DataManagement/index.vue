<template>
  <pagePanelNew class="data-page">
    <div class="data-layout">
      <aside class="tree-panel">
        <el-tree
          :data="treeData"
          node-key="key"
          default-expand-all
          highlight-current
          :expand-on-click-node="false"
          :props="{ label: 'label', children: 'children' }"
          class="data-tree"
          @node-click="handleNodeClick"
        />
      </aside>

      <main class="table-panel">
        <div class="panel-head">
          <div class="head-title">
            <h2>{{ tableDefinition?.displayName || '数据管理' }}</h2>
           
          </div>
          <div class="head-actions">
            <el-input
              v-model="keyword"
              placeholder="输入关键字查询"
              class="search-input cm-inline-control"
              clearable
              @keyup.enter="loadTablePage"
            />
            <el-button type="primary" @click="openCreateDialog" :disabled="!tableDefinition">添加</el-button>
            <el-button type="warning" @click="openEditDialog" :disabled="!selectedRow || !tableDefinition">编辑</el-button>
            <el-button type="danger" @click="handleDelete" :disabled="!selectedRow || !tableDefinition">删除</el-button>
            <el-button @click="loadTablePage" :disabled="!tableDefinition">刷新</el-button>
          </div>
        </div>

        <div class="table-wrapper">
          <el-table
            v-loading="loading"
            :data="tableRows"
            fit
            height="100%"
            class="static-table"
            highlight-current-row
            @current-change="handleCurrentChange"
          >
            <el-table-column type="index" min-width="60" label="#" />
            <el-table-column
              v-for="column in displayColumns"
              :key="column.name"
              :prop="column.name"
              :label="column.label"
              :min-width="columnMinWidth(column)"
              show-overflow-tooltip
            >
              <template #default="{ row }">
                {{ formatCell(row[column.name], column) }}
              </template>
            </el-table-column>
          </el-table>
        </div>

        <div class="footer-bar">
          <div class="schema-tip">
            字段 {{ displayColumns.length || 0 }} 个，当前共 {{ total }} 条数据
          </div>
          <el-pagination
            v-model:current-page="pagination.pageIndex"
            v-model:page-size="pagination.pageSize"
            :page-sizes="[20, 50, 100]"
            layout="total, sizes, prev, pager, next"
            :total="total"
            @current-change="loadTablePage"
            @size-change="handleSizeChange"
          />
        </div>
      </main>
    </div>

    <el-dialog
      v-model="dialogVisible"
      :title="dialogMode === 'create' ? '新增数据' : '编辑数据'"
      width="760px"
      class="dark-data-dialog"
    >
      <el-form label-width="140px" class="edit-form">
        <el-form-item v-for="column in editableColumns" :key="column.name" :label="column.label">
          <el-switch
            v-if="isBooleanColumn(column)"
            v-model="formState[column.name]"
          />
          <el-input-number
            v-else-if="isNumericColumn(column)"
            v-model="formState[column.name]"
            :controls="false"
            style="width: 100%"
          />
          <el-date-picker
            v-else-if="isDateColumn(column)"
            v-model="formState[column.name]"
            type="datetime"
            value-format="YYYY-MM-DD HH:mm:ss"
            format="YYYY-MM-DD HH:mm:ss"
            style="width: 100%"
          />
          <el-input
            v-else-if="isLongTextColumn(column)"
            v-model="formState[column.name]"
            type="textarea"
            :rows="3"
          />
          <el-input v-else v-model="formState[column.name]" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({
  name: 'DataManagement'
})

import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { DataTableColumn, DataTableDefinition, DataTreeNode } from '@/api/dataManagement'
import {
  createDataRow,
  deleteDataRow,
  getDataTablePage,
  getDataTree,
  updateDataRow
} from '@/api/dataManagement'

const treeData = ref<DataTreeNode[]>([])
const currentTableName = ref('')
const tableDefinition = ref<DataTableDefinition | null>(null)
const tableRows = ref<Record<string, any>[]>([])
const total = ref(0)
const loading = ref(false)
const saving = ref(false)
const selectedRow = ref<Record<string, any> | null>(null)
const keyword = ref('')
const dialogVisible = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const formState = reactive<Record<string, any>>({})

const pagination = reactive({
  pageIndex: 1,
  pageSize: 20
})

const isUserVisibleColumn = (column: DataTableColumn) => !column.isForeignKey

const visibleColumns = computed(() =>
  (tableDefinition.value?.columns ?? []).filter(isUserVisibleColumn)
)

const displayColumns = computed(() => visibleColumns.value)

const editableColumns = computed(() => {
  const columns = visibleColumns.value
  return columns.filter((column) => {
    if (dialogMode.value === 'create') {
      return !column.isAutoIncrement
    }
    return !column.isPrimaryKey
  })
})

const isBooleanColumn = (column: DataTableColumn) =>
  column.dataType.toLowerCase() === 'tinyint(1)' || column.dataType.toLowerCase() === 'bit(1)'
const isNumericColumn = (column: DataTableColumn) =>
  /int|decimal|double|float|numeric/i.test(column.dataType) && !isBooleanColumn(column)
const isDateColumn = (column: DataTableColumn) => /date|time|timestamp/i.test(column.dataType)
const isLongTextColumn = (column: DataTableColumn) => /text|json/i.test(column.dataType)

const columnMinWidth = (column: DataTableColumn) => {
  if (isLongTextColumn(column)) return 220
  if (isDateColumn(column)) return 180
  return 140
}

const formatCell = (value: any, column: DataTableColumn) => {
  if (value === null || value === undefined || value === '') return '-'
  if (isBooleanColumn(column)) return value ? '是' : '否'
  return `${value}`
}

const resetForm = () => {
  Object.keys(formState).forEach((key) => {
    delete formState[key]
  })
}

const initializeForm = (row?: Record<string, any> | null) => {
  resetForm()
  const columns = editableColumns.value
  columns.forEach((column) => {
    const sourceValue = row ? row[column.name] : null
    if (sourceValue !== null && sourceValue !== undefined) {
      formState[column.name] = sourceValue
      return
    }

    if (isBooleanColumn(column)) {
      formState[column.name] = false
      return
    }

    formState[column.name] = column.defaultValue ?? ''
  })
}

const buildPayload = () => {
  const payload: Record<string, any> = {}
  editableColumns.value.forEach((column) => {
    const value = formState[column.name]
    if ((value === '' || value === undefined) && column.isNullable) {
      payload[column.name] = null
      return
    }
    payload[column.name] = value
  })
  return payload
}

const loadTree = async () => {
  const res = await getDataTree()
  if (res.code !== 200 || !res.data) {
    throw new Error(res.message || '数据树加载失败')
  }
  treeData.value = res.data

  const firstLeaf = treeData.value.flatMap((item) => item.children).find((item) => item.tableName)
  if (firstLeaf?.tableName) {
    currentTableName.value = firstLeaf.tableName
    await loadTablePage()
  }
}

const loadTablePage = async () => {
  if (!currentTableName.value) return

  loading.value = true
  try {
    const res = await getDataTablePage(currentTableName.value, {
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize,
      keyword: keyword.value || undefined
    })

    if (res.code !== 200 || !res.data) {
      throw new Error(res.message || '数据表查询失败')
    }

    tableDefinition.value = res.data.definition
    tableRows.value = res.data.items
    total.value = res.data.totalCount
    selectedRow.value = null
  } catch (error) {
    ElMessage.error((error as Error).message)
  } finally {
    loading.value = false
  }
}

const handleNodeClick = async (node: DataTreeNode) => {
  if (!node.tableName) return
  currentTableName.value = node.tableName
  pagination.pageIndex = 1
  keyword.value = ''
  await loadTablePage()
}

const handleCurrentChange = (row: Record<string, any> | null) => {
  selectedRow.value = row
}

const openCreateDialog = () => {
  if (!tableDefinition.value) return
  dialogMode.value = 'create'
  if (editableColumns.value.length === 0) {
    ElMessage.warning('当前数据表没有适合直接维护的字段，请先补充上游基础数据。')
    return
  }
  initializeForm()
  dialogVisible.value = true
}

const openEditDialog = () => {
  if (!tableDefinition.value || !selectedRow.value) return
  dialogMode.value = 'edit'
  if (editableColumns.value.length === 0) {
    ElMessage.warning('当前数据表没有适合直接维护的字段，请先补充上游基础数据。')
    return
  }
  initializeForm(selectedRow.value)
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!tableDefinition.value) return
  saving.value = true
  try {
    const payload = buildPayload()
    const primaryKeyName = tableDefinition.value.primaryKeyName

    const res = dialogMode.value === 'create'
      ? await createDataRow(currentTableName.value, payload)
      : await updateDataRow(currentTableName.value, `${selectedRow.value?.[primaryKeyName]}`, payload)

    if (res.code !== 200) {
      throw new Error(res.message || '保存失败')
    }

    ElMessage.success(dialogMode.value === 'create' ? '新增成功' : '更新成功')
    dialogVisible.value = false
    await loadTablePage()
  } catch (error) {
    ElMessage.error(resolveSaveErrorMessage(error))
  } finally {
    saving.value = false
  }
}

const handleDelete = async () => {
  if (!tableDefinition.value || !selectedRow.value) return

  try {
    await ElMessageBox.confirm('确认删除当前选中数据吗？', '删除确认', {
      type: 'warning'
    })

    const primaryKeyValue = selectedRow.value[tableDefinition.value.primaryKeyName]
    const res = await deleteDataRow(currentTableName.value, `${primaryKeyValue}`)
    if (res.code !== 200) {
      throw new Error(res.message || '删除失败')
    }

    ElMessage.success('删除成功')
    await loadTablePage()
  } catch (error) {
    if ((error as Error).message !== 'cancel') {
      ElMessage.error((error as Error).message)
    }
  }
}

const handleSizeChange = () => {
  pagination.pageIndex = 1
  void loadTablePage()
}

const resolveSaveErrorMessage = (error: unknown) => {
  const message = (error as Error)?.message || '保存失败'
  if (/请先添加/.test(message)) {
    return message
  }
  return message
}

onMounted(async () => {
  try {
    await loadTree()
  } catch (error) {
    ElMessage.error((error as Error).message)
  }
})
</script>

<style scoped lang="scss">
.data-page {
  height: 100% !important;
  overflow: hidden;
}

.data-layout {
  height: 100%;
  min-height: 0;
  box-sizing: border-box;
  padding: 12px;
  display: flex;
}

.tree-panel,
.table-panel {
  border: 1px solid rgba(48, 188, 255, 0.24);
  background: rgba(7, 29, 53, 0.52);
  min-height: 0;
  overflow: auto;
}

.tree-panel {
  color: #fff;
  width: clamp(220px, 20vw, 320px);
  flex: 0 1 clamp(220px, 20vw, 320px);
  min-width: 220px;
  max-width: 320px;
  margin-right: 10px;
  display: flex;
  flex-direction: column;
}

.tree-header {
  padding: 16px 18px;
  font-size: 18px;
  font-weight: 600;
  border-bottom: 1px solid rgba(48, 188, 255, 0.18);
}

.data-tree {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.tree-panel :deep(.el-tree) {
  background: transparent;
  color: #ffffff;
  padding: 10px 8px 16px;
}

.tree-panel :deep(.el-tree-node__content) {
  height: 36px;
  border-radius: 4px;
  color: #ffffff;
}

.tree-panel :deep(.el-tree-node__label) {
  color: #ffffff !important;
}

.tree-panel :deep(.el-tree-node) {
  color: #ffffff;
}

.tree-panel :deep(.el-tree-node__content:hover) {
  background: rgba(48, 188, 255, 0.12);
}

.tree-panel :deep(.el-tree-node.is-current > .el-tree-node__content) {
  background: linear-gradient(90deg, rgba(10, 68, 112, 0.92), rgba(17, 116, 184, 0.72));
  box-shadow: inset 0 0 0 1px rgba(64, 194, 255, 0.28);
  color: #fff;
}

.tree-panel :deep(.el-tree-node__expand-icon) {
  color: #ffffff;
}

.table-panel {
  flex: 1;
  min-width: 0;
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
  overflow: auto;
}

.panel-head {
  padding: 16px 18px 8px;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
}

.head-title h2 {
  margin: 0 0 6px;
  font-size: 24px;
  color: #ffffff;
}

.head-title p {
  margin: 0;
  font-size: 13px;
  color: #ffffff;
}

.head-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.search-input {
  width: 220px;
}

.search-input :deep(.el-input__wrapper) {
  background: rgba(7, 31, 52, 0.92) !important;
  box-shadow: 0 0 0 1px #00c8ff inset, 0 0 8px rgba(0, 200, 255, 0.22) !important;
}

.search-input :deep(.el-input__inner) {
  color: #d9ecff !important;
}

.search-input :deep(.el-input__inner::placeholder) {
  color: #ffffff;
}

.table-wrapper {
  min-height: 0;
  padding: 0 12px 12px;
  overflow: auto;
}

.static-table {
  height: 100%;
  width: 100%;
}

.static-table :deep(.el-table) {
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: rgba(18, 93, 148, 0.64);
  --el-table-border-color: rgba(39, 174, 255, 0.24);
  --el-table-row-hover-bg-color: rgba(22, 123, 190, 0.2);
  --el-table-text-color: #eef8ff;
  --el-table-header-text-color: #eef8ff;
}

.static-table :deep(.el-table__header .cell) {
  white-space: normal;
  word-break: break-word;
  line-height: 1.4;
  overflow: visible;
  text-overflow: clip;
}

.static-table :deep(.el-table__inner-wrapper::before) {
  display: none;
}

.footer-bar {
  padding: 12px 18px;
  border-top: 1px solid rgba(48, 188, 255, 0.16);
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
}

.schema-tip {
  color: #ffffff;
  font-size: 13px;
}

.footer-bar :deep(.el-pagination) {
  --el-pagination-button-bg-color: transparent;
  --el-pagination-button-color: #d9ecff;
  --el-pagination-text-color: #d9ecff;
  --el-pagination-hover-color: #27d4ff;
}

.edit-form {
  max-height: 60vh;
  overflow: auto;
  padding-right: 10px;
}

.dark-data-dialog :deep(.el-dialog) {
  background: #123149;
  border: 1px solid rgba(48, 188, 255, 0.28);
}

.dark-data-dialog :deep(.el-dialog__title),
.dark-data-dialog :deep(.el-form-item__label) {
  color: #eef8ff;
}

.dark-data-dialog :deep(.el-dialog__body) {
  color: #d9ecff;
}

@media (max-width: 1200px) {
  .data-layout {
    flex-direction: column;
  }

  .tree-panel {
    width: 100%;
    flex-basis: auto;
    margin-right: 0;
    margin-bottom: 10px;
  }

  .panel-head,
  .footer-bar {
    flex-direction: column;
    align-items: stretch;
  }

  .search-input {
    width: 100%;
  }
}
</style>
