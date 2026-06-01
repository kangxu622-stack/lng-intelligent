<template>
  <side-nav
    v-if="showSidebar"
    :show-logo="showSidebarLogo"
    :layout="setting.layout"
    :is-fixed="setting.isSidebarFixed"
    :menu="sideMenu"
    :theme="mode"
    :is-compact="setting.isSidebarCompact"
    :max-level="setting.splitMenu ? 2 : 3"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSettingStore } from '@/store/modules/setting'
import { asyncRouterList } from '@/router'
import SideNav from './SideNav.vue'
import proxy from '@/config/host'

const env = import.meta.env.MODE || 'development'

const route = useRoute()
const settingStore = useSettingStore()

const showSidebar = computed(() => settingStore.showSidebar)
const showSidebarLogo = computed(() => settingStore.showSidebarLogo)
const mode = computed(() => settingStore.mode)

const setting = computed(() => ({
  layout: settingStore.layout,
  isSidebarFixed: settingStore.isSidebarFixed,
  isSidebarCompact: settingStore.isSidebarCompact,
  splitMenu: settingStore.splitMenu
}))

const sideMenu = computed(() => {
  const { layout, splitMenu } = setting.value
  let menuRouters = asyncRouterList

  menuRouters.forEach((el: any) => {
    el.children?.forEach((element: any) => {
      element.children?.forEach((item: any) => {
        item.isThirdRouter = true
      })
    })
  })

  if (layout === 'mix') {
    if (splitMenu) {
      menuRouters.forEach(menu => {
        if (route.path.indexOf(menu.path) === 0) {
          menuRouters = menu.children?.map((subMenu: any) => ({
            ...subMenu,
            path: `${menu.path}/${subMenu.path}`
          })) || []
        }
      })
    }
  }
  return menuRouters.filter((v: any) => !v.appId || v.appId === proxy[env as keyof typeof proxy].appId)
})
</script>
<style lang="less" scoped></style>
