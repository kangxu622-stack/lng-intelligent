<template>
  <pagePanelNew class="llm-test-page">
    <div class="llm-test-layout">
      <section class="control-panel">
        <div class="panel-head">
          <h2>模型调用测试</h2>
          <p>用于验证前端、后端和 Ollama 服务之间的调用链是否正常。</p>
        </div>

        <div class="control-grid">
          <label class="field">
            <span>业务类型</span>
            <el-select v-model="bizType" class="field-control">
              <el-option label="智能问答" value="question" />
              <el-option label="故障诊断" value="fault-diagnosis" />
              <el-option label="方案解释" value="scheme-explanation" />
            </el-select>
          </label>

          <label class="field">
            <span>用户 ID</span>
            <el-input-number
              v-model="userId"
              class="field-control"
              :min="1"
              :step="1"
              :controls="false"
            />
          </label>
        </div>

        <label class="field field-block">
          <span>测试问题</span>
          <el-input
            v-model="prompt"
            type="textarea"
            resize="none"
            :rows="5"
            placeholder="请输入测试模型调用的问题"
          />
        </label>

        <div class="action-row">
          <el-button type="primary" :loading="running" @click="runTest">
            开始测试
          </el-button>
          <el-button :disabled="running" @click="resetState">
            重置
          </el-button>
        </div>

        <div class="hint-block">
          <div class="hint-title">成功判定</div>
          <div class="hint-text">接口返回 `code = 200`，且助手回复内容非空。</div>
        </div>
      </section>

      <section class="result-panel">
        <div class="result-summary">
          <div class="summary-card">
            <span class="summary-label">测试结果</span>
            <el-tag :type="statusTagType" effect="dark">{{ statusText }}</el-tag>
          </div>
          <div class="summary-card">
            <span class="summary-label">模型名称</span>
            <strong>{{ result.modelName || '-' }}</strong>
          </div>
          <div class="summary-card">
            <span class="summary-label">会话编码</span>
            <strong>{{ result.conversationCode || '-' }}</strong>
          </div>
          <div class="summary-card">
            <span class="summary-label">响应耗时</span>
            <strong>{{ result.responseMs !== null ? `${result.responseMs} ms` : '-' }}</strong>
          </div>
        </div>

        <div class="detail-panel">
          <div class="detail-line">
            <span>接口消息</span>
            <strong>{{ result.message || '待执行' }}</strong>
          </div>
          <div class="detail-line">
            <span>Token 统计</span>
            <strong>
              {{
                result.totalTokens !== null
                  ? `${result.totalTokens} (prompt ${result.promptTokens || 0} / completion ${result.completionTokens || 0})`
                  : '-'
              }}
            </strong>
          </div>
          <div v-if="errorMessage" class="detail-error">{{ errorMessage }}</div>
        </div>

        <ChatAnswerPanel :messages="messages" />
      </section>
    </div>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({
  name: 'LlmTestCase'
})

import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import ChatAnswerPanel, { type ChatMessageItem } from '../component/ChatAnswerPanel.vue'
import { getCurrentUserId, sendLlmMessage, type LlmMessage } from '@/api/llm'

type BizType = 'question' | 'fault-diagnosis' | 'scheme-explanation'
type RunStatus = 'idle' | 'running' | 'success' | 'failed'

const defaultPrompt = '请回复“模型调用成功”，并补充一句你当前基于什么模型在回答。'

const bizType = ref<BizType>('question')
const userId = ref(Math.max(getCurrentUserId(), 1))
const prompt = ref(defaultPrompt)
const running = ref(false)
const status = ref<RunStatus>('idle')
const errorMessage = ref('')
const messages = ref<ChatMessageItem[]>([])

const result = ref({
  modelName: '',
  conversationCode: '',
  responseMs: null as number | null,
  promptTokens: null as number | null,
  completionTokens: null as number | null,
  totalTokens: null as number | null,
  message: ''
})

const statusText = computed(() => {
  if (status.value === 'running') return '测试中'
  if (status.value === 'success') return '调用成功'
  if (status.value === 'failed') return '调用失败'
  return '待执行'
})

const statusTagType = computed<'info' | 'warning' | 'success' | 'danger'>(() => {
  if (status.value === 'running') return 'warning'
  if (status.value === 'success') return 'success'
  if (status.value === 'failed') return 'danger'
  return 'info'
})

const resetState = () => {
  status.value = 'idle'
  errorMessage.value = ''
  messages.value = []
  result.value = {
    modelName: '',
    conversationCode: '',
    responseMs: null,
    promptTokens: null,
    completionTokens: null,
    totalTokens: null,
    message: ''
  }
}

