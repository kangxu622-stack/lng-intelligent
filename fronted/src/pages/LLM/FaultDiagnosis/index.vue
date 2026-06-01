<template>
  <pagePanelNew class="fault-page">
    <div class="fault-layout">
      <SessionSidebar
        :conversations="conversations"
        :active-conversation-id="activeConversationId"
        @new-conversation="startNewConversation"
        @restore-history="restoreHistoryConversation"
        @select-conversation="selectConversation"
        @delete-conversation="deleteConversation"
      />

      <section class="fault-main" style="overflow: scroll;">
        <cm-panel class="intro-panel">
          <p class="intro-text">上传设备图片并补充现象描述，系统会给出故障诊断建议。</p>
        </cm-panel>

        <div class="upload-strip">
          <div class="upload-strip-left">
            <el-icon class="upload-strip-icon"><UploadFilled /></el-icon>
            <span class="upload-strip-title">图片上传</span>
            <span class="upload-strip-tip">支持 PNG / JPG / JPEG</span>
            <span v-if="uploadedFileName" class="upload-file-tag">{{ uploadedFileName }}</span>
          </div>
          <div class="upload-strip-right">
            <el-upload
              :auto-upload="false"
              :show-file-list="false"
              accept=".png,.jpg,.jpeg"
              action="#"
              @change="handleFileChange"
            >
              <el-button type="primary" :loading="uploading">选择图片</el-button>
            </el-upload>
          </div>
        </div>

        <div v-if="uploadedImageUrl" class="preview-strip">
          <el-image
            class="preview-thumb"
            :src="uploadedImageUrl"
            :preview-src-list="[uploadedImageUrl]"
            :initial-index="0"
            fit="cover"
            preview-teleported
          />
          <div class="preview-meta">
            <div class="preview-name">{{ uploadedFileName }}</div>
            <div class="preview-tip">点击缩略图可放大查看，发送问题时会自动带上这张图片。</div>
          </div>
        </div>

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
            placeholder="请输入故障现象、报警信息或希望诊断的问题"
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
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({
  name: 'FaultDiagnosis'
})

import { onBeforeUnmount, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox, type UploadFile, type UploadFiles } from 'element-plus'
import { ArrowRight, Promotion, UploadFilled } from '@element-plus/icons-vue'
import ChatAnswerPanel, { type ChatMessageItem } from '../component/ChatAnswerPanel.vue'
import SessionSidebar, { type SessionConversationItem } from '../component/SessionSidebar.vue'
import {
  getCurrentUserId,
  deleteLlmConversation,
  getLlmConversations,
  getLlmMessages,
  sendLlmMessageStream,
  uploadFaultImage,
  type LlmConversation
} from '@/api/llm'

const quickQuestions = ['这张图里最可能的异常点是什么？', '这个报警现象通常有哪些原因？', '请给出排查顺序和处理建议']

const bizType = 'fault-diagnosis'
const conversations = ref<SessionConversationItem[]>([])
const activeConversationId = ref('new')
const inputValue = ref('')
const uploadedFileName = ref('')
const uploadedImageUrl = ref('')
const uploadedImageIsObjectUrl = ref(false)
const attachmentIds = ref<number[]>([])
const userId = ref(0)
const sending = ref(false)
const uploading = ref(false)
const streamController = ref<AbortController | null>(null)

