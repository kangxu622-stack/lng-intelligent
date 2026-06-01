<template>
  <router-view v-if="isRouterAlive" :class="[mode]" />
</template>

<script setup lang="ts">
import { ref, computed, provide } from 'vue'
import { useSettingStore } from './store/modules/setting'

const isRouterAlive = ref(true)
const settingStore = useSettingStore()

const mode = computed(() => settingStore.mode)

const reload = () => {
  isRouterAlive.value = false
  setTimeout(() => {
    isRouterAlive.value = true
  }, 0)
}

provide('reload', reload)
</script>
