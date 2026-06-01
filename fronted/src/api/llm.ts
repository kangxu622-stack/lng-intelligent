import http from './http'
import { TOKEN_NAME, USER_INFO_KEY } from '@/config/global'

interface ApiEnvelope<T> {
  code: number
  message: string
  data: T
}

export interface LlmConversation {
  conversationId: number
  conversationCode: string
  bizType: string
  title: string
  status: string
  lastMessageAt?: string | null
  createdAt: string
}

export interface LlmMessage {
  messageId: number
  role: 'system' | 'user' | 'assistant' | 'tool'
  contentType: 'text' | 'image' | 'mixed' | 'json'
  content: string
  modelName?: string | null
  promptTokens?: number | null
  completionTokens?: number | null
  totalTokens?: number | null
  responseMs?: number | null
  sequenceNo: number
  createdAt: string
}

export interface SendLlmMessageRequest {
  userId: number
  conversationCode?: string
  content: string
  attachmentIds?: number[]
}

export interface SendLlmMessageResult {
  conversationCode: string
  bizType: string
  userMessage: LlmMessage
  assistantMessage: LlmMessage
}

export interface UploadFaultImageResult {
  attachmentId: number
  conversationCode: string
  fileName: string
  storagePath: string
  previewUrl?: string | null
}

export interface LlmStreamStartEvent {
  type: 'start'
  conversationCode?: string
  messageId?: number
}

export interface LlmStreamDeltaEvent {
  type: 'delta'
  conversationCode?: string
  content?: string
  modelName?: string
}

export interface LlmStreamDoneEvent {
  type: 'done'
  conversationCode?: string
  messageId?: number
  modelName?: string
  promptTokens?: number
  completionTokens?: number
  totalTokens?: number
}

export interface LlmStreamErrorEvent {
  type: 'error'
  message?: string
}

export const getCurrentUserId = () => {
  const token = localStorage.getItem(TOKEN_NAME)
  if (token && !Number.isNaN(Number(token))) {
    return Number(token)
  }

  const rawUserInfo = localStorage.getItem(USER_INFO_KEY)
  if (rawUserInfo) {
    try {
      const userInfo = JSON.parse(rawUserInfo) as { userId?: number }
      if (userInfo.userId) {
        return userInfo.userId
      }
    } catch {
      // ignore malformed local data
    }
  }

  return 0
}

export const getLlmConversations = async (bizType: string, userId: number) => {
  const response = await http.get<ApiEnvelope<LlmConversation[]>>(`/api/llm/${bizType}/conversations`, {
    params: { userId }
  })
  return response.data
}

export const getLlmMessages = async (bizType: string, conversationCode: string) => {
  const response = await http.get<ApiEnvelope<LlmMessage[]>>(
    `/api/llm/${bizType}/conversations/${conversationCode}/messages`
  )
  return response.data
}

export const deleteLlmConversation = async (bizType: string, conversationCode: string, userId: number) => {
  const response = await http.delete<ApiEnvelope<null>>(`/api/llm/${bizType}/conversations/${conversationCode}`, {
    params: { userId }
  })
  return response.data
}

export const sendLlmMessage = async (bizType: string, payload: SendLlmMessageRequest) => {
  const response = await http.post<ApiEnvelope<SendLlmMessageResult>>(`/api/llm/${bizType}/send`, payload, {
    timeout: 60000
  })
  return response.data
}

export const sendLlmMessageStream = async (
  bizType: string,
  payload: SendLlmMessageRequest,
  handlers: {
    onStart?: (event: LlmStreamStartEvent) => void
    onDelta?: (event: LlmStreamDeltaEvent) => void
    onDone?: (event: LlmStreamDoneEvent) => void
    onError?: (event: LlmStreamErrorEvent) => void
  },
  signal?: AbortSignal
) => {
  const response = await fetch(`/api/llm/${bizType}/stream`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload),
    signal
  })

  if (!response.ok || !response.body) {
    throw new Error(`流式请求失败: ${response.status}`)
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder('utf-8')
  let buffer = ''

  const processEventBlock = (block: string) => {
    const lines = block.split('\n')
    let eventName = 'message'
    const dataLines: string[] = []

    for (const rawLine of lines) {
      const line = rawLine.trimEnd()
      if (line.startsWith('event:')) {
        eventName = line.slice(6).trim()
      } else if (line.startsWith('data:')) {
        dataLines.push(line.slice(5).trim())
      }
    }

    if (dataLines.length === 0) return

    const payloadText = dataLines.join('\n')
    const eventPayload = JSON.parse(payloadText) as Record<string, unknown>

    if (eventName === 'start' && handlers.onStart) {
      handlers.onStart({ type: 'start', ...eventPayload } as LlmStreamStartEvent)
    } else if (eventName === 'delta' && handlers.onDelta) {
      handlers.onDelta({ type: 'delta', ...eventPayload } as LlmStreamDeltaEvent)
    } else if (eventName === 'done' && handlers.onDone) {
      handlers.onDone({ type: 'done', ...eventPayload } as LlmStreamDoneEvent)
    } else if (eventName === 'error' && handlers.onError) {
      handlers.onError({ type: 'error', ...eventPayload } as LlmStreamErrorEvent)
    }
  }

  while (true) {
    const { value, done } = await reader.read()
    if (done) break

    buffer += decoder.decode(value, { stream: true })
    const blocks = buffer.split('\n\n')
    buffer = blocks.pop() || ''

    for (const block of blocks) {
      if (block.trim()) {
        processEventBlock(block)
      }
    }
  }

  if (buffer.trim()) {
    processEventBlock(buffer)
  }
}

export interface LlmConfig {
  activeProvider: string
  activeModel: string
  hasApiKey: boolean
  openAiBaseUrl: string
  openAiModel: string
}

export interface UpdateLlmConfigRequest {
  apiKey?: string
  baseUrl?: string
  model?: string
}

export const getLlmConfig = async () => {
  const response = await http.get<ApiEnvelope<LlmConfig>>('/api/llm/config')
  return response.data
}

export const updateLlmConfig = async (payload: UpdateLlmConfigRequest) => {
  const response = await http.put<ApiEnvelope<null>>('/api/llm/config', payload)
  return response.data
}

export const uploadFaultImage = async (file: File, userId: number, conversationCode?: string) => {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('userId', String(userId))
  if (conversationCode) {
    formData.append('conversationCode', conversationCode)
  }

  const response = await http.post<ApiEnvelope<UploadFaultImageResult>>(
    '/api/llm/fault-diagnosis/upload',
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data'
      },
      timeout: 60000
    }
  )

  return response.data
}
