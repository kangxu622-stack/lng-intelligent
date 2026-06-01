<template>
  <router-view v-slot="{ Component, route: currentRoute }">
    <transition name="fade" mode="out-in">
      <Suspense>
        <component
          v-if="Component"
          :is="Component"
          :key="currentRoute.fullPath || currentRoute.path"
        />
        <template #fallback>
          <div class="route-loading">页面加载中...</div>
        </template>
      </Suspense>
    </transition>
  </router-view>
</template>

<script setup lang="ts"></script>

<style lang="less" scoped>
@import "@/style/variables";

.fade-leave-active,
.fade-enter-active {
  transition: opacity @anim-duration-slow @anim-time-fn-easing;
}

.fade-enter,
.fade-leave-to {
  opacity: 0;
}

.route-loading {
  padding: 20px;
  color: #fff;
}
</style>
