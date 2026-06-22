import http from './http'
import type { ApiResponse } from './constants'

// ── Types ──

export interface ManualDto {
  manualId: string
  manualName: string
  fileType: string | null
  uploadUser: string | null
  uploadTime: string | null
  status: string
  chunkCount: number
}

export interface ManualParseResultDto {
  manualId: string
  text: string
  length: number
}

export interface ManualChunkDto {
  chunkId: string
  manualId: string
  chapterTitle: string | null
  sectionNo: string | null
  content: string | null
  pageNo: string | null
  systemName: string | null
  createdTime: string | null
}

export interface KnowledgePointDto {
  knowledgeId: string
  name: string
  systemName: string | null
  chapterTitle: string | null
  position: string | null
  difficulty: string | null
  riskLevel: string | null
  sourceChunkId: string | null
  manualBasis: string | null
  status: string
  createdTime: string | null
  questionCount: number
  wrongCount: number
}

export interface KnowledgeExtractResult {
  savedIds: string[]
  count: number
}

export interface QuestionDto {
  questionId: string
  questionType: string | null
  stem: string
  optionsJson: string | null
  answer: string | null
  explanation: string | null
  knowledgeId: string | null
  knowledgeName: string | null
  position: string | null
  difficulty: string | null
  source: string | null
  manualBasis: string | null
  reviewStatus: string
  createdTime: string | null
  updatedTime: string | null
}

export interface QuestionGenerateResult {
  savedIds: string[]
  count: number
}

export interface AnswerSubmitResult {
  recordId: string
  isCorrect: boolean
  correctAnswer: string | null
  explanation: string | null
  knowledgeName: string | null
}

export interface AnswerRecordDto {
  recordId: string
  userId: string
  questionId: string | null
  userAnswer: string | null
  correctAnswer: string | null
  isCorrect: number
  score: number | null
  answerTime: string | null
  duration: number | null
  stem: string | null
  questionType: string | null
  knowledgeName: string | null
}

export interface WrongQuestionDto {
  wrongId: string
  userId: string
  questionId: string | null
  knowledgeId: string | null
  wrongAnswer: string | null
  correctAnswer: string | null
  createdTime: string | null
  reviewCount: number
  stem: string | null
  questionType: string | null
  knowledgeName: string | null
  qAnswer: string | null
  explanation: string | null
  manualBasis: string | null
}

export interface UserStatsDto {
  total: number
  correct: number
  wrong: number
  accuracy: number
}

export interface KnowledgeAccuracyDto {
  knowledgeId: string | null
  knowledgeName: string
  total: number
  correct: number
  accuracy: number
  level: string
}

export interface UserAnalyticsDto {
  stats: UserStatsDto
  knowledgeAccuracy: KnowledgeAccuracyDto[]
  weakKnowledge: KnowledgeAccuracyDto[]
  recentRecords: AnswerRecordDto[]
}

export interface AdminStatsDto {
  totalUsers: number
  totalAnswers: number
  avgAccuracy: number
  perUser: { name: string; position: string; total: number; correct: number }[]
  knowledgeStats: { knowledgeName: string; total: number; correct: number }[]
}

export interface SystemConfig {
  positions: string[]
  systems: string[]
  questionTypes: string[]
  difficultyLevels: string[]
}

// ── API Functions ──

// System
export function getTrainingConfig() {
  return http.get<ApiResponse<SystemConfig>>('/api/training/system/config')
}

// Manuals
export function uploadManual(file: File, userId: string = 'admin') {
  const form = new FormData()
  form.append('file', file)
  form.append('userId', userId)
  return http.post<ApiResponse<ManualDto>>('/api/training/manuals/upload', form, {
    timeout: 120000
  })
}

export function getManuals() {
  return http.get<ApiResponse<ManualDto[]>>('/api/training/manuals')
}

export function parseManual(manualId: string, userId: string = 'admin') {
  return http.post<ApiResponse<ManualParseResultDto>>(`/api/training/manuals/${manualId}/parse?userId=${userId}`)
}

export function chunkManual(manualId: string, userId: string = 'admin') {
  return http.post<ApiResponse<ManualChunkDto[]>>(`/api/training/manuals/${manualId}/chunk?userId=${userId}`)
}

export function deleteManual(manualId: string, userId: string = 'admin') {
  return http.delete<ApiResponse>(`/api/training/manuals/${manualId}?userId=${userId}`)
}

export function searchChunks(keyword: string, manualId?: string) {
  let url = `/api/training/manuals/search?keyword=${encodeURIComponent(keyword)}`
  if (manualId) url += `&manualId=${manualId}`
  return http.get<ApiResponse<ManualChunkDto[]>>(url)
}

