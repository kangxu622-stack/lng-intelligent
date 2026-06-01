<template>
  <div :class="sideNavCls">
    <t-menu
      width="232px"
      :class="menuCls"
      :theme="theme"
      :value="active"
      :collapsed="collapsed"
      :expand-type="showLogo ? 'popup' : 'normal'"
    >
      <template #logo>
        <span v-if="showLogo" :class="`${prefix}-side-nav-logo-wrapper`">
          <img :src="logoSrc" :class="`${prefix}-side-nav-logo-t-logo side-nav-logo-image`" alt="logo" />
        </span>
        <span
          v-if="!collapsed && showLogo"
          class="side-nav-title"
          :style="{ color: settingStore.mode === 'light' ? '#000' : '#fff' }"
        >
          LNG接收站智能启停系统
        </span>
      </template>
      <menu-content :nav-data="menu" />
      <template #operations />
    </t-menu>
    <div :class="`${prefix}-side-nav-placeholder${collapsed ? '-hidden' : ''}`" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { useSettingStore } from '@/store/modules/setting'
import { prefix } from '@/config/global'
import type { MenuRoute } from '@/interface'
import MenuContent from './MenuContent.vue'
import logoSrc from '@/assets/校徽白色-背景透明版.gif'

const MIN_POINT = 992 - 1

const props = defineProps<{
  menu?: MenuRoute[]
  showLogo?: boolean
  isFixed?: boolean
  layout?: string
  headerHeight?: string
  theme?: string
  isCompact?: boolean
  maxLevel?: number
}>()

const route = useRoute()
const settingStore = useSettingStore()

const collapsed = computed(() => settingStore.isSidebarCompact)

const sideNavCls = computed(() => [
  `${prefix}-sidebar-layout`,
  {
    [`${prefix}-sidebar-compact`]: props.isCompact
  }
])

const menuCls = computed(() => [
  `${prefix}-side-nav`,
  {
    [`${prefix}-side-nav-no-logo`]: !props.showLogo,
    [`${prefix}-side-nav-no-fixed`]: !props.isFixed,
    [`${prefix}-side-nav-mix-fixed`]: props.layout === 'mix' && props.isFixed
  }
])

const active = computed(() => {
  if (!route.path) {
    return ''
  }

  return route.path
    .split('/')
    .filter((_item: string, index: number) => index <= (props.maxLevel || 3) && index > 0)
    .map((item: string) => `/${item}`)
    .join('')
})

const autoCollapsed = () => {
  const isCompact = window.innerWidth <= MIN_POINT
  settingStore.showSidebarCompactValue(isCompact)
}

onMounted(() => {
  autoCollapsed()
  window.addEventListener('resize', autoCollapsed)
})

onUnmounted(() => {
  window.removeEventListener('resize', autoCollapsed)
})
</script>

<style lang="less" scoped>
.side-nav-logo-image {
  width: 32px;
  height: 32px;
  object-fit: contain;
}

.side-nav-title {
  font-size: 16px;
  white-space: nowrap;
}
</style>
