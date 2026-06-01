<template>
  <pagePanelNew class="question-page">
    <div class="question-layout">
      <SessionSidebar
        :conversations="conversations"
        :active-conversation-id="activeConversationId"
        @new-conversation="startNewConversation"
        @restore-history="restoreHistoryConversation"
        @select-conversation="selectConversation"
        @delete-conversation="deleteConversation"
      />

      <section class="question-main">
        <cm-panel class="intro-panel" style="height: 45px;">
          <p class="intro-text">请输入 LNG 调度相关问题，系统会结合当前业务场景给出智能问答结果。</p>
          <el-button class="settings-btn" :icon="Setting" size="small" @click="settingsRef?.open()">设置</el-button>
        </cm-panel>

        <ChatAnswerPanel :messages="messages" />

        <div class="quick-question-row">
          <button
            v-for="item in quickQuestions"
            :key="item"
            class="quick-question"
            @click="useQuickQuestion(item)"
          >
            <span>{{ item }}</span>
            <el-icon><ArrowRight /></el-icon>
          </button>
        </div>

        <div class="input-panel">
          <el-input
            v-model="inputValue"
            type="textarea"
            resize="none"
            :rows="5"
            placeholder="请输入想咨询的问题"
            @keydown.enter.exact.prevent="sendQuestion"
          />
          <div class="input-footer">
            <span class="input-tip">Enter 发送，Shift + Enter 换行</span>
            <div class="input-actions">
              <el-button v-if="sending" @click="stopGeneration">停止生成</el-button>
              <el-button type="primary" :icon="Promotion" :loading="sending" @click="sendQuestion">
                发送
              </el-button>
            </div>
          </div>
        </div>
      </section>
    </div>

    <LlmSettings ref="settingsRef" />
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({
  name: 'Question'
})

import { onBeforeUnmount, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowRight, Promotion, Setting } from '@element-plus/icons-vue'
import ChatAnswerPanel, { type ChatMessageItem } from '../component/ChatAnswerPanel.vue'
import SessionSidebar, { type SessionConversationItem } from '../component/SessionSidebar.vue'
import LlmSettings from '../component/LlmSettings.vue'
import {
  getCurrentUserId,
  deleteLlmConversation,
  getLlmConversations,
  getLlmMessages,
  sendLlmMessageStream,
  type LlmConversation
} from '@/api/llm'

const quickQuestions = [
  'LNG 卸船操作的关键风险点有哪些？',
  'BOG 异常升高时应该优先检查什么？',
  '如何快速判断当前调度方案是否合理？'
]

const bizType = 'question'
const conversations = ref<SessionConversationItem[]>([])
const activeConversationId = ref('new')
const inputValue = ref('')
const userId = ref(0)
const sending = ref(false)
const streamController = ref<AbortController | null>(null)
const settingsRef = ref<InstanceType<typeof LlmSettings> | null>(null)

const initialMessages = (): ChatMessageItem[] => [
  {
    id: 'msg-1',
    role: 'assistant',
    content: '您好，我可以帮助您处理 LNG 调度、工艺运行和业务分析相关问题。'
  }
]

const messages = ref<ChatMessageItem[]>(initialMessages())

const formatConversationTime = (value?: string | null) => {
  if (!value) return '刚刚'

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '刚刚'

  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  const hour = `${date.getHours()}`.padStart(2, '0')
  const minute = `${date.getMinutes()}`.padStart(2, '0')
  return `${month}-${day} ${hour}:${minute}`
}

const mapConversation = (item: LlmConversation): SessionConversationItem => ({
  id: item.conversationCode,
  title: item.title,
  time: formatConversationTime(item.lastMessageAt || item.createdAt)
})

const refreshConversations = async () => {
  if (userId.value <= 0) return

  const response = await getLlmConversations(bizType, userId.value)
  if (response.code !== 200) {
    throw new Error(response.message || '加载历史会话失败')
  }

  conversations.value = response.data.map(mapConversation)
}