// Knowledge
export function getKnowledgeChunks(manualId: string) {
  return http.get<ApiResponse<ManualChunkDto[]>>(`/api/training/knowledge/chunks/${manualId}`)
}

export function extractKnowledge(chunkIds: string[], userId: string = 'admin') {
  return http.post<ApiResponse<KnowledgeExtractResult>>('/api/training/knowledge/extract', { chunkIds, userId }, { timeout: 120000 })
}

export function getPendingKnowledge() {
  return http.get<ApiResponse<KnowledgePointDto[]>>('/api/training/knowledge/pending')
}

export function getConfirmedKnowledge() {
  return http.get<ApiResponse<KnowledgePointDto[]>>('/api/training/knowledge/confirmed')
}

export function confirmKnowledge(knowledgeId: string, userId: string = 'admin') {
  return http.post<ApiResponse>(`/api/training/knowledge/${knowledgeId}/confirm?userId=${userId}`)
}

export function updateKnowledge(knowledgeId: string, updates: Record<string, string | null>, userId: string = 'admin') {
  return http.put<ApiResponse>(`/api/training/knowledge/${knowledgeId}?userId=${userId}`, updates)
}

export function deleteKnowledge(knowledgeId: string, userId: string = 'admin') {
  return http.delete<ApiResponse>(`/api/training/knowledge/${knowledgeId}?userId=${userId}`)
}

// Questions
export function generateQuestions(input: {
  position: string; questionType: string; difficulty: string; count: number
  chunkIds?: string[]; customText?: string; knowledgeId?: string; knowledgeName?: string; userId?: string
}) {
  return http.post<ApiResponse<QuestionGenerateResult>>('/api/training/questions/generate', input, { timeout: 120000 })
}

export function getPendingQuestions() {
  return http.get<ApiResponse<QuestionDto[]>>('/api/training/questions/pending')
}

export function getApprovedQuestions(params: {
  knowledgeId?: string; position?: string; questionType?: string; difficulty?: string; limit?: number
} = {}) {
  const query = new URLSearchParams()
  if (params.knowledgeId) query.append('knowledgeId', params.knowledgeId)
  if (params.position) query.append('position', params.position)
  if (params.questionType) query.append('questionType', params.questionType)
  if (params.difficulty) query.append('difficulty', params.difficulty)
  if (params.limit) query.append('limit', String(params.limit))
  return http.get<ApiResponse<QuestionDto[]>>(`/api/training/questions/approved?${query.toString()}`)
}

export function getQuestion(questionId: string) {
  return http.get<ApiResponse<QuestionDto>>(`/api/training/questions/${questionId}`)
}

export function approveQuestion(questionId: string, userId: string = 'admin') {
  return http.post<ApiResponse>(`/api/training/questions/${questionId}/approve?userId=${userId}`)
}

export function rejectQuestion(questionId: string, userId: string = 'admin') {
  return http.post<ApiResponse>(`/api/training/questions/${questionId}/reject?userId=${userId}`)
}

export function updateQuestion(questionId: string, updates: Record<string, string | null>, userId: string = 'admin') {
  return http.put<ApiResponse>(`/api/training/questions/${questionId}?userId=${userId}`, updates)
}

export function deleteQuestion(questionId: string, userId: string = 'admin') {
  return http.delete<ApiResponse>(`/api/training/questions/${questionId}?userId=${userId}`)
}

// Training Sessions
export function getTrainingQuestions(input: {
  userId: string; knowledgeId?: string; position?: string; questionType?: string
  difficulty?: string; limit?: number
}) {
  return http.post<ApiResponse<QuestionDto[]>>('/api/training/sessions/questions', input)
}

export function submitAnswer(input: { userId: string; questionId: string; userAnswer: string; duration?: number }) {
  return http.post<ApiResponse<AnswerSubmitResult>>('/api/training/sessions/answer', input)
}

export function getAnswerRecords(userId: string, limit: number = 100) {
  return http.get<ApiResponse<AnswerRecordDto[]>>(`/api/training/records/${userId}?limit=${limit}`)
}

export function getWrongQuestions(userId: string, knowledgeId?: string) {
  let url = `/api/training/wrong-questions/${userId}`
  if (knowledgeId) url += `?knowledgeId=${knowledgeId}`
  return http.get<ApiResponse<WrongQuestionDto[]>>(url)
}

// Analytics
export function getUserAnalytics(userId: string) {
  return http.get<ApiResponse<UserAnalyticsDto>>(`/api/training/analytics/stats/${userId}`)
}

export function getAdminStats() {
  return http.get<ApiResponse<AdminStatsDto>>('/api/training/analytics/admin')
}

export function analyzeWrongQuestions(userId: string) {
  return http.post<ApiResponse<{ analysis: string }>>(`/api/training/analytics/wrong-analysis/${userId}`, null, { timeout: 120000 })
}
