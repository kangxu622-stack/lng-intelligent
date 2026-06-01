<template>
  <div class="login-wrapper">
    <div class="login-scene">
      <div class="scene-title">LNG接收站智能启停系统</div>

      <div class="login-panel-wrap">
        <login-panel />
      </div>

      <div class="scene-version">V1.0</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import LoginPanel from './components/components-login.vue'
</script>

<style lang="less">
@import url("./index.less");
</style>
