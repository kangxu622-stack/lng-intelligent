<template>
  <pagePanelNew class="scheme-explanation-page">
    <div class="scheme-layout">
      <section class="scheme-panel left-panel">
        <div class="panel-frame">
          <div class="strategy-preview">
            <div class="preview-image">
              <div class="preview-grid">
                <div
                  v-for="cell in 168"
                  :key="cell"
                  class="preview-cell"
                  :class="{ active: activePreviewCells.has(cell) }"
                ></div>
              </div>
            </div>
            <div class="preview-title">上传调度策略</div>
          </div>

          <div class="panel-actions">
            <el-button type="primary" @click="handleSelectFile">选择</el-button>
            <el-upload
              :auto-upload="false"
              :show-file-list="false"
              accept=".png,.jpg,.jpeg,.pdf,.doc,.docx,.ppt,.pptx"
              action="#"
              @change="handleUploadChange"
            >
              <el-button type="primary">上传</el-button>
            </el-upload>
          </div>
        </div>
      </section>

      <div class="arrow-section">
        <el-icon><Right /></el-icon>
      </div>

      <section class="scheme-panel right-panel">
        <div class="panel-frame result-frame">
          <div class="result-content">
            <div class="result-title">生成调度方案（支持word、ppt）</div>
            <div v-if="uploadedFileName" class="result-file">当前文件：{{ uploadedFileName }}</div>
          </div>

          <div class="panel-actions panel-actions-right">
            <el-button type="primary" @click="handleExport">导出</el-button>
          </div>
        </div>
      </section>
    </div>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({
  name: 'SchemeExplanation'
})

import { computed, ref } from 'vue'
import { ElMessage, type UploadFile, type UploadFiles } from 'element-plus'
import { Right } from '@element-plus/icons-vue'

const uploadedFileName = ref('')

const activePreviewCells = computed(() => {
  const active = new Set<number>()
  for (let i = 1; i <= 168; i += 1) {
    if (i % 3 === 0 || i % 5 === 0 || (i + Math.floor(i / 8)) % 7 === 0) {
      active.add(i)
    }
  }
  return active
})

const handleSelectFile = () => {
  ElMessage.info('请选择调度策略文件后上传')
}

const handleUploadChange = (file: UploadFile, _files: UploadFiles) => {
  uploadedFileName.value = file.name
  ElMessage.success(`已选择文件：${file.name}`)
}

const handleExport = () => {
  if (!uploadedFileName.value) {
    ElMessage.warning('请先上传调度策略文件')
    return
  }
  ElMessage.success('导出功能已预留，后续可接入word/ppt生成接口')
}
</script>

<style scoped lang="scss">
.scheme-explanation-page {
  height: 100%;
  overflow: scroll;
}

.scheme-layout {
  height: 100%;
  min-height: 0;
  padding: 10px;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 110px minmax(0, 1.08fr);
  gap: 28px;
}

.scheme-panel {
  min-height: 0;
}

.panel-frame {
  height: 100%;
  border: 1px solid rgba(48, 188, 255, 0.38);
  background: rgba(4, 23, 44, 0.22);
  padding: 22px 20px 18px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.strategy-preview {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: 20px;
}

.preview-image {
  width: min(620px, 92%);
  aspect-ratio: 1.95 / 1;
  padding: 18px;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(57, 181, 255, 0.3);
}

.preview-grid {
  width: 100%;
  height: 100%;
  display: grid;
  grid-template-columns: repeat(14, 1fr);
  grid-template-rows: repeat(12, 1fr);
  gap: 2px;
}

.preview-cell {
  background: #ffffff;
}

.preview-cell.active {
  background: #17b61a;
}

.preview-title,
.result-title {
  margin-top: 70px;
  color: #ffffff;
  font-size: 26px;
  text-align: center;
}

.result-frame {
  align-items: center;
}

.result-content {
  flex: 1;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 18px;
}

.result-title {
  margin-top: 0;
}

.result-file {
  color: rgba(220, 238, 255, 0.8);
  font-size: 15px;
}

.panel-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.panel-actions-right {
  justify-content: flex-end;
  width: 100%;
}

.arrow-section {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #00e7ff;
  font-size: 80px;
}

@media (max-width: 1500px) {
  .scheme-layout {
    grid-template-columns: 1fr;
  }

  .arrow-section {
    transform: rotate(90deg);
    min-height: 80px;
  }

  .panel-frame {
    min-height: 560px;
  }
}
</style>
