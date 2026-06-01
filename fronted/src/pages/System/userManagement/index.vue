<template>
    <cm-panel style="height: 56px; display: flex; align-items: center;" class="mb10">
          <el-form :inline="true" :model="queryParams">
            <el-form-item label="用户名">
              <el-input
                v-model="queryParams.username"
                placeholder="请输入用户名"
                clearable
                style="width: 220px"
                @keyup.enter="loadUsers"
              />
            </el-form-item>
            <el-form-item label="状态">
              <el-select v-model="queryParams.isActive" clearable placeholder="请选择状态" style="width: 140px">
                <el-option label="启用" :value="true" />
                <el-option label="停用" :value="false" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="loadUsers">查询</el-button>
              <el-button @click="resetQuery">重置</el-button>
              <el-button type="success" @click="handleAdd">新增用户</el-button>
            </el-form-item>
          </el-form>
    </cm-panel>
    <pagePanelNew style="padding: 10px;">
       
          <el-table v-loading="loading" :data="userList" border height="100%">
            <el-table-column type="index" label="序号" width="70" />
            <el-table-column prop="userId" label="用户ID" width="90" />
            <el-table-column prop="username" label="用户名" min-width="150" />
            <el-table-column prop="roleName" label="角色" min-width="140" />
            <el-table-column prop="phone" label="手机号" min-width="140" />
            <el-table-column prop="email" label="邮箱" min-width="200" show-overflow-tooltip />
            <el-table-column prop="department" label="部门" min-width="140" />
            <el-table-column label="状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.isActive ? 'success' : 'danger'">
                  {{ row.isActive ? '启用' : '停用' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="240" fixed="right" align="center">
              <template #default="{ row }">
                <el-button type="primary" @click="handleEdit(row)">编辑</el-button>
                <el-button type="warning" link @click="handleResetPassword(row)">重置密码</el-button>
                <el-button type="danger" link @click="handleDelete(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>

    </PagePanelNew>
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="560px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item v-if="editingUserId === null" label="密码" prop="password">
          <el-input v-model="form.password" show-password placeholder="请输入密码" />
        </el-form-item>
        <el-form-item label="角色" prop="roleId">
          <el-select v-model="form.roleId" placeholder="请选择角色" style="width: 100%">
            <el-option v-for="role in roleOptions" :key="role.roleId" :label="role.roleName" :value="role.roleId" />
          </el-select>
        </el-form-item>
        <el-form-item label="手机号">
          <el-input v-model="form.phone" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="form.email" placeholder="请输入邮箱" />
        </el-form-item>
        <el-form-item label="部门">
          <el-input v-model="form.department" placeholder="请输入部门" />
        </el-form-item>
        <el-form-item label="状态">
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
import {
  createUser,
  deleteUser,
  getRoleList,
  getUserList,
  updateUser,
  type RoleRecord,
  type UserRecord
} from '@/api/systemManagement'

const loading = ref(false)
const submitting = ref(false)
const dialogVisible = ref(false)
const dialogTitle = ref('新增用户')
const editingUserId = ref<number | null>(null)
const userList = ref<UserRecord[]>([])
const roleOptions = ref<RoleRecord[]>([])
const formRef = ref<FormInstance>()

const queryParams = reactive<{
  username: string
  isActive?: boolean
}>({
  username: '',
  isActive: undefined
})

const form = reactive({
  username: '',
  password: '',
  roleId: undefined as number | undefined,
  email: '',
  phone: '',
  department: '',
  isActive: true
})

const rules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
  roleId: [{ required: true, message: '请选择角色', trigger: 'change' }]
}

const loadRoles = async () => {
  const res = await getRoleList({ isActive: true })
  if (res.code === 200) {
    roleOptions.value = res.data || []
  }
}

const loadUsers = async () => {
  loading.value = true
  try {
    const res = await getUserList({
      username: queryParams.username || undefined,
      isActive: queryParams.isActive
    })
    if (res.code !== 200) {
      throw new Error(res.message || '获取用户列表失败')
    }
    userList.value = res.data || []
  } catch (error: any) {
    ElMessage.error(error.message || '获取用户列表失败')
  } finally {
    loading.value = false
  }
}

const resetQuery = () => {
  queryParams.username = ''
  queryParams.isActive = undefined
  loadUsers()
}

const handleAdd = async () => {
  await loadRoles()
  editingUserId.value = null
  dialogTitle.value = '新增用户'
  dialogVisible.value = true
}

const handleEdit = async (row: UserRecord) => {
  await loadRoles()
  editingUserId.value = row.userId
  dialogTitle.value = '编辑用户'
  form.username = row.username
  form.password = ''
  form.roleId = row.roleId ?? undefined
  form.email = row.email || ''
  form.phone = row.phone || ''
  form.department = row.department || ''
  form.isActive = row.isActive
  dialogVisible.value = true
}

const handleDelete = async (row: UserRecord) => {
  try {
    await ElMessageBox.confirm(`确认删除用户“${row.username}”吗？`, '提示', { type: 'warning' })
    const res = await deleteUser(row.userId)
    if (res.code !== 200) {
      throw new Error(res.message || '删除用户失败')
    }
    ElMessage.success('删除用户成功')
    loadUsers()
  } catch (error: any) {
    if (error !== 'cancel' && error !== 'close') {
      ElMessage.error(error.message || '删除用户失败')
    }
  }
}

const handleResetPassword = async (row: UserRecord) => {
  try {
    const result = await ElMessageBox.prompt(`请输入用户 ${row.username} 的新密码`, '重置密码', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      inputType: 'password',
      inputPattern: /^.{6,}$/,
      inputErrorMessage: '密码长度不能少于 6 位'
    })

    const res = await updateUser(row.userId, {
      username: row.username,
      password: result.value,
      roleId: row.roleId ?? undefined,
      email: row.email || undefined,
      phone: row.phone || undefined,
      department: row.department || undefined,
      isActive: row.isActive
    })

    if (res.code !== 200) {
      throw new Error(res.message || '重置密码失败')
    }
    ElMessage.success('重置密码成功')
  } catch (error: any) {
    if (error !== 'cancel' && error !== 'close') {
      ElMessage.error(error.message || '重置密码失败')
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
      username: form.username.trim(),
      password: form.password.trim() || undefined,
      roleId: form.roleId,
      email: form.email.trim() || undefined,
      phone: form.phone.trim() || undefined,
      department: form.department.trim() || undefined,
      isActive: form.isActive
    }

    const res = editingUserId.value === null
      ? await createUser(payload)
      : await updateUser(editingUserId.value, payload)

    if (res.code !== 200) {
      throw new Error(res.message || '保存用户失败')
    }

    ElMessage.success(editingUserId.value === null ? '新增用户成功' : '编辑用户成功')
    dialogVisible.value = false
    loadUsers()
  } catch (error: any) {
    ElMessage.error(error.message || '保存用户失败')
  } finally {
    submitting.value = false
  }
}

const resetForm = () => {
  formRef.value?.resetFields()
  editingUserId.value = null
  form.username = ''
  form.password = ''
  form.roleId = undefined
  form.email = ''
  form.phone = ''
  form.department = ''
  form.isActive = true
}

onMounted(async () => {
  await loadRoles()
  await loadUsers()
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
