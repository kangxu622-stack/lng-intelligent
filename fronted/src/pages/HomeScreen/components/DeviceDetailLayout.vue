<template>
  <pagePanelNew class="device-detail-page">
    <div class="device-detail-grid" :class="{ 'with-aside': hasAside }">
      <section class="device-detail-main">
        <pagePanel :header-title="summaryTitle" class="device-detail-panel">
          <slot name="summary"></slot>
        </pagePanel>
        <pagePanel :header-title="deviceTitle" class="device-detail-panel">
          <slot></slot>
        </pagePanel>
      </section>
      <aside v-if="hasAside" class="device-detail-aside">
        <pagePanel :header-title="asideTitle" class="device-detail-panel device-detail-panel-fill">
          <slot name="aside"></slot>
        </pagePanel>
      </aside>
    </div>
  </pagePanelNew>
</template>

<script setup lang="ts">
import { computed, useSlots } from 'vue'

interface Props {
  summaryTitle: string
  deviceTitle: string
  asideTitle?: string
}

withDefaults(defineProps<Props>(), {
  asideTitle: ''
})

const slots = useSlots()
const hasAside = computed(() => Boolean(slots.aside))
</script>

<style lang="scss" scoped>
.device-detail-page {
  height: 100%;
  padding: 10px;
}

.device-detail-grid {
  height: 100%;
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
}

.device-detail-grid.with-aside {
  grid-template-columns: minmax(0, 1.7fr) minmax(320px, 0.9fr);
}

.device-detail-main,
.device-detail-aside {
  min-height: 0;
}

.device-detail-main {
  display: grid;
  grid-template-rows: auto minmax(0, 1fr);
  gap: 16px;
}

.device-detail-panel {
  height: 100%;
}

.device-detail-panel-fill {
  min-height: 100%;
}

:deep(.device-metric-grid) {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 14px;
}

:deep(.device-metric-card) {
  min-height: 118px;
  padding: 16px 18px;
  border-radius: 12px;
  border: 1px solid rgba(104, 178, 255, 0.22);
  background: linear-gradient(180deg, rgba(20, 52, 93, 0.88), rgba(11, 31, 58, 0.92));
}

:deep(.device-metric-head) {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 14px;
}

:deep(.device-metric-label) {
  color: rgba(215, 235, 255, 0.72);
  font-size: 13px;
}

:deep(.device-metric-tag) {
  display: inline-flex;
  align-items: center;
  padding: 3px 10px;
  border-radius: 999px;
  border: 1px solid rgba(89, 205, 144, 0.35);
  background: rgba(89, 205, 144, 0.12);
  color: #83f0b0;
  font-size: 12px;
  white-space: nowrap;
}

:deep(.device-metric-tag.is-warning) {
  border-color: rgba(255, 206, 86, 0.35);
  background: rgba(255, 206, 86, 0.12);
  color: #ffd76a;
}

:deep(.device-metric-tag.is-error) {
  border-color: rgba(245, 108, 108, 0.35);
  background: rgba(245, 108, 108, 0.12);
  color: #ff9b9b;
}

:deep(.device-metric-tag.is-neutral) {
  border-color: rgba(124, 176, 255, 0.35);
  background: rgba(64, 158, 255, 0.12);
  color: #9ec9ff;
}

:deep(.device-metric-value) {
  color: #ffffff;
  font-size: 30px;
  font-weight: 700;
  line-height: 1;
}

:deep(.device-metric-unit) {
  margin-left: 6px;
  color: rgba(215, 235, 255, 0.6);
  font-size: 13px;
  font-weight: 500;
}

:deep(.device-metric-foot) {
  margin-top: 10px;
  color: rgba(215, 235, 255, 0.5);
  font-size: 12px;
}

:deep(.device-list-grid) {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(420px, 1fr));
  gap: 16px;
}

:deep(.device-inline-note) {
  margin-bottom: 14px;
  padding: 12px 16px;
  border-radius: 10px;
  border: 1px solid rgba(110, 180, 255, 0.18);
  background: rgba(11, 35, 65, 0.6);
  color: rgba(220, 236, 255, 0.82);
  font-size: 13px;
}

:deep(.device-inline-note.is-warning) {
  border-color: rgba(255, 206, 86, 0.3);
  background: rgba(74, 58, 20, 0.34);
  color: #ffe08a;
}

:deep(.device-inline-note-list) {
  display: grid;
  gap: 10px;
  margin-bottom: 14px;
}

:deep(.device-progress) {
  display: grid;
  gap: 6px;
}

:deep(.device-progress-track) {
  height: 8px;
  border-radius: 999px;
  overflow: hidden;
  background: rgba(133, 168, 214, 0.18);
}

:deep(.device-progress-fill) {
  height: 100%;
  background: linear-gradient(90deg, #1ec8ff, #6ee7ff);
}

:deep(.device-progress-text) {
  color: rgba(220, 236, 255, 0.7);
  font-size: 12px;
}

:deep(.device-info-card) {
  padding: 18px;
  border-radius: 12px;
  border: 1px solid rgba(104, 178, 255, 0.18);
  background: linear-gradient(180deg, rgba(21, 55, 96, 0.72), rgba(11, 31, 58, 0.84));
}

:deep(.device-info-title) {
  color: #ffffff;
  font-size: 18px;
  font-weight: 600;
  margin-bottom: 14px;
}

:deep(.device-info-grid) {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

:deep(.device-info-item) {
  display: grid;
  gap: 6px;
  padding: 12px 14px;
  border-radius: 10px;
  background: rgba(8, 25, 46, 0.5);
}

:deep(.device-info-label) {
  color: rgba(215, 235, 255, 0.58);
  font-size: 12px;
}

:deep(.device-info-value) {
  color: #ffffff;
  font-size: 14px;
  font-weight: 500;
}

:deep(.device-stat-list) {
  display: grid;
  gap: 12px;
}

:deep(.device-stat-row) {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 14px;
  border-radius: 10px;
  background: rgba(8, 25, 46, 0.5);
}

:deep(.device-stat-label) {
  color: rgba(215, 235, 255, 0.7);
  font-size: 13px;
}

:deep(.device-stat-value) {
  color: #ffffff;
  font-size: 16px;
  font-weight: 600;
}

@media (max-width: 1440px) {
  .device-detail-grid.with-aside {
    grid-template-columns: minmax(0, 1fr);
  }
}

@media (max-width: 900px) {
  :deep(.device-list-grid) {
    grid-template-columns: 1fr;
  }

  :deep(.device-info-grid) {
    grid-template-columns: 1fr;
  }
}
</style>
