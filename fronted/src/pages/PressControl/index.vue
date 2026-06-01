<template>
  <div class="design-container">
    <div class="design-content">
      <div class="empty-placeholder">
        <p class="empty-text">系统设计页面</p>
        <p class="empty-desc">此页面待开发</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// 系统设计页面
</script>

<style lang="scss" scoped>
.design-container {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  box-sizing: border-box;
}

.design-content {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-placeholder {
  text-align: center;
  color: var(--td-text-color-secondary);
}

.empty-text {
  font-size: 24px;
  font-weight: 500;
  margin-bottom: 10px;
  color: var(--td-text-color-primary);
}

.empty-desc {
  font-size: 14px;
  color: var(--td-text-color-placeholder);
}
</style>