const readMetrics = (message: LlmMessage) => {
  return {
    promptTokens: message.promptTokens ?? null,
    completionTokens: message.completionTokens ?? null,
    totalTokens: message.totalTokens ?? null,
    responseMs: message.responseMs ?? null
  }
}

const runTest = async () => {
  const content = prompt.value.trim()
  if (!content) {
    ElMessage.warning('请先输入测试问题')
    return
  }

  if (userId.value <= 0) {
    ElMessage.warning('请填写有效的用户 ID')
    return
  }

  running.value = true
  status.value = 'running'
  errorMessage.value = ''
  result.value.message = '正在请求模型接口...'
  messages.value = [
    {
      id: `user-${Date.now()}`,
      role: 'user',
      content
    }
  ]

  try {
    const response = await sendLlmMessage(bizType.value, {
      userId: userId.value,
      content
    })

    const assistantMessage = response.data?.assistantMessage
    if (response.code !== 200 || !assistantMessage?.content?.trim()) {
      throw new Error(response.message || '接口返回异常，或模型回复为空')
    }

    const metrics = readMetrics(assistantMessage)
    result.value = {
      modelName: assistantMessage.modelName || '',
      conversationCode: response.data.conversationCode || '',
      responseMs: metrics.responseMs,
      promptTokens: metrics.promptTokens,
      completionTokens: metrics.completionTokens,
      totalTokens: metrics.totalTokens,
      message: response.message || '模型调用成功'
    }

    messages.value.push({
      id: String(assistantMessage.messageId),
      role: 'assistant',
      content: assistantMessage.content
    })

    status.value = 'success'
    ElMessage.success('模型调用成功')
  } catch (error) {
    status.value = 'failed'
    errorMessage.value = (error as Error).message || '模型调用失败'
    result.value.message = '模型调用失败'
    messages.value.push({
      id: `assistant-error-${Date.now()}`,
      role: 'assistant',
      content: `调用失败：${errorMessage.value}`
    })
    ElMessage.error(errorMessage.value)
  } finally {
    running.value = false
  }
}
</script>

<style scoped lang="scss">
.llm-test-page {
  height: 100%;
  overflow: auto;
}

.llm-test-layout {
  min-height: 100%;
  padding: 12px;
  display: grid;
  grid-template-columns: 360px minmax(0, 1fr);
  gap: 12px;
}

.control-panel,
.result-panel {
  border: 1px solid rgba(48, 188, 255, 0.45);
  background: rgba(4, 23, 44, 0.32);
}

.control-panel {
  padding: 18px 16px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.panel-head h2 {
  margin: 0 0 8px;
  color: #ffffff;
  font-size: 22px;
  font-weight: 600;
}

.panel-head p {
  margin: 0;
  color: rgba(220, 238, 255, 0.7);
  line-height: 1.6;
  font-size: 14px;
}

.control-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 14px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 8px;
  color: #dcedff;
  font-size: 14px;
}

.field-block {
  flex: 1;
}

.field-control {
  width: 100%;
}

.action-row {
  display: flex;
  gap: 12px;
}

.hint-block {
  padding: 14px;
  border: 1px solid rgba(48, 188, 255, 0.24);
  background: rgba(7, 29, 53, 0.56);
}

.hint-title {
  margin-bottom: 6px;
  color: #ffffff;
  font-size: 14px;
  font-weight: 600;
}

.hint-text {
  color: rgba(220, 238, 255, 0.72);
  line-height: 1.6;
  font-size: 13px;
}

.result-panel {
  min-width: 0;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.result-summary {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.summary-card {
  min-width: 0;
  padding: 14px 16px;
  border: 1px solid rgba(48, 188, 255, 0.24);
  background: rgba(7, 29, 53, 0.56);
  display: flex;
  flex-direction: column;
  gap: 8px;
  color: #ffffff;
}

.summary-card strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 15px;
}

.summary-label {
  color: rgba(220, 238, 255, 0.6);
  font-size: 12px;
}

.detail-panel {
  padding: 14px 16px;
  border: 1px solid rgba(48, 188, 255, 0.24);
  background: rgba(7, 29, 53, 0.56);
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.detail-line {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  color: #dcedff;
  line-height: 1.6;
}

.detail-line strong {
  text-align: right;
}

.detail-error {
  color: #ff9a9a;
  line-height: 1.6;
}

@media (max-width: 1280px) {
  .llm-test-layout {
    grid-template-columns: 1fr;
  }

  .result-summary {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
