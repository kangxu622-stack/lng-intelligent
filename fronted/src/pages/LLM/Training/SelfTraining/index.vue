<template>
  <pagePanelNew class="training-page">
    <div class="page-header">
      <h2>自学训练</h2>
      <p class="subtitle">选择训练方式，系统将从已审核题库中抽取题目进行训练。</p>
    </div>

    <cm-panel v-if="!session.active && !session.finished">
      <el-row :gutter="14">
        <el-col :span="8">
          <label class="f-label">训练方式</label>
          <el-select v-model="trainMode" style="width:100%">
            <el-option v-for="m in trainModes" :key="m" :label="m" :value="m" />
          </el-select>
        </el-col>
        <el-col :span="8">
          <label class="f-label">岗位</label>
          <el-select v-model="filter.position" style="width:100%">
            <el-option v-for="p in settings?.positions" :key="p" :label="p" :value="p" />
          </el-select>
        </el-col>
        <el-col :span="8">
          <label class="f-label">难度</label>
          <el-select v-model="filter.difficulty" style="width:100%">
            <el-option label="全部" value="" />
            <el-option v-for="d in settings?.difficultyLevels" :key="d" :label="d" :value="d" />
          </el-select>
        </el-col>
      </el-row>
      <el-row :gutter="14" style="margin-top:12px">
        <el-col :span="8">
          <label class="f-label">题型</label>
          <el-select v-model="filter.questionType" style="width:100%">
            <el-option label="全部" value="" />
            <el-option v-for="t in settings?.questionTypes" :key="t" :label="t" :value="t" />
          </el-select>
        </el-col>
        <el-col :span="8">
          <label class="f-label">题目数量</label>
          <el-input-number v-model="filter.limit" :min="1" :max="50" />
        </el-col>
      </el-row>
      <el-button style="margin-top:16px" type="primary" size="large" @click="startTraining">开始训练</el-button>
    </cm-panel>

    <!-- Training Active -->
    <div v-if="session.active && session.questions.length > 0 && currentIdx < session.questions.length">
      <div class="progress-bar">
        <el-progress :percentage="Math.round((currentIdx + 1) / session.questions.length * 100)" :text-inside="true" :stroke-width="20">
          {{ currentIdx + 1 }} / {{ session.questions.length }}
        </el-progress>
      </div>

      <cm-panel class="question-panel">
        <h3>[{{ currentQ.questionType }}] {{ currentQ.stem }}</h3>

        <div v-if="currentQ.optionsJson">
          <el-radio-group v-if="!answered" v-model="userAnswer" class="option-group">
            <el-radio v-for="(opt, key) in parseOptions(currentQ.optionsJson)" :key="key" :value="key" class="option-item">
              {{ key }}: {{ opt }}
            </el-radio>
          </el-radio-group>
        </div>
        <div v-else>
          <el-input v-if="!answered" v-model="userAnswer" placeholder="请输入答案" type="textarea" :rows="3" />
        </div>

        <div v-if="!answered" style="margin-top:14px">
          <el-button type="primary" @click="submitAnswer">提交答案</el-button>
          <el-button @click="skipQuestion">跳过</el-button>
        </div>

        <div v-else class="answer-result">
          <p v-if="lastResult?.isCorrect" class="correct">正确！你的答案: {{ userAnswer }}</p>
          <p v-else class="wrong">错误！你的答案: {{ userAnswer }}，正确答案: {{ lastResult?.correctAnswer }}</p>
          <p v-if="lastResult?.explanation"><strong>解析:</strong> {{ lastResult.explanation }}</p>
          <p><strong>知识点:</strong> {{ lastResult?.knowledgeName || currentQ.knowledgeName }}</p>
          <el-button type="primary" @click="nextQuestion">下一题</el-button>
        </div>
      </cm-panel>
    </div>

    <!-- Finished -->
    <cm-panel v-if="session.finished">
      <h3 style="color:#4caf50">训练完成！</h3>
      <p>本次正确率: {{ accuracy }}%</p>
      <el-button type="primary" @click="resetSession">换一批新题</el-button>
    </cm-panel>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({ name: 'TrainingSelfTraining' })
