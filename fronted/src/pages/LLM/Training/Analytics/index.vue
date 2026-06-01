<template>
  <pagePanelNew class="training-page">
    <div class="page-header">
      <h2>学习分析</h2>
      <p class="subtitle">查看个人训练统计和薄弱知识点分析。</p>
    </div>

    <el-tabs v-model="activeTab">
      <el-tab-pane label="个人统计" name="personal">
        <cm-panel v-if="analytics">
          <el-row :gutter="14">
            <el-col :span="6">
              <div class="stat-card"><div class="stat-num">{{ analytics.stats.total }}</div><div class="stat-label">总答题数</div></div>
            </el-col>
            <el-col :span="6">
              <div class="stat-card"><div class="stat-num" style="color:#4caf50">{{ analytics.stats.correct }}</div><div class="stat-label">正确数</div></div>
            </el-col>
            <el-col :span="6">
              <div class="stat-card"><div class="stat-num" style="color:#f44336">{{ analytics.stats.wrong }}</div><div class="stat-label">错误数</div></div>
            </el-col>
            <el-col :span="6">
              <div class="stat-card"><div class="stat-num">{{ analytics.stats.accuracy }}%</div><div class="stat-label">正确率</div></div>
            </el-col>
          </el-row>
        </cm-panel>

        <h3 style="color:#7bd7ff;margin-top:20px">知识点掌握详情</h3>
        <cm-panel v-if="analytics?.knowledgeAccuracy.length">
          <el-table :data="analytics.knowledgeAccuracy" style="width:100%" border>
            <el-table-column prop="knowledgeName" label="知识点" />
            <el-table-column prop="total" label="答题数" width="100" />
            <el-table-column prop="correct" label="正确数" width="100" />
            <el-table-column prop="accuracy" label="正确率(%)" width="120" />
            <el-table-column prop="level" label="掌握等级" width="120" />
          </el-table>
        </cm-panel>
        <div v-else class="empty-msg" style="padding:20px">暂无答题数据。</div>

        <h3 v-if="analytics?.weakKnowledge.length" style="color:#f44336;margin-top:20px">薄弱知识点 (正确率 &lt; 70%)</h3>
        <cm-panel v-for="w in analytics?.weakKnowledge" :key="w.knowledgeId || w.knowledgeName" style="margin-bottom:8px">
          <p>{{ w.knowledgeName }} — 正确率 {{ w.accuracy }}% ({{ w.level }})</p>
        </cm-panel>

        <el-button v-if="analytics?.weakKnowledge.length" style="margin-top:12px" type="primary" :loading="analyzing" @click="doAnalyze">
          AI 生成复习建议
        </el-button>
        <cm-panel v-if="analysis" style="margin-top:12px">
          <pre style="white-space:pre-wrap;color:#e0e6ff">{{ analysis }}</pre>
        </cm-panel>
      </el-tab-pane>

      <el-tab-pane label="全员视图" name="admin">
        <cm-panel v-if="adminStats">
          <el-row :gutter="14">
            <el-col :span="8"><div class="stat-card"><div class="stat-num">{{ adminStats.totalUsers }}</div><div class="stat-label">学员总数</div></div></el-col>
            <el-col :span="8"><div class="stat-card"><div class="stat-num">{{ adminStats.totalAnswers }}</div><div class="stat-label">总答题数</div></div></el-col>
            <el-col :span="8"><div class="stat-card"><div class="stat-num">{{ adminStats.avgAccuracy }}%</div><div class="stat-label">整体正确率</div></div></el-col>
          </el-row>

          <h4 style="color:#7bd7ff;margin-top:20px">学员统计</h4>
          <el-table v-if="adminStats.perUser.length" :data="adminStats.perUser" border style="width:100%">
            <el-table-column prop="name" label="姓名" />
            <el-table-column prop="position" label="岗位" />
            <el-table-column prop="total" label="答题数" />
            <el-table-column prop="correct" label="正确数" />
          </el-table>

          <h4 style="color:#7bd7ff;margin-top:20px">知识点全局统计</h4>
          <el-table v-if="adminStats.knowledgeStats.length" :data="adminStats.knowledgeStats" border style="width:100%">
            <el-table-column prop="knowledgeName" label="知识点" />
            <el-table-column prop="total" label="总答题" />
            <el-table-column prop="correct" label="正确数" />
          </el-table>
        </cm-panel>
      </el-tab-pane>
    </el-tabs>
  </pagePanelNew>
</template>

<script setup lang="ts">
defineOptions({ name: 'TrainingAnalytics' })
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { getUserAnalytics, getAdminStats as fetchAdmin, analyzeWrongQuestions, type UserAnalyticsDto, type AdminStatsDto } from '@/api/training'

const activeTab = ref('personal')
const analytics = ref<UserAnalyticsDto | null>(null)
const adminStats = ref<AdminStatsDto | null>(null)
const analysis = ref('')
const analyzing = ref(false)

const doAnalyze = async () => {
  analyzing.value = true
  try {
    const res = await analyzeWrongQuestions('student1')
    if (res.data.code === 200) analysis.value = res.data.data?.analysis || ''
  } catch (e: any) { ElMessage.error(e.message || '分析失败') }
  finally { analyzing.value = false }
}

onMounted(async () => {
  const [aRes, sRes] = await Promise.allSettled([getUserAnalytics('student1'), fetchAdmin()])
  if (aRes.status === 'fulfilled' && aRes.value.data.code === 200) analytics.value = aRes.value.data.data || null
  if (sRes.status === 'fulfilled' && sRes.value.data.code === 200) adminStats.value = sRes.value.data.data || null
})
</script>

<style scoped lang="scss">
.training-page { padding: 20px; height: 100%; overflow-y: auto; }
.page-header { margin-bottom: 20px; }
.page-header h2 { margin: 0; color: #fff; font-size: 22px; }
.subtitle { color: rgba(220,238,255,0.7); margin: 6px 0 0; font-size: 14px; }
.stat-card { text-align: center; padding: 14px; border: 1px solid rgba(48,188,255,0.3); background: rgba(4,23,44,0.42); }
.stat-num { font-size: 28px; color: #00e7ff; font-weight: bold; }
.stat-label { font-size: 13px; color: rgba(220,238,255,0.7); margin-top: 4px; }
.empty-msg { color: rgba(220,238,255,0.5); text-align: center; }
</style>