const sendQuestion = async () => {
  const question = inputValue.value.trim()
  if (!question) {
    ElMessage.warning('请输入问题内容')
    return
  }

  if (userId.value <= 0) {
    ElMessage.warning('未获取到当前用户信息，暂时无法发起问答')
    return
  }

  if (sending.value) return

  sending.value = true
  const controller = new AbortController()
  streamController.value = controller

  try {
    const isNewConversation = activeConversationId.value === 'new'
    const tempUserId = `user-${Date.now()}`
    const tempAssistantId = `assistant-${Date.now()}`
    const questionSnapshot = question
    let streamUsedFallback = false
    let assistantReplyContent = ''

    if (isNewConversation) {
      messages.value = []
    }

    messages.value.push(
      {
        id: tempUserId,
        role: 'user',
        content: questionSnapshot
      },
      {
        id: tempAssistantId,
        role: 'assistant',
        content: ''
      }
    )

    inputValue.value = ''

    await sendLlmMessageStream(
      bizType,
      {
        userId: userId.value,
        conversationCode: activeConversationId.value === 'new' ? undefined : activeConversationId.value,
        content: question
      },
      {
        onStart: event => {
          if (event.conversationCode) {
            activeConversationId.value = event.conversationCode
          }
        },
        onDelta: event => {
          const target = messages.value.find(item => item.id === tempAssistantId)
          if (target) {
            const delta = event.content || ''
            target.content += delta
            assistantReplyContent += delta
          }
        },
        onDone: event => {
          if (event.modelName === 'fallback') {
            streamUsedFallback = true
          }
          const assistant = messages.value.find(item => item.id === tempAssistantId)
          if (assistant && event.messageId) {
            assistant.id = String(event.messageId)
          }
          const user = messages.value.find(item => item.id === tempUserId)
          if (user && user.id === tempUserId) {
            user.id = `${tempUserId}-done`
          }
        },
        onError: event => {
          throw new Error(event.message || '问答生成失败')
        }
      },
      controller.signal
    )

    if (streamUsedFallback || !assistantReplyContent.trim()) {
      ElMessage.error('模型服务连接失败，当前返回的是降级结果，请检查 Ollama 服务连接。')
    }

    await refreshConversations()
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      ElMessage.info('已停止生成')
      return
    }

    ElMessage.error((error as Error).message || '发送失败')
  } finally {
    streamController.value = null
    sending.value = false
  }
}

const stopGeneration = () => {
  streamController.value?.abort()
}

const useQuickQuestion = async (question: string) => {
  inputValue.value = question
  await sendQuestion()
}

const selectConversation = async (id: string) => {
  activeConversationId.value = id
  try {
    const response = await getLlmMessages(bizType, id)
    if (response.code !== 200) {
      throw new Error(response.message || '加载会话内容失败')
    }

    messages.value = response.data.map(item => ({
      id: String(item.messageId),
      role: item.role === 'assistant' ? 'assistant' : 'user',
      content: item.content
    }))

    if (messages.value.length === 0) {
      messages.value = initialMessages()
    }
  } catch (error) {
    ElMessage.error((error as Error).message || '加载会话内容失败')
  }
}

const startNewConversation = () => {
  streamController.value?.abort()
  activeConversationId.value = 'new'
  inputValue.value = ''
  messages.value = initialMessages()
}

const deleteConversation = async (id: string) => {
  if (userId.value <= 0) return

  try {
    await ElMessageBox.confirm('确认删除这条历史对话吗？', '提示', {
      type: 'warning'
    })

    const response = await deleteLlmConversation(bizType, id, userId.value)
    if (response.code !== 200) {
      throw new Error(response.message || '删除历史对话失败')
    }

    if (activeConversationId.value === id) {
      startNewConversation()
    }

    await refreshConversations()
    ElMessage.success('历史对话已删除')
  } catch (error) {
    if (error === 'cancel') return
    ElMessage.error((error as Error).message || '删除历史对话失败')
  }
}

const restoreHistoryConversation = () => {
  if (conversations.value.length === 0) {
    ElMessage.info('暂无历史会话')
    return
  }

  void selectConversation(conversations.value[0]?.id || 'new')
}

onMounted(async () => {
  userId.value = getCurrentUserId()
  if (userId.value <= 0) return

  try {
    await refreshConversations()
  } catch (error) {
    ElMessage.error((error as Error).message || '加载历史会话失败')
  }
})

onBeforeUnmount(() => {
  streamController.value?.abort()
})
</script>

<style scoped lang="scss">
.question-page {
  height: 100% !important;
  overflow: scroll;
}

.question-layout {
  display: flex;
  gap: 10px;
  height: 100%;
  min-height: 0;
  padding-left: 10px;
}

.question-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
  min-height: 0;
  padding: 10px 0;
}

.intro-panel {
  height: 45px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.settings-btn {
  flex-shrink: 0;
}

.intro-text {
  margin: 0;
  color: #ffffff;
  font-size: 15px;
  line-height: 1.4;
}

.input-panel {
  min-height: 220px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  border: 1px solid rgba(48, 188, 255, 0.75);
  background: rgba(4, 23, 44, 0.42);
}

.quick-question-row {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.quick-question {
  height: 46px;
  padding: 0 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  border: 1px solid rgba(48, 188, 255, 0.75);
  background: rgba(4, 23, 44, 0.42);
  color: #ffffff;
  cursor: pointer;
  transition: background 0.2s ease, border-color 0.2s ease;
}

.quick-question:hover {
  border-color: #5fd6ff;
  background: rgba(9, 52, 89, 0.6);
}

.quick-question span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: left;
}

.input-panel :deep(.el-textarea__inner) {
  min-height: 140px !important;
  color: #ffffff;
  border: 1px solid rgba(57, 181, 255, 0.24);
  background: rgba(7, 29, 53, 0.65);
  box-shadow: none;
}

.input-panel :deep(.el-textarea__inner::placeholder) {
  color: rgba(220, 238, 255, 0.45);
}

.input-footer {
  margin-top: 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.input-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
}

.input-tip {
  color: rgba(220, 238, 255, 0.6);
  font-size: 12px;
}

@media (max-width: 1280px) {
  .quick-question-row {
    grid-template-columns: 1fr;
  }
}
</style>
