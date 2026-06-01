<template>
  <common-header
    v-if="showHeader"
    :show-logo="showHeaderLogo"
    :theme="mode"
    :layout="setting.layout"
    :is-fixed="setting.isHeaderFixed"
    :menu="headerMenu"
    :is-compact="setting.isSidebarCompact"
    :max-level="setting.splitMenu ? 1 : 3"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useSettingStore } from '@/store/modules/setting'
import CommonHeader from './Header.vue'
import proxy from '@/config/host'
import { asyncRouterList } from '@/router'

const env = import.meta.env.MODE || 'development'

const settingStore = useSettingStore()

const showHeader = computed(() => settingStore.showHeader)
const showHeaderLogo = computed(() => settingStore.showHeaderLogo)
const mode = computed(() => settingStore.mode)

const setting = computed(() => ({
  layout: settingStore.layout,
  isHeaderFixed: settingStore.isHeaderFixed,
  isSidebarCompact: settingStore.isSidebarCompact,
  splitMenu: settingStore.splitMenu
}))

const headerMenu = computed(() => {
  const { layout, splitMenu } = setting.value
  const menuRouters = asyncRouterList
  if (layout === 'mix') {
    if (splitMenu) {
      return menuRouters.map(menu => ({
        ...menu,
        children: []
      }))
    }
    return []
  }
  return menuRouters.filter(v => !v.appId || v.appId === proxy[env as keyof typeof proxy].appId)
})
</script>
