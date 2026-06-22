<template>
  <pagePanelNew class="training-page">
    <div class="page-header">
      <h2>错题本</h2>
      <p class="subtitle">查看个人错题记录，分析错误原因，针对性复习。</p>
    </div>

    <cm-panel v-if="wrongQuestions.length === 0" class="empty-msg">
      暂无错题记录！请先进行训练答题。
    </cm-panel>

    <cm-panel v-for="w in wrongQuestions" :key="w.wrongId" style="margin-bottom:10px">
      <div class="wq-item">
        <p><strong>[{{ w.questionType }}] {{ w.stem }}</strong></p>
        <p class="wq-wrong">你的答案: {{ w.wrongAnswer }}</p>
        <p class="wq-correct">正确答案: {{ w.correctAnswer }}</p>
        <p v-if="w.explanation">解析: {{ w.explanation }}</p>
        <p>知识点: {{ w.knowledgeName }} | 手册依据: {{ w.manualBasis || '-' }} | 复习次数: {{ w.reviewCount }}</p>
      </div>
    </cm-panel>

    <div v-if="wrongQuestions.length > 0" style="margin-top:16px">
      <el-button type="primary" @click="goToTraining">基于错题知识点生成强化训练</el-button>
    </div>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({ name: 'TrainingWrongQuestions' })
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getWrongQuestions, type WrongQuestionDto } from '@/api/training'

const router = useRouter()
const wrongQuestions = ref<WrongQuestionDto[]>([])

const goToTraining = () => {
  router.push('/ai-assistant/training/self-training')
}

onMounted(async () => {
  const res = await getWrongQuestions('student1')
  if (res.data.code === 200) wrongQuestions.value = res.data.data || []
})
</script>

<style scoped lang="scss">
.training-page { padding: 20px; height: 100%; overflow-y: auto; }
.page-header { margin-bottom: 20px; }
.page-header h2 { margin: 0; color: #fff; font-size: 22px; }
.subtitle { color: rgba(220,238,255,0.7); margin: 6px 0 0; font-size: 14px; }
.wq-item p { margin: 4px 0; color: #e0e6ff; }
.wq-item strong { color: #7bd7ff; }
.wq-wrong { color: #f44336 !important; }
.wq-correct { color: #4caf50 !important; }
.empty-msg { color: rgba(220,238,255,0.5); text-align: center; padding: 40px; }
</style>
