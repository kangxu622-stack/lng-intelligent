<template>
  <el-dialog v-model="visible" title="模型设置" width="520px" :close-on-click-modal="false">
    <div class="settings-body">
      <div class="status-row">
        <span class="label">当前模型</span>
        <el-tag :type="config.activeProvider === 'ollama' ? 'success' : 'warning'" size="small">
          {{ config.activeProvider === 'ollama' ? '本地 Ollama' : '外部 API' }}
        </el-tag>
        <span class="model-name">{{ config.activeModel }}</span>
      </div>

      <el-divider />

      <el-form label-position="top" size="small">
        <el-form-item label="API Key">
          <el-input
            v-model="form.apiKey"
            type="password"
            show-password
            placeholder="输入 OpenAI 兼容 API Key"
          />
        </el-form-item>
        <el-form-item label="API 地址">
          <el-input v-model="form.baseUrl" placeholder="https://api.deepseek.com/v1" />
        </el-form-item>
        <el-form-item label="模型名称">
          <el-input v-model="form.model" placeholder="deepseek-chat" />
        </el-form-item>
      </el-form>

      <el-alert
        v-if="saveSuccess"
        title="配置已保存"
        type="success"
        :closable="false"
        show-icon
        style="margin-top: 12px;"
      />
    </div>

    <template #footer>
      <el-button @click="visible = false">取消</el-button>
      <el-button type="primary" :loading="saving" @click="saveConfig">保存</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { getLlmConfig, updateLlmConfig, type LlmConfig } from '@/api/llm'
import { ElMessage } from 'element-plus'

const visible = ref(false)
const saving = ref(false)
const saveSuccess = ref(false)

const config = reactive<LlmConfig>({
  activeProvider: 'ollama',
  activeModel: '',
  hasApiKey: false,
  openAiBaseUrl: '',
  openAiModel: ''
})

const form = reactive({
  apiKey: '',
  baseUrl: '',
  model: ''
})

const loadConfig = async () => {
  try {
    const res = await getLlmConfig()
    Object.assign(config, res.data)
    form.baseUrl = res.data.openAiBaseUrl || 'https://api.deepseek.com/v1'
    form.model = res.data.openAiModel || 'deepseek-chat'
    form.apiKey = ''
  } catch {
    ElMessage.error('获取配置失败')
  }
}

const saveConfig = async () => {
  saving.value = true
  saveSuccess.value = false
  try {
    await updateLlmConfig({
      apiKey: form.apiKey || undefined,
      baseUrl: form.baseUrl || undefined,
      model: form.model || undefined
    })
    saveSuccess.value = true
    ElMessage.success('配置已保存')
    await loadConfig()
  } catch {
    ElMessage.error('保存配置失败')
  } finally {
    saving.value = false
  }
}

watch(visible, (val) => {
  if (val) {
    saveSuccess.value = false
    loadConfig()
  }
})

const open = () => {
  visible.value = true
}

defineExpose({ open })
</script>

<style scoped>
.status-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.label {
  font-weight: 500;
  color: var(--td-text-color-primary);
}
.model-name {
  color: var(--td-text-color-secondary);
  font-size: 13px;
}
</style>
