<template>
  <pagePanelNew class="training-page">
    <div class="page-header">
      <h2>智能出题</h2>
      <p class="subtitle">基于已确认知识点，调用大模型生成训练题目。</p>
    </div>

    <cm-panel v-if="settings">
      <el-row :gutter="16">
        <el-col :span="6">
          <label class="field-label">目标岗位</label>
          <el-select v-model="params.position" style="width:100%">
            <el-option v-for="p in settings.positions" :key="p" :label="p" :value="p" />
          </el-select>
        </el-col>
        <el-col :span="6">
          <label class="field-label">题型</label>
          <el-select v-model="params.questionType" style="width:100%">
            <el-option v-for="t in settings.questionTypes" :key="t" :label="t" :value="t" />
          </el-select>
        </el-col>
        <el-col :span="6">
          <label class="field-label">难度</label>
          <el-select v-model="params.difficulty" style="width:100%">
            <el-option v-for="d in settings.difficultyLevels" :key="d" :label="d" :value="d" />
          </el-select>
        </el-col>
        <el-col :span="6">
          <label class="field-label">数量: {{ params.count }}</label>
          <el-slider v-model="params.count" :min="1" :max="10" />
        </el-col>
      </el-row>

      <div style="margin-top:14px">
        <label class="field-label">选择知识点（可多选）</label>
        <el-select v-model="selectedKpIds" multiple placeholder="选择知识点" style="width:100%">
          <el-option v-for="kp in confirmedKps" :key="kp.knowledgeId" :label="kp.name" :value="kp.knowledgeId" />
        </el-select>
      </div>
      <div style="margin-top:14px">
        <label class="field-label">或手动输入参考内容</label>
        <el-input v-model="customText" type="textarea" :rows="4" placeholder="粘贴操作手册相关段落..." />
      </div>

      <el-button style="margin-top:16px" type="primary" :loading="generating" @click="doGenerate">
        生成题目
      </el-button>
    </cm-panel>

    <div v-if="generatedQuestions.length > 0" style="margin-top:20px">
      <h3 style="color:#7bd7ff">生成结果 ({{ generatedQuestions.length }} 道)</h3>
      <cm-panel v-for="(q, i) in generatedQuestions" :key="i" style="margin-bottom:10px">
        <p><strong>题目 {{ i + 1 }}: {{ q.stem }}</strong></p>
        <p>题型: {{ q.questionType }} | 正确答案: {{ q.answer }} | 难度: {{ q.difficulty }}</p>
        <p v-if="q.explanation">解析: {{ q.explanation }}</p>
        <p>知识点: {{ q.knowledgeName }} | 手册依据: {{ q.manualBasis || '-' }}</p>
      </cm-panel>
    </div>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({ name: 'TrainingQuestionGeneration' })
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { getTrainingConfig, getConfirmedKnowledge, generateQuestions, type KnowledgePointDto, type QuestionDto, type SystemConfig } from '@/api/training'

const settings = ref<SystemConfig | null>(null)
const confirmedKps = ref<KnowledgePointDto[]>([])
const selectedKpIds = ref<string[]>([])
const customText = ref('')
const generating = ref(false)
const generatedQuestions = ref<QuestionDto[]>([])

const params = reactive({ position: '操作员', questionType: '单选题', difficulty: '中级', count: 3 })

const doGenerate = async () => {
  generating.value = true
  try {
    const res = await generateQuestions({
      position: params.position, questionType: params.questionType,
      difficulty: params.difficulty, count: params.count,
      chunkIds: selectedKpIds.value.length > 0 ? selectedKpIds.value : undefined,
      customText: customText.value || undefined,
      knowledgeId: selectedKpIds.value[0],
      knowledgeName: confirmedKps.value.find(k => k.knowledgeId === selectedKpIds.value[0])?.name
    })
    if (res.data.code === 200) {
      ElMessage.success(`生成完成，共 ${res.data.data?.count || 0} 道题目`)
      // reload questions - they were saved to DB as pending, show them
      generatedQuestions.value = []
    } else {
      ElMessage.error(res.data.message || '生成失败')
    }
  } catch (e: any) {
    ElMessage.error(e.message || '生成失败')
  } finally {
    generating.value = false
  }
}

onMounted(async () => {
  const [cfgRes, kpRes] = await Promise.all([getTrainingConfig(), getConfirmedKnowledge()])
  if (cfgRes.data.code === 200) settings.value = cfgRes.data.data || null
  if (kpRes.data.code === 200) confirmedKps.value = kpRes.data.data || []
})
</script>

<style scoped lang="scss">
.training-page { padding: 20px; height: 100%; overflow-y: auto; }
.page-header { margin-bottom: 20px; }
.page-header h2 { margin: 0; color: #fff; font-size: 22px; }
.subtitle { color: rgba(220,238,255,0.7); margin: 6px 0 0; font-size: 14px; }
.field-label { color: #7bd7ff; font-size: 13px; display: block; margin-bottom: 4px; }
</style>
