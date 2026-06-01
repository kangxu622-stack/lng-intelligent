<template>
  <div>
    <template v-if="setting.layout === 'side'">
      <t-layout key="side">
        <t-aside v-if="!isInIframe">
          <layout-sidebar />
        </t-aside>
        <t-layout>
          <t-header v-if="!isInIframe">
            <layout-header />
          </t-header>
          <t-content :class="{ 'is-in-iframe': isInIframe }">
            <layout-content />
          </t-content>
        </t-layout>
      </t-layout>
    </template>
    <template v-else-if="setting.layout === 'top'">
      <t-layout key="top">
        <t-header v-if="!isInIframe">
          <layout-header />
        </t-header>
        <t-content :class="{ 'is-in-iframe': isInIframe }">
          <layout-content />
        </t-content>
      </t-layout>
    </template>
    <template v-else>
      <t-layout key="mix">
        <t-header v-if="!isInIframe">
          <layout-header />
        </t-header>
        <t-layout>
          <t-aside v-if="!isInIframe">
            <layout-sidebar />
          </t-aside>
          <t-content :class="{ 'is-in-iframe': isInIframe }">
            <layout-content />
          </t-content>
        </t-layout>
      </t-layout>
    </template>
    <Setting />
  </div>
</template>

<script setup lang="ts">
import { computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import { useSettingStore } from '@/store/modules/setting'
import { useTabRouterStore } from '@/store/modules/tab-router'
import LayoutHeader from './components/LayoutHeader.vue'
import LayoutContent from './components/LayoutContent.vue'
import LayoutSidebar from './components/LayoutSidebar.vue'
import Setting from './setting.vue'
// layout.less 已在 index.less 中引入，无需重复引入

const route = useRoute()
const settingStore = useSettingStore()
const tabRouterStore = useTabRouterStore()

// const paddingNum = ref('20px') // 暂时未使用

const setting = computed(() => ({
  layout: settingStore.layout
}))

const isInIframe = computed(() => window.self !== window.top)

const getTabRouterListCache = () => {
  const cached = sessionStorage.getItem('tabRouterList')
  if (cached) {
    tabRouterStore.initTabRouterList(JSON.parse(cached))
  }
}

const setTabRouterListCache = () => {
  sessionStorage.setItem('tabRouterList', JSON.stringify(tabRouterStore.tabRouterList))
}

watch(
  () => route.fullPath,
  () => {
    const { path, meta, name, query } = route
    tabRouterStore.appendTabRouterList({
      path,
      title: meta?.title as string,
      name: name as string,
      query: query || {},
      isAlive: true,
      routeIdx: 0
    })
  },
  { immediate: true }
)

onMounted(() => {
  window.addEventListener('beforeunload', setTabRouterListCache)
  getTabRouterListCache()
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', setTabRouterListCache)
})
</script>
