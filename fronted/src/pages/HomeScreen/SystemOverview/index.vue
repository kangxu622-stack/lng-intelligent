<template>
  <div class="system-overview-container">
    <Graph />
  </div>
</template>

<script setup lang="ts">
import Graph from './graph.vue'
</script>

<style lang="scss" scoped>
.system-overview-container {
  width: 100%;
  height: 100%;
  overflow: hidden;
  padding: 10px;
}
</style>
