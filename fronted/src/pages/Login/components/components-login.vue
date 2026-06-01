<template>
  <div class="login-card">
    <div class="tabline" />
    <div class="loginInput">
      <t-form
        ref="formRef"
        class="formDiv"
        :data="formData"
        :rules="rules"
        label-width="0"
        @validate="onValidate"
      >

      <t-form-item name="username">
        <div class="textDiv">用户名</div>
        <el-input v-model="formData.username" class="innerInput" placeholder="请输入用户名" />
        <div class="bottomBorderDiv" />
      </t-form-item>

      <t-form-item name="password">
        <div class="textDiv">密码</div>
        <el-input
          v-model="formData.password"
          class="innerInput"
          placeholder="请输入密码"
          show-password
          @keyup.enter="handleLogin"
        />
        <div class="bottomBorderDiv" />
      </t-form-item>

        <div class="loginBtn">
          <t-form-item class="btn-container">
            <el-button size="small" type="primary" :loading="submitting" @click="handleLogin">登录</el-button>
            <el-button size="small" :disabled="submitting" @click="handleReset">重置</el-button>
          </t-form-item>
        </div>
      </t-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { AxiosError } from 'axios'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance } from 'element-plus'
import { login } from '@/api/auth'
import { TOKEN_NAME, USER_INFO_KEY, USER_NAME } from '@/config/global'

const router = useRouter()
const formRef = ref<FormInstance>()
const submitting = ref(false)

const formData = ref({
  username: '',
  password: ''
})

const rules = {
  username: [{ required: true, message: '请输入用户名', type: 'error' }],
  password: [{ required: true, message: '请输入密码', type: 'error' }]
}

const finishLogin = async () => {
  await router.push('/home/overview')
}

const onValidate = () => {}

const handleLogin = async () => {
  const valid = await (formRef.value as any)?.validate?.().catch(() => false)
  if (!valid || submitting.value) return

  submitting.value = true
  try {
    const response = await login({
      username: formData.value.username.trim(),
      password: formData.value.password
    })

    const result = response.data.data
    localStorage.setItem(TOKEN_NAME, String(result.userId))
    localStorage.setItem(USER_NAME, result.username)
    localStorage.setItem(USER_INFO_KEY, JSON.stringify({
      userId: result.userId,
      username: result.username,
      roleId: result.roleId,
      roleCode: result.roleCode,
      roleName: result.roleName
    }))

    ElMessage.success(result.message || '登录成功')
    await finishLogin()
  } catch (error) {
    const axiosError = error as AxiosError<{ error?: string }>
    const message =
      axiosError.response?.data?.error ||
      axiosError.message ||
      '登录失败，请稍后重试'
    ElMessage.error(message)
  } finally {
    submitting.value = false
  }
}


const handleReset = () => {
  formData.value.username = ''
  formData.value.password = ''
}
</script>

<style scoped>
.loginInput {
  padding: 20px;
}

.loginBtn {
  margin-top: 20px;
}

.loginBtn .el-button {
  padding: 8px 20px;
  font-size: 14px;
}

.login-title {
  font-size: 18px;
  font-weight: bold;
  text-align: center;
  margin-bottom: 20px;
  color: #333;
}
</style>
