<template>
  <pagePanelNew class="training-page">
    <div class="page-header">
      <h2>知识点管理</h2>
      <p class="subtitle">从手册切块中提取知识点，确认后入库作为出题依据。</p>
    </div>

    <el-tabs v-model="activeTab">
      <el-tab-pane label="提取知识点" name="extract">
        <cm-panel>
          <el-select v-model="selectedManualId" placeholder="选择手册" @change="loadChunks" style="width:400px">
            <el-option v-for="m in manuals" :key="m.manualId" :label="m.manualName" :value="m.manualId" />
          </el-select>
          <div style="margin-top:12px">
            <el-checkbox-group v-model="selectedChunkIds">
              <div v-for="(c, i) in chunks" :key="c.chunkId" style="margin-bottom:6px">
                <el-checkbox :value="c.chunkId">
                  {{ c.chapterTitle }} — {{ (c.content || '').substring(0, 60) }}...
                </el-checkbox>
              </div>
            </el-checkbox-group>
          </div>
          <div v-if="selectedChunkIds.length > 0" style="margin-top:12px">
            <el-input type="textarea" :rows="6" :model-value="combinedContent" readonly />
            <el-button style="margin-top:10px" type="primary" :loading="extracting" @click="doExtract">
              调用大模型提取知识点
            </el-button>
          </div>
          <div v-if="extractResults.length > 0" style="margin-top:12px">
            <p style="color:#7bd7ff">提取结果 ({{ extractResults.length }} 条)</p>
            <el-button type="success" @click="doSaveAll">确认并保存所有知识点</el-button>
          </div>
        </cm-panel>
      </el-tab-pane>

      <el-tab-pane label="候选知识点" name="pending">
        <div v-for="kp in pendingList" :key="kp.knowledgeId" class="kp-card">
          <cm-panel>
            <div class="kp-row">
              <div class="kp-info">
                <strong>{{ kp.name }}</strong>
                <span>系统: {{ kp.systemName || '-' }} | 岗位: {{ kp.position || '-' }}</span>
                <span>难度: {{ kp.difficulty || '-' }} | 风险: {{ kp.riskLevel || '-' }}</span>
                <span>关联题目: {{ kp.questionCount }} | 错题: {{ kp.wrongCount }}</span>
              </div>
              <div class="kp-actions">
                <el-button size="small" type="primary" @click="doConfirm(kp.knowledgeId)">确认入库</el-button>
                <el-button size="small" type="danger" @click="doDelete(kp.knowledgeId)">删除</el-button>
              </div>
            </div>
          </cm-panel>
        </div>
        <div v-if="pendingList.length === 0" class="empty-msg">暂无待确认知识点。</div>
      </el-tab-pane>

      <el-tab-pane label="已确认知识点" name="confirmed">
        <div v-if="confirmedList.length > 0" style="color:#7bd7ff;margin-bottom:10px">共 {{ confirmedList.length }} 个已确认知识点</div>
        <div v-for="kp in confirmedList" :key="kp.knowledgeId" class="kp-card">
          <cm-panel>
            <div class="kp-row">
              <div class="kp-info">
                <strong>{{ kp.name }}</strong>
                <span>系统: {{ kp.systemName || '-' }} | 岗位: {{ kp.position || '-' }}</span>
                <span>关联题目: {{ kp.questionCount }} | 错题: {{ kp.wrongCount }}</span>
              </div>
              <div class="kp-actions">
                <el-button size="small" type="danger" @click="doDelete(kp.knowledgeId)">删除</el-button>
              </div>
            </div>
          </cm-panel>
        </div>
        <div v-if="confirmedList.length === 0" class="empty-msg">暂无已确认知识点。</div>
      </el-tab-pane>
    </el-tabs>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({ name: 'TrainingKnowledgeManagement' })
import { onMounted, ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getManuals, getKnowledgeChunks, extractKnowledge, getPendingKnowledge,
  getConfirmedKnowledge, confirmKnowledge, deleteKnowledge,
  type ManualDto, type ManualChunkDto, type KnowledgePointDto
} from '@/api/training'

const activeTab = ref('extract')
const manuals = ref<ManualDto[]>([])
const selectedManualId = ref('')
const chunks = ref<ManualChunkDto[]>([])
const selectedChunkIds = ref<string[]>([])
const extracting = ref(false)
const extractResults = ref<any[]>([])
const pendingList = ref<KnowledgePointDto[]>([])
const confirmedList = ref<KnowledgePointDto[]>([])

const combinedContent = computed(() => {
  return selectedChunkIds.value
    .map(id => chunks.value.find(c => c.chunkId === id))
    .filter(Boolean)
    .map(c => `## ${c!.chapterTitle}\n${c!.content}`)
    .join('\n\n')
})

const loadChunks = async () => {
  if (!selectedManualId.value) return
  const res = await getKnowledgeChunks(selectedManualId.value)
  if (res.data.code === 200) chunks.value = res.data.data || []
}

const doExtract = async () => {
  extracting.value = true
  try {
    const res = await extractKnowledge(selectedChunkIds.value)
    if (res.data.code === 200) {
      extractResults.value = res.data.data?.savedIds || []
      ElMessage.success(`提取完成，共 ${res.data.data?.count || 0} 条候选知识点`)
    } else {
      ElMessage.error(res.data.message || '提取失败')
    }
  } catch (e: any) {
    ElMessage.error(e.message || '提取失败')
  } finally {
    extracting.value = false
  }
}

const doSaveAll = async () => {
  ElMessage.success('知识点已保存')
  extractResults.value = []
  await refreshPending()
}

const doConfirm = async (id: string) => {
  const res = await confirmKnowledge(id)
  if (res.data.code === 200) { ElMessage.success('已确认入库'); await refreshAll() }
  else ElMessage.error(res.data.message || '确认失败')
}

const doDelete = async (id: string) => {
  try {
    const res = await deleteKnowledge(id)
    if (res.data.code === 200) { ElMessage.success('已删除'); await refreshAll() }
    else ElMessage.error(res.data.message || '删除失败')
  } catch (e: any) { ElMessage.error(e.message || '删除失败') }
}

const refreshPending = async () => {
  const res = await getPendingKnowledge()
  if (res.data.code === 200) pendingList.value = res.data.data || []
}

const refreshConfirmed = async () => {
  const res = await getConfirmedKnowledge()
  if (res.data.code === 200) confirmedList.value = res.data.data || []
}

const refreshAll = async () => { await refreshPending(); await refreshConfirmed() }

onMounted(async () => {
  const res = await getManuals()
  if (res.data.code === 200) manuals.value = res.data.data || []
  await refreshAll()
})
</script>

<style scoped lang="scss">
.training-page { padding: 20px; height: 100%; overflow-y: auto; }
.page-header { margin-bottom: 20px; }
.page-header h2 { margin: 0; color: #fff; font-size: 22px; }
.subtitle { color: rgba(220,238,255,0.7); margin: 6px 0 0; font-size: 14px; }
.kp-card { margin-bottom: 10px; }
.kp-row { display: flex; justify-content: space-between; align-items: center; gap: 10px; }
.kp-info { display: flex; gap: 14px; flex-wrap: wrap; color: #e0e6ff; font-size: 13px; }
.kp-info strong { color: #7bd7ff; font-size: 14px; }
.kp-actions { display: flex; gap: 6px; }
.empty-msg { color: rgba(220,238,255,0.5); text-align: center; padding: 40px; }
</style>