const initialMessages = (): ChatMessageItem[] => [
  {
    id: 'msg-1',
    role: 'assistant',
    content: '您好，我可以结合故障描述和上传图片协助您做初步诊断分析。'
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
    ElMessage.warning('请输入诊断问题')
    return
  }

  if (userId.value <= 0) {
    ElMessage.warning('未获取到当前用户信息，暂时无法进行故障诊断')
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
        content: question,
        attachmentIds: attachmentIds.value
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
          throw new Error(event.message || '故障诊断生成失败')
        }
      },
      controller.signal
    )

    if (streamUsedFallback || !assistantReplyContent.trim()) {
      ElMessage.error('模型服务连接失败，请检查 Ollama 服务连接。')
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

const clearUploadedImageUrl = () => {
  if (uploadedImageUrl.value && uploadedImageIsObjectUrl.value) {
    URL.revokeObjectURL(uploadedImageUrl.value)
  }
  uploadedImageUrl.value = ''
  uploadedImageIsObjectUrl.value = false
}

const handleFileChange = async (file: UploadFile, _files: UploadFiles) => {
  if (!file.raw) {
    ElMessage.warning('未读取到图片文件')
    return
  }

  if (userId.value <= 0) {
    ElMessage.warning('未获取到当前用户信息，暂时无法上传图片')
    return
  }

  if (uploading.value) return

  uploading.value = true
  clearUploadedImageUrl()
  uploadedFileName.value = file.name
  uploadedImageUrl.value = URL.createObjectURL(file.raw)
  uploadedImageIsObjectUrl.value = true

  try {
    const response = await uploadFaultImage(
      file.raw,
      userId.value,
      activeConversationId.value === 'new' ? undefined : activeConversationId.value
    )

    if (response.code !== 200) {
      throw new Error(response.message || '图片上传失败')
    }

    const result = response.data
    attachmentIds.value = [result.attachmentId]
    activeConversationId.value = result.conversationCode

    // Keep the local blob preview after upload succeeds. In nginx deployments,
    // the server-side /uploads path may not be exposed, which would break the
    // immediate preview if we switch to result.previewUrl here.

    await refreshConversations()
    ElMessage.success(`图片上传成功：${file.name}`)
  } catch (error) {
    ElMessage.error((error as Error).message || '图片上传失败')
  } finally {
    uploading.value = false
  }
}

const selectConversation = async (id: string) => {
  activeConversationId.value = id
  uploadedFileName.value = ''
  attachmentIds.value = []
  clearUploadedImageUrl()

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
  uploadedFileName.value = ''
  attachmentIds.value = []
  clearUploadedImageUrl()
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
  clearUploadedImageUrl()
})
</script>

<style scoped lang="scss">
.fault-page {
  height: 100% !important;
}

.fault-layout {
  display: flex;
  gap: 10px;
  height: 100%;
  min-height: 0;
  padding-left: 10px;
}

.fault-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
  min-height: 0;
  padding: 10px 0;
}

.intro-panel {
  min-height: 45px;
  display: flex;
  align-items: center;
}

.intro-text {
  margin: 0;
  color: #ffffff;
  font-size: 15px;
  line-height: 1.4;
}

.upload-strip,
.input-panel {
  border: 1px solid rgba(48, 188, 255, 0.75);
  background: rgba(4, 23, 44, 0.42);
}

.upload-strip {
  min-height: 58px;
  padding: 10px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.upload-strip-left {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.upload-strip-right {
  flex-shrink: 0;
}

.upload-strip-icon {
  color: #00e7ff;
  font-size: 20px;
}

.upload-strip-title {
  color: #00e7ff;
  font-size: 16px;
  font-weight: 600;
  white-space: nowrap;
}

.upload-strip-tip {
  color: rgba(220, 238, 255, 0.7);
  font-size: 13px;
  white-space: nowrap;
}

.upload-file-tag {
  max-width: 320px;
  padding: 4px 10px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  border: 1px solid rgba(57, 181, 255, 0.3);
  background: rgba(8, 32, 57, 0.5);
  color: #7bd7ff;
  font-size: 13px;
}

.preview-strip {
  padding: 12px 14px;
  display: flex;
  align-items: center;
  gap: 14px;
  border: 1px solid rgba(48, 188, 255, 0.4);
  background: rgba(6, 27, 50, 0.38);
}

.preview-thumb {
  width: 88px;
  height: 88px;
  flex-shrink: 0;
  border: 1px solid rgba(57, 181, 255, 0.45);
  cursor: pointer;
}

.preview-meta {
  min-width: 0;
}

.preview-name {
  color: #ffffff;
  font-size: 14px;
  line-height: 1.5;
  word-break: break-all;
}

.preview-tip {
  margin-top: 6px;
  color: rgba(220, 238, 255, 0.62);
  font-size: 12px;
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

.input-panel {
  min-height: 220px;
  padding: 16px;
  display: flex;
  flex-direction: column;
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

  .upload-strip {
    align-items: flex-start;
    flex-direction: column;
  }

  .upload-strip-left {
    flex-wrap: wrap;
  }

  .preview-strip {
    align-items: flex-start;
  }
}
</style>
