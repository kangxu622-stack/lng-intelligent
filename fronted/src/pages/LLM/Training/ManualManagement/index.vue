<template>
  <pagePanelNew class="training-page">
    <div class="page-header">
      <h2>手册知识库</h2>
      <p class="subtitle">上传操作手册，解析文本并进行内容切块，为知识点提取做准备。</p>
    </div>

    <el-tabs v-model="activeTab">
      <el-tab-pane label="上传手册" name="upload">
        <cm-panel>
          <p>支持格式: .txt, .docx, .pdf</p>
          <el-upload
            :auto-upload="false"
            :show-file-list="false"
            accept=".txt,.docx,.pdf"
            action="#"
            @change="handleFileUpload"
          >
            <el-button type="primary" :loading="uploading">选择文件并上传</el-button>
          </el-upload>
          <div v-if="uploadResult" class="result-msg">{{ uploadResult }}</div>
        </cm-panel>
      </el-tab-pane>

      <el-tab-pane label="手册列表" name="list">
        <cm-panel v-for="m in manuals" :key="m.manualId" class="manual-item">
          <div class="manual-row">
            <div class="manual-info">
              <strong>{{ m.manualName }}</strong>
              <span class="tag">类型: {{ m.fileType || '-' }}</span>
              <span class="tag">状态: {{ m.status }}</span>
              <span class="tag">切块数: {{ m.chunkCount }}</span>
              <span class="tag">上传时间: {{ m.uploadTime }}</span>
            </div>
            <div class="manual-actions">
              <el-button size="small" @click="doParse(m.manualId)">解析文本</el-button>
              <el-button size="small" type="primary" @click="doChunk(m.manualId)">文本切块</el-button>
              <el-button size="small" type="danger" @click="doDelete(m.manualId, m.manualName)">删除</el-button>
            </div>
          </div>
          <div v-if="parsedText && currentManualId === m.manualId" class="parsed-area">
            <p>解析结果 ({{ parsedLength }} 字符)</p>
            <el-input type="textarea" :rows="8" :model-value="parsedText" readonly />
          </div>
          <div v-if="chunks.length && currentManualId === m.manualId" class="chunks-area">
            <p>切块结果 ({{ chunks.length }} 块)</p>
            <div v-for="c in chunks" :key="c.chunkId" class="chunk-item">
              <strong>{{ c.chapterTitle }} (块#{{ c.sectionNo }}, 系统: {{ c.systemName }})</strong>
              <el-input type="textarea" :rows="4" :model-value="c.content || ''" readonly />
            </div>
          </div>
        </cm-panel>
        <div v-if="manuals.length === 0" class="empty-msg">暂无手册，请先上传。</div>
      </el-tab-pane>

      <el-tab-pane label="内容检索" name="search">
        <cm-panel>
          <el-input v-model="searchKeyword" placeholder="输入关键词搜索，例如: 储罐、BOG、应急..." @keyup.enter="doSearch" />
          <el-button style="margin-top:10px" type="primary" @click="doSearch">搜索</el-button>
          <div v-for="r in searchResults" :key="r.chunkId" class="chunk-item" style="margin-top:10px">
            <strong>{{ r.chapterTitle }} (系统: {{ r.systemName }})</strong>
            <el-input type="textarea" :rows="4" :model-value="r.content || ''" readonly />
          </div>
          <div v-if="searched && searchResults.length === 0">无搜索结果</div>
        </cm-panel>
      </el-tab-pane>
    </el-tabs>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({ name: 'TrainingManualManagement' })
import { onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  uploadManual, getManuals, parseManual, chunkManual, deleteManual, searchChunks,
  type ManualDto, type ManualChunkDto
} from '@/api/training'

const activeTab = ref('list')
const uploading = ref(false)
const uploadResult = ref('')
const manuals = ref<ManualDto[]>([])
const parsedText = ref('')
const parsedLength = ref(0)
const currentManualId = ref('')
const chunks = ref<ManualChunkDto[]>([])
const searchKeyword = ref('')
const searchResults = ref<ManualChunkDto[]>([])
const searched = ref(false)

const refreshManuals = async () => {
  const res = await getManuals()
  if (res.data.code === 200) manuals.value = res.data.data || []
}

const handleFileUpload = async (file: any) => {
  if (!file.raw) return
  uploading.value = true
  try {
    const res = await uploadManual(file.raw)
    if (res.data.code === 200) {
      uploadResult.value = `上传成功: ${file.name}`
      ElMessage.success('上传成功')
      await refreshManuals()
      activeTab.value = 'list'
    }
  } catch (e: any) {
    ElMessage.error(e.message || '上传失败')
  } finally {
    uploading.value = false
  }
}

const doParse = async (manualId: string) => {
  try {
    const res = await parseManual(manualId)
    if (res.data.code === 200) {
      parsedText.value = res.data.data?.text || ''
      parsedLength.value = res.data.data?.length || 0
      currentManualId.value = manualId
      ElMessage.success('解析成功')
    }
  } catch (e: any) {
    ElMessage.error(e.message || '解析失败')
  }
}

const doChunk = async (manualId: string) => {
  try {
    const res = await chunkManual(manualId)
    if (res.data.code === 200) {
      chunks.value = res.data.data || []
      currentManualId.value = manualId
      ElMessage.success(`切块完成，共 ${chunks.value.length} 个片段`)
      await refreshManuals()
    }
  } catch (e: any) {
    ElMessage.error(e.message || '切块失败')
  }
}

const doDelete = async (manualId: string, name: string) => {
  try {
    await ElMessageBox.confirm(`确认删除手册《${name}》？`, '提示', { type: 'warning' })
    const res = await deleteManual(manualId)
    if (res.data.code === 200) {
      ElMessage.success('删除成功')
      await refreshManuals()
    }
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

const doSearch = async () => {
  if (!searchKeyword.value.trim()) return
  searched.value = true
  try {
    const res = await searchChunks(searchKeyword.value.trim())
    if (res.data.code === 200) searchResults.value = res.data.data || []
  } catch (e: any) {
    ElMessage.error(e.message || '搜索失败')
  }
}

onMounted(() => { refreshManuals() })
</script>

<style scoped lang="scss">
.training-page { padding: 20px; height: 100%; overflow-y: auto; }
.page-header { margin-bottom: 20px; }
.page-header h2 { margin: 0; color: #fff; font-size: 22px; }
.subtitle { color: rgba(220,238,255,0.7); margin: 6px 0 0; font-size: 14px; }
.manual-item { margin-bottom: 12px; }
.manual-row { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px; }
.manual-info { display: flex; gap: 12px; flex-wrap: wrap; align-items: center; color: #e0e6ff; }
.tag { padding: 2px 8px; border: 1px solid rgba(57,181,255,0.3); background: rgba(8,32,57,0.5); color: #7bd7ff; font-size: 12px; }
.manual-actions { display: flex; gap: 6px; }
.parsed-area, .chunks-area { margin-top: 12px; padding: 10px; border: 1px solid rgba(48,188,255,0.3); }
.parsed-area p, .chunks-area p { color: #7bd7ff; margin: 0 0 6px; }
.chunk-item { margin-top: 8px; }
.chunk-item strong { color: #e0e6ff; display: block; margin-bottom: 4px; }
.result-msg { margin-top: 10px; color: #4caf50; }
.empty-msg { color: rgba(220,238,255,0.5); text-align: center; padding: 40px; }
</style>
