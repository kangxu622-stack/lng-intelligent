<template>
  <cm-panel style="height: 56px; display: flex; align-items: center;" class="mb10">
    <el-form :inline="true" :model="queryParams">
      <el-form-item label="角色名称/编码">
        <el-input
          v-model="queryParams.keyword"
          placeholder="请输入角色名称或角色编码"
          clearable
          style="width: 240px"
          @keyup.enter="loadRoles"
        />
      </el-form-item>
      <el-form-item label="状态">
        <el-select v-model="queryParams.isActive" clearable placeholder="请选择状态" style="width: 140px">
          <el-option label="启用" :value="true" />
          <el-option label="停用" :value="false" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="loadRoles">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
        <el-button type="success" @click="handleAdd">新增角色</el-button>
      </el-form-item>
    </el-form>
  </cm-panel>

  <pagePanelNew style="padding: 10px;">
    <el-table v-loading="loading" :data="roleList" border height="100%">
      <el-table-column type="index" label="序号" width="70" />
      <el-table-column prop="roleId" label="角色ID" width="90" />
      <el-table-column prop="roleName" label="角色名称" min-width="180" />
      <el-table-column prop="roleCode" label="角色编码" min-width="180" />
      <el-table-column prop="userCount" label="关联用户数" width="120" align="center" />
      <el-table-column label="状态" width="100" align="center">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'danger'">
            {{ row.isActive ? '启用' : '停用' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180" fixed="right" align="center">
        <template #default="{ row }">
          <el-button type="primary"  @click="handleEdit(row)">编辑</el-button>
          <el-button type="danger" link @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>
  </pagePanelNew>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="520px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="角色名称" prop="roleName">
          <el-input v-model="form.roleName" placeholder="请输入角色名称" />
        </el-form-item>
        <el-form-item label="角色编码" prop="roleCode">
          <el-input v-model="form.roleCode" placeholder="请输入角色编码" />
        </el-form-item>
        <el-form-item label="状态" prop="isActive">
          <el-switch v-model="form.isActive" inline-prompt active-text="启用" inactive-text="停用" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="submitForm">保存</el-button>
      </template>
    </el-dialog>

</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { createRole, deleteRole, getRoleList, updateRole, type RoleRecord } from '@/api/systemManagement'

const loading = ref(false)
const submitting = ref(false)
const roleList = ref<RoleRecord[]>([])
const dialogVisible = ref(false)
const dialogTitle = ref('新增角色')
const editingRoleId = ref<number | null>(null)
const formRef = ref<FormInstance>()

const queryParams = reactive<{
  keyword: string
  isActive?: boolean
}>({
  keyword: '',
  isActive: undefined
})

const form = reactive({
  roleCode: '',
  roleName: '',
  isActive: true
})

const rules: FormRules = {
  roleName: [{ required: true, message: '请输入角色名称', trigger: 'blur' }],
  roleCode: [{ required: true, message: '请输入角色编码', trigger: 'blur' }]
}

const loadRoles = async () => {
  loading.value = true
  try {
    const res = await getRoleList({
      keyword: queryParams.keyword || undefined,
      isActive: queryParams.isActive
    })
    if (res.code !== 200) {
      throw new Error(res.message || '获取角色列表失败')
    }
    roleList.value = res.data || []
  } catch (error: any) {
    ElMessage.error(error.message || '获取角色列表失败')
  } finally {
    loading.value = false
  }
}

const resetQuery = () => {
  queryParams.keyword = ''
  queryParams.isActive = undefined
  loadRoles()
}

const handleAdd = () => {
  editingRoleId.value = null
  dialogTitle.value = '新增角色'
  dialogVisible.value = true
}

const handleEdit = (row: RoleRecord) => {
  editingRoleId.value = row.roleId
  dialogTitle.value = '编辑角色'
  form.roleCode = row.roleCode
  form.roleName = row.roleName
  form.isActive = row.isActive
  dialogVisible.value = true
}

const handleDelete = async (row: RoleRecord) => {
  try {
    await ElMessageBox.confirm(`确认删除角色“${row.roleName}”吗？`, '提示', { type: 'warning' })
    const res = await deleteRole(row.roleId)
    if (res.code !== 200) {
      throw new Error(res.message || '删除角色失败')
    }
    ElMessage.success('删除角色成功')
    loadRoles()
  } catch (error: any) {
    if (error !== 'cancel' && error !== 'close') {
      ElMessage.error(error.message || '删除角色失败')
    }
  }
}

const submitForm = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    const payload = {
      roleCode: form.roleCode.trim(),
      roleName: form.roleName.trim(),
      isActive: form.isActive
    }

    const res = editingRoleId.value === null
      ? await createRole(payload)
      : await updateRole(editingRoleId.value, payload)

    if (res.code !== 200) {
      throw new Error(res.message || '保存角色失败')
    }

    ElMessage.success(editingRoleId.value === null ? '新增角色成功' : '编辑角色成功')
    dialogVisible.value = false
    loadRoles()
  } catch (error: any) {
    ElMessage.error(error.message || '保存角色失败')
  } finally {
    submitting.value = false
  }
}

const resetForm = () => {
  formRef.value?.resetFields()
  editingRoleId.value = null
  form.roleCode = ''
  form.roleName = ''
  form.isActive = true
}

onMounted(() => {
  loadRoles()
})
</script>

<style scoped lang="scss">
.system-page {
  width: 100%;
  height: 100%;
  padding: 10px;
  background: rgba(20, 50, 100, 0.3);
}

.full-panel {
  width: 100%;
  height: 100%;
}

.page-inner {
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.toolbar,
.table-wrap {
  padding: 20px;
  border-radius: 8px;
  border: 1px solid rgba(100, 150, 255, 0.3);
  background: rgba(30, 60, 100, 0.4);
}

.table-wrap {
  flex: 1;
  min-height: 0;
}
</style>