import { onMounted, reactive, ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { getTrainingConfig, getTrainingQuestions, submitAnswer as submitApi, type QuestionDto, type AnswerSubmitResult, type SystemConfig } from '@/api/training'

const settings = ref<SystemConfig | null>(null)
const trainModes = ['按岗位训练', '随机训练', '错题强化']
const trainMode = ref('按岗位训练')
const filter = reactive({ position: '操作员', difficulty: '', questionType: '', limit: 10 })
const session = reactive({ active: false, finished: false, questions: [] as QuestionDto[] })
const currentIdx = ref(0)
const userAnswer = ref('')
const answered = ref(false)
const lastResult = ref<AnswerSubmitResult | null>(null)
const correctCount = ref(0)

const currentQ = computed(() => session.questions[currentIdx.value] || ({} as QuestionDto))
const accuracy = computed(() => {
  if (session.questions.length === 0) return 0
  return Math.round(correctCount.value / session.questions.length * 100)
})

const parseOptions = (json: string): Record<string, string> => {
  try { return JSON.parse(json) } catch { return {} }
}

const startTraining = async () => {
  try {
    const res = await getTrainingQuestions({
      userId: 'student1',
      position: filter.position || undefined,
      questionType: filter.questionType || undefined,
      difficulty: filter.difficulty || undefined,
      limit: filter.limit
    })
    if (res.data.code === 200 && (res.data.data || []).length > 0) {
      session.questions = res.data.data || []
      session.active = true
      session.finished = false
      currentIdx.value = 0
      correctCount.value = 0
      answered.value = false
    } else {
      ElMessage.warning('没有找到符合条件的题目，请管理员先审核题目入库。')
    }
  } catch (e: any) {
    ElMessage.error(e.message || '加载题目失败')
  }
}

const submitAnswer = async () => {
  if (!userAnswer.value.trim()) { ElMessage.warning('请输入答案'); return }
  try {
    const res = await submitApi({
      userId: 'student1',
      questionId: currentQ.value.questionId,
      userAnswer: userAnswer.value.trim()
    })
    if (res.data.code === 200) {
      lastResult.value = res.data.data || null
      if (lastResult.value?.isCorrect) correctCount.value++
      answered.value = true
    }
  } catch (e: any) { ElMessage.error(e.message || '提交失败') }
}

const skipQuestion = () => { nextQuestion() }

const nextQuestion = () => {
  if (currentIdx.value + 1 < session.questions.length) {
    currentIdx.value++
    userAnswer.value = ''
    answered.value = false
    lastResult.value = null
  } else {
    session.active = false
    session.finished = true
  }
}

const resetSession = () => {
  session.active = false
  session.finished = false
  session.questions = []
  currentIdx.value = 0
  answered.value = false
}

onMounted(async () => {
  const res = await getTrainingConfig()
  if (res.data.code === 200) settings.value = res.data.data || null
})
</script>

<style scoped lang="scss">
.training-page { padding: 20px; height: 100%; overflow-y: auto; }
.page-header { margin-bottom: 20px; }
.page-header h2 { margin: 0; color: #fff; font-size: 22px; }
.subtitle { color: rgba(220,238,255,0.7); margin: 6px 0 0; font-size: 14px; }
.f-label { color: #7bd7ff; font-size: 13px; display: block; margin-bottom: 4px; }
.progress-bar { margin-bottom: 14px; }
.question-panel { min-height: 200px; }
.question-panel h3 { color: #fff; margin-bottom: 14px; }
.option-group { display: flex; flex-direction: column; gap: 8px; }
.option-item { color: #e0e6ff; }
.answer-result { margin-top: 14px; padding: 12px; border: 1px solid rgba(48,188,255,0.3); }
.correct { color: #4caf50; font-size: 16px; }
.wrong { color: #f44336; font-size: 16px; }
</style>
