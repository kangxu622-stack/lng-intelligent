<template>
  <div class="wh100" :class="border ? 'border-style' : ''" :style="{ height: wrapHeight }">
    <div
      class="temp--panel-container"
      :style="{ ...fullScreenStyle, width: width, height: isFullScreen ? 'auto' : height }"
      :class="{ 'light-bg-logo': mode !== 'dark' }"
    >
      <div
        v-if="isTitle"
        class="temp-panel-title"
        :class="{ 'primary-title-bg': type === 'primary' }"
        :style="titleStyle"
      >
        <span v-if="title">{{ title }}</span>
        <span v-else><slot name="title" /></span>
        <div>
          <slot name="extends" />
          <svg-icon
            v-if="fullscreen"
            :icon-class="isFullScreen ? 'no-expand' : 'expand'"
            :style="{
              fill: mode === 'dark' ? '#ffffff' : '#0075e9'
            }"
            class="full-screen"
            @clickIcon="handleScreen"
          />
        </div>
      </div>
      <template v-if="isScrollbar">
        <el-scrollbar style="flex: 1; min-height: 200px">
          <div class="temp-panel-main">
            <slot />
          </div>
        </el-scrollbar>
      </template>
      <template v-else>
        <div style="flex: 1" class="temp-panel-main" :style="mainStyle">
          <slot />
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, useAttrs } from 'vue'
import { useSettingStore } from '@/store/modules/setting'
import SvgIcon from '@/components/svg-icon/index.vue'

interface Props {
  title?: string
  isTitle?: boolean
  fullscreen?: boolean
  type?: 'default' | 'primary' | string
  isScrollbar?: boolean
  width?: string
  // panel 外部容器高度
  wrapHeight?: string
  height?: string
  titleStyle?: Record<string, string | number>
  mainStyle?: Record<string, string | number>
}

const props = withDefaults(defineProps<Props>(), {
  title: '',
  isTitle: false,
  fullscreen: true,
  type: 'default',
  isScrollbar: false,
  width: '100%',
  wrapHeight: '100%',
  height: '100%',
  titleStyle: () => ({}),
  mainStyle: () => ({
    padding: '10px'
  })
})

const emit = defineEmits<{
  (e: 'onFullscreen', isFullScreen: boolean): void
}>()

const settingStore = useSettingStore()
const mode = computed(() => settingStore.mode)

const attrs = useAttrs()
const border = computed(() =>
  Object.prototype.hasOwnProperty.call(attrs, 'border')
)

const fullScreenStyle = ref<Record<string, string> | null>(null)
const isFullScreen = ref(false)

const handleScreen = () => {
  isFullScreen.value = !isFullScreen.value
  if (isFullScreen.value) {
    const headerH = document.querySelector('header') as HTMLElement | null
    const headerHeight = headerH?.offsetHeight ?? 0
    fullScreenStyle.value = {
      position: 'fixed',
      height: 'auto !important',
      margin: '0 !important',
      top: `${headerHeight}px`,
      right: '0',
      bottom: '0',
      left: '0',
      'z-index': '1000',
      'background-color':
        mode.value === 'dark' ? 'var(--td-bg-color-container)' : ''
    }
  } else {
    fullScreenStyle.value = null
  }
  emit('onFullscreen', isFullScreen.value)
}
</script>

<style lang="scss" scoped>
.temp--panel-container {
  position: relative;
  display: flex;
  flex-direction: column;
  border: 1px solid #ddd;
  border-image: linear-gradient(180deg, #2e5b7c, #01aaf2) 3 3;
  box-shadow: unset;
  background: var(--logo-bg) no-repeat top right,
    linear-gradient(360deg, rgba(0, 68, 115, 0.64) 0%, rgba(0, 72, 122, 0.16) 100%);
  .temp-panel-title {
    padding: 5px 10px;
    box-sizing: border-box;
    display: flex;
    font-size: 14px;
    align-items: center;
    position: relative;
    justify-content: space-between;
    .full-screen {
      cursor: pointer;
      color: var(--td-text-color-primary);
    }
  }
  .temp-panel-main {
    padding: 10px;
    box-sizing: border-box;
    flex: 1 !important;
    overflow: hidden;
    // overflow: auto;
  }
}
.dark {
  .temp-panel-title {
    background: linear-gradient(270deg, rgba(0, 202, 255, 0.68) 0%, rgba(0, 150, 255, 0) 100%);
    border-bottom: 1px solid;
    border-image: linear-gradient(180deg, rgba(116, 190, 243, 0), rgba(0, 180, 255, 0.6)) 1 1;
    color: #24deff;
  }
}
.light {
  .temp--panel-container {
    background: #fff !important;
    box-shadow: 0 0 10px 0 rgba(144, 147, 153, 0.3) !important;
    border: 1px solid #fff !important;
    color: #333 !important;
  }
  .temp-panel-title {
    border-bottom: 1px solid #e7e1e1;
    color: var(--light-blue-color);
  }
  .primary-title-bg {
    background: var(--light-blue-color);
    color: #fff;
    border-bottom: none;
    i {
      color: #fff !important;
    }
  }
  .border-style {
    // border: 1px solid #e7e1e1;
    border: 1px solid var(--border-color-2);
  }
}
</style>
