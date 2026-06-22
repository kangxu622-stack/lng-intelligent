<template>
  <pagePanelNew class="training-page">
    <div class="page-header">
      <h2>题目审核</h2>
      <p class="subtitle">审核大模型生成的题目，通过后进入题库供学员训练使用。</p>
    </div>

    <div v-if="questions.length === 0" class="empty-msg">暂无待审核题目。</div>

    <cm-panel v-for="q in questions" :key="q.questionId" style="margin-bottom:10px">
      <div class="question-row">
        <div class="question-info">
          <strong>[{{ q.questionType }}] {{ q.stem }}</strong>
          <div v-if="q.optionsJson" class="options-block">
            <p v-for="(txt, key) in parseOptions(q.optionsJson)" :key="key" class="option-item">
              {{ key }}: {{ txt }}
            </p>
          </div>
          <p class="answer-line">正确答案: <span class="correct-answer">{{ q.answer }}</span> | 难度: {{ q.difficulty }} | 岗位: {{ q.position }}</p>
          <p v-if="q.explanation">解析: {{ q.explanation }}</p>
          <p>知识点: {{ q.knowledgeName }} | 手册依据: {{ q.manualBasis || '-' }}</p>
        </div>
        <div class="question-actions">
          <el-button size="small" type="success" @click="doApprove(q.questionId)">通过</el-button>
          <el-button size="small" type="danger" @click="doReject(q.questionId)">拒绝</el-button>
          <el-button size="small" @click="showEdit(q)">编辑</el-button>
        </div>
      </div>
    </cm-panel>

    <el-dialog v-model="editVisible" title="编辑题目">
      <el-input v-model="editForm.stem" placeholder="题干" type="textarea" :rows="3" />
      <el-input v-model="editForm.answer" placeholder="正确答案" style="margin-top:8px" />
      <el-input v-model="editForm.explanation" placeholder="答案解析" style="margin-top:8px" type="textarea" :rows="3" />
      <el-input v-model="editForm.knowledgeName" placeholder="知识点名称" style="margin-top:8px" />
      <template #footer>
        <el-button @click="editVisible = false">取消</el-button>
        <el-button type="primary" @click="doUpdate">保存</el-button>
      </template>
    </el-dialog>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({ name: 'TrainingQuestionReview' })
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { getPendingQuestions, approveQuestion, rejectQuestion, updateQuestion, type QuestionDto } from '@/api/training'

const questions = ref<QuestionDto[]>([])
const editVisible = ref(false)
const editForm = ref<Record<string, string | null>>({})
const editingId = ref('')

const parseOptions = (json: string): Record<string, string> => {
  try { return JSON.parse(json) } catch { return {} }
}

const refresh = async () => {
  const res = await getPendingQuestions()
  if (res.data.code === 200) questions.value = res.data.data || []
}

const doApprove = async (id: string) => {
  const res = await approveQuestion(id)
  if (res.data.code === 200) { ElMessage.success('已通过'); await refresh() }
}

const doReject = async (id: string) => {
  const res = await rejectQuestion(id)
  if (res.data.code === 200) { ElMessage.success('已拒绝'); await refresh() }
}

const showEdit = (q: QuestionDto) => {
  editingId.value = q.questionId
  editForm.value = { stem: q.stem, answer: q.answer, explanation: q.explanation, knowledgeName: q.knowledgeName }
  editVisible.value = true
}

const doUpdate = async () => {
  const res = await updateQuestion(editingId.value, editForm.value)
  if (res.data.code === 200) { ElMessage.success('更新成功'); editVisible.value = false; await refresh() }
}

onMounted(() => { refresh() })
</script>

<style scoped lang="scss">
.training-page { padding: 20px; height: 100%; overflow-y: auto; }
.page-header { margin-bottom: 20px; }
.page-header h2 { margin: 0; color: #fff; font-size: 22px; }
.subtitle { color: rgba(220,238,255,0.7); margin: 6px 0 0; font-size: 14px; }
.question-row { display: flex; justify-content: space-between; align-items: flex-start; gap: 14px; }
.question-info { flex: 1; color: #e0e6ff; }
.question-info strong { color: #7bd7ff; }
.question-info p { margin: 4px 0; font-size: 13px; }
.options-block { margin: 6px 0; padding: 8px; border: 1px solid rgba(57,181,255,0.2); background: rgba(8,32,57,0.3); }
.option-item { color: #b0c8e0; margin: 2px 0; font-size: 13px; }
.answer-line { margin: 6px 0; }
.correct-answer { color: #4caf50; font-weight: bold; }
.question-actions { display: flex; gap: 6px; flex-shrink: 0; }
.empty-msg { color: rgba(220,238,255,0.5); text-align: center; padding: 40px; }
</style>
