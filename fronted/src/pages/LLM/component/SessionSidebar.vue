<template>
  <aside class="session-sidebar">
    <cm-panel class="session-panel">
      <div class="session-actions">
        <el-button
          class="session-action"
          :class="{ active: activeConversationId === 'new' }"
          text
          @click="$emit('new-conversation')"
        >
          <el-icon><Plus /></el-icon>
          <span>新建对话</span>
        </el-button>
        <el-button class="session-action" text @click="$emit('restore-history')">
          <el-icon><Clock /></el-icon>
          <span>历史对话</span>
        </el-button>
      </div>

      <div class="session-list">
        <div
          v-for="item in conversations"
          :key="item.id"
          class="session-item"
          :class="{ active: activeConversationId === item.id }"
        >
          <button class="session-item-main" @click="$emit('select-conversation', item.id)">
            <span class="session-title">{{ item.title }}</span>
            <span class="session-time">{{ item.time }}</span>
          </button>
          <el-button
            class="session-delete"
            text
            @click.stop="$emit('delete-conversation', item.id)"
          >
            <el-icon><Delete /></el-icon>
          </el-button>
        </div>
      </div>
    </cm-panel>
  </aside>
</template>

<script setup lang="ts">
import { Clock, Delete, Plus } from '@element-plus/icons-vue'

export interface SessionConversationItem {
  id: string
  title: string
  time: string
}

defineProps<{
  conversations: SessionConversationItem[]
  activeConversationId: string
}>()

defineEmits<{
  (e: 'new-conversation'): void
  (e: 'restore-history'): void
  (e: 'select-conversation', id: string): void
  (e: 'delete-conversation', id: string): void
}>()
</script>

<style scoped lang="scss">
.session-sidebar {
  width: 260px;
  flex-shrink: 0;
  min-height: 0;
}

.session-panel {
  height: 100%;
  padding: 10px 0;
  display: flex;
  flex-direction: column;
}

.session-actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.session-action {
  display: inline-flex;
  justify-content: flex-start;
  align-items: center;
  gap: 8px;
  width: 100%;
  height: 42px;
  padding: 0 12px;
  color: #ffffff;
  border: 1px solid rgba(57, 181, 255, 0.28);
  background: rgba(8, 32, 57, 0.5);
  margin-left: 0 !important;
}

.session-action:hover,
.session-action:focus,
.session-action:active,
.session-action.active {
  color: #dceeff;
  border-color: #2da6ff;
  background: rgba(32, 99, 183, 0.35);
  box-shadow: inset 0 0 0 1px rgba(45, 166, 255, 0.18);
}

.session-action.is-text:not(.is-disabled):hover,
.session-action.is-text:not(.is-disabled):focus {
  color: #dceeff;
  background: rgba(32, 99, 183, 0.35);
}

.session-action :deep(.el-icon) {
  margin: 0 !important;
  font-size: 16px;
}

.session-action :deep(span),
.session-action :deep(.el-button__text) {
  margin-left: 0 !important;
}

.session-list {
  flex: 1;
  margin-top: 14px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  overflow: auto;
}

.session-item {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
  padding: 0 8px 0 10px;
  border: 1px solid rgba(57, 181, 255, 0.2);
  background: rgba(7, 29, 53, 0.45);
  transition: all 0.2s ease;
}

.session-item.active,
.session-item:hover {
  border-color: #2da6ff;
  background: rgba(32, 99, 183, 0.35);
  box-shadow: inset 0 0 0 1px rgba(45, 166, 255, 0.18);
}

.session-item-main {
  flex: 1;
  min-width: 0;
  height: 38px;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0;
  border: 0;
  background: transparent;
  color: #dceeff;
  cursor: pointer;
  text-align: left;
}

.session-title {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;
}

.session-time {
  flex-shrink: 0;
  color: rgba(220, 238, 255, 0.65);
  font-size: 11px;
  line-height: 1;
}

.session-delete {
  width: 24px;
  height: 24px;
  padding: 0;
  color: rgba(220, 238, 255, 0.72);
}

.session-delete:hover {
  color: #ffffff;
}
</style>
