<template>
  <t-layout :class="[`${prefix}-layout`]">
    <t-tabs
      v-if="isUseTabsRouter && !isInIframe"
      theme="card"
      :class="`${prefix}-layout-tabs-nav`"
      :value="route.path"
      :style="{ position: 'sticky', top: 0, width: '100%' }"
      @change="handleChangeCurrentTab"
    >
      <t-tab-panel
        v-for="(routeItem, idx) in tabRouterList"
        :key="`${routeItem.path}_${idx}`"
        :value="routeItem.path"
        :removable="!routeItem.isHome"
        @remove="() => handleRemove(routeItem.path, idx)"
      >
        <template #label>
          <t-dropdown
            trigger="context-menu"
            :min-column-width="128"
            :popup-props="{
              overlayClassName: 'route-tabs-dropdown',
              onVisibleChange: (visible: boolean, ctx: any) => handleTabMenuClick(visible, ctx, routeItem.path),
              visible: activeTabPath === routeItem.path,
            }"
          >
            <template v-if="!routeItem.isHome">
              <el-tooltip :content="routeItem.title + (routeItem.query?.pathName ? '-' + routeItem.query?.pathName : '')">
                <div style="max-width: 400px; text-overflow: ellipsis; overflow: hidden; white-space: pre;">
                  {{ routeItem.title + (routeItem.query?.pathName ? '-' + routeItem.query?.pathName : '') }}
                </div>
              </el-tooltip>
            </template>
            <home-icon v-else />
            <template #dropdown>
              <t-dropdown-menu>
                <t-dropdown-item @click="() => handleRefresh(routeItem.path, idx)">
                  <refresh-icon />
                  刷新
                </t-dropdown-item>
                <t-dropdown-item v-if="idx > 0" @click="() => handleCloseAhead(routeItem.path, idx)">
                  <arrow-left-icon />
                  关闭左侧
                </t-dropdown-item>
                <t-dropdown-item
                  v-if="idx < tabRouterList.length - 1"
                  @click="() => handleCloseBehind(routeItem.path, idx)"
                >
                  <arrow-right-icon />
                  关闭右侧
                </t-dropdown-item>
                <t-dropdown-item @click="() => handleCloseOther(routeItem.path, idx)">
                  <close-circle-icon />
                  关闭其它
                </t-dropdown-item>
              </t-dropdown-menu>
            </template>
          </t-dropdown>
        </template>
      </t-tab-panel>
    </t-tabs>

    <t-content :class="`${prefix}-content-layout`" :style="{ overflow: showFooter ? 'hidden auto' : '' }">
      <layout-breadcrumb v-if="setting.showBreadcrumb" />
      <common-content />
    </t-content>
    <t-footer v-if="showFooter && route.path !== '/DevelopmentManagement/DemoIndex2'" :class="`${prefix}-footer-layout`">
      <layout-footer />
    </t-footer>
  </t-layout>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { RefreshIcon, ArrowLeftIcon, ArrowRightIcon, HomeIcon, CloseCircleIcon } from 'tdesign-icons-vue-next'
import CommonContent from './Content.vue'
import LayoutBreadcrumb from './Breadcrumb.vue'
import LayoutFooter from './Footer.vue'
import { prefix } from '@/config/global'
import { useSettingStore } from '@/store/modules/setting'
import { useTabRouterStore } from '@/store/modules/tab-router'

const route = useRoute()
const router = useRouter()
const settingStore = useSettingStore()
const tabRouterStore = useTabRouterStore()

const activeTabPath = ref<string>('')

const showFooter = computed(() => settingStore.showFooter)
const isUseTabsRouter = computed(() => settingStore.isUseTabsRouter)
const tabRouterList = computed(() => tabRouterStore.tabRouterList)
const isInIframe = computed(() => window.self !== window.top)

const setting = computed(() => ({
  showBreadcrumb: settingStore.showBreadcrumb
}))

const handleRemove = (path: string, routeIdx: number) => {
  const nextRouter = tabRouterList.value[routeIdx + 1] || tabRouterList.value[routeIdx - 1]

  tabRouterStore.subtractCurrentTabRouter({ path, routeIdx, title: '', name: '' })
  if (path === route.path && nextRouter) {
    const tabRouterItem = tabRouterList.value.find(item => item.path === nextRouter.path) || nextRouter
    router.push({ path: nextRouter.path, query: (tabRouterItem as any).query || {} })
  }
}

const handleChangeCurrentTab = (path: string) => {
  const tabRouterItem = tabRouterList.value.find(item => item.path === path) || { path }
  router.push({ path: tabRouterItem.path, query: (tabRouterItem as any).query || {} })
}

const handleRefresh = (currentPath: string, routeIdx: number) => {
  tabRouterStore.toggleTabRouterAlive(routeIdx)
  nextTick(() => {
    tabRouterStore.toggleTabRouterAlive(routeIdx)
    router.replace({ path: currentPath, query: { ...route.query } })
  })
  activeTabPath.value = ''
}

const handleCloseAhead = (path: string, routeIdx: number) => {
  tabRouterStore.subtractTabRouterAhead({ path, routeIdx, title: '', name: '' })
  handleOperationEffect('ahead', routeIdx)
}

const handleCloseBehind = (path: string, routeIdx: number) => {
  tabRouterStore.subtractTabRouterBehind({ path, routeIdx, title: '', name: '' })
  handleOperationEffect('behind', routeIdx)
}

const handleCloseOther = (path: string, routeIdx: number) => {
  tabRouterStore.subtractTabRouterOther({ path, routeIdx, title: '', name: '' })
  handleOperationEffect('other', routeIdx)
}

const handleOperationEffect = (type: 'other' | 'ahead' | 'behind', routeIndex: number) => {
  const currentPath = route.path
  const tabRouters = tabRouterList.value
  const currentIdx = tabRouters.findIndex((i: { path: string }) => i.path === currentPath)
  const needRefreshRouter =
    (type === 'other' && currentIdx !== routeIndex) ||
    (type === 'ahead' && currentIdx < routeIndex) ||
    (type === 'behind' && currentIdx === -1)
  if (needRefreshRouter) {
    let nextRouteIdx: number
    switch (type) {
      case 'behind':
        nextRouteIdx = tabRouters.length - 1
        break
      case 'other':
        nextRouteIdx = tabRouterList.value.length - 1
        break
      case 'ahead':
        nextRouteIdx = 1
        break
      default:
        nextRouteIdx = tabRouters.length - 1
        break
    }
    const nextRouter = tabRouterList.value[nextRouteIdx]
    if (nextRouter) {
      const tabRouterItem = tabRouterList.value.find(item => item.path === nextRouter.path) || nextRouter
      router.push({ path: nextRouter.path, query: (tabRouterItem as any).query || {} })
    }
  }

  activeTabPath.value = ''
}

const handleTabMenuClick = (visible: boolean, ctx: any, path: string) => {
  if (ctx?.trigger === 'document') activeTabPath.value = ''
  if (visible) activeTabPath.value = path
}
</script>
<style lang="scss" scoped>
.light {
  .tdesign-starter-layout-tabs-nav {
    border-bottom: 1px solid #dfe4e8;
  }
}
</style>
