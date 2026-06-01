<template>
  <div class="register-card">
    <div class="register-title">注册账号</div>

    <el-form ref="formRef" :model="formData" :rules="rules" label-width="0" class="register-form" status-icon>
      <el-form-item prop="username">
        <el-input v-model="formData.username" placeholder="请输入用户名" />
      </el-form-item>

      <el-form-item prop="phone">
        <el-input v-model="formData.phone" placeholder="请输入手机号" />
      </el-form-item>

      <el-form-item prop="email">
        <el-input v-model="formData.email" placeholder="请输入邮箱，可不填" />
      </el-form-item>

      <el-form-item prop="password">
        <el-input v-model="formData.password" type="password" show-password placeholder="请输入密码" />
      </el-form-item>

      <el-form-item prop="confirmPassword">
        <el-input v-model="formData.confirmPassword" type="password" show-password placeholder="请确认密码" />
      </el-form-item>

      <el-form-item>
        <el-button type="primary" class="submit-btn" :loading="submitting" @click="handleRegister">注册</el-button>
      </el-form-item>
    </el-form>

    <div class="switch-container">
      <span class="tip" @click="emit('switch-login')">返回登录</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { AxiosError } from 'axios'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { register } from '@/api/auth'

const emit = defineEmits<{
  'register-success': []
  'switch-login': []
}>()

const formRef = ref<FormInstance>()
const submitting = ref(false)
const formData = ref({
  username: '',
  phone: '',
  email: '',
  password: '',
  confirmPassword: ''
})

const rules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  phone: [
    { required: true, message: '请输入手机号', trigger: 'blur' },
    { pattern: /^1\d{10}$/, message: '请输入有效手机号', trigger: 'blur' }
  ],
  email: [
    {
      validator: (_rule, value, callback) => {
        if (!value) {
          callback()
          return
        }
        const ok = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
        callback(ok ? undefined : new Error('请输入有效邮箱'))
      },
      trigger: 'blur'
    }
  ],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
  confirmPassword: [
    { required: true, message: '请再次输入密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== formData.value.password) {
          callback(new Error('两次输入的密码不一致'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ]
}

const handleRegister = async () => {
  if (!formRef.value || submitting.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    const response = await register({
      username: formData.value.username.trim(),
      password: formData.value.password,
      phone: formData.value.phone.trim(),
      email: formData.value.email.trim() || undefined,
      roleCode: 'VISITOR'
    })

    ElMessage.success(response.data.message || '注册成功')
    emit('register-success')
  } catch (error) {
    const axiosError = error as AxiosError<{ error?: string }>
    const message =
      axiosError.response?.data?.error ||
      axiosError.message ||
      '注册失败，请稍后重试'
    ElMessage.error(message)
  } finally {
    submitting.value = false
  }
}
</script>
