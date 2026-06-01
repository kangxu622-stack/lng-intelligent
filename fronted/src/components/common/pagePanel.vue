<template>
  <div
    ref="panelRef"
    class="g-w100 panelBox"
    :class="isMax ? 'maxPage' : 'minPage'"
    :style="{
      background: mode === 'dark'
        ? isMax
          ? '#032a3b !important'
          : 'transparent'
        : '#FFFFFF'
    }"
  >
    <!-- <div v-if="isMax" class="t-layout g-w100 g-h100 posBg" /> -->
    <div
      class="headerStyle g-row-flex-V"
      :style="{
        background:
          mode === 'dark'
            ? 'linear-gradient(to right, rgba(2,30,54,0.8), #017AA3)'
            : '#FDFDFD',
        color: mode === 'dark' ? '#fff' : '#333',
        borderBottom:
          mode === 'dark'
            ? '1px solid  #2D5E81'
            : '1px solid  #2D5E8133'
      }"
    >
      <div class="panelTitle">
        {{ headerTitle }}
      </div>
      <div v-if="showBtn" class="panelActions">
        <el-tooltip
          class="item"
          effect="dark"
          :content="isMax ? '退出全屏' : '全屏'"
          placement="bottom"
        >
          <svg-icon
            :icon-class="isMax ? 'exit-fullscreen' : 'fullscreen'"
            :style="{
              fill: mode === 'dark' ? '#ffffff' : '#0075e9'
            }"
            class="panelIconClass fullscreenIconClass"
            @clickIcon="maximizeCom"
          />
        </el-tooltip>
        <el-tooltip class="item" effect="dark" content="下载图片" placement="bottom">
          <svg-icon
            icon-class="download"
            :style="{
              fill: mode === 'dark' ? '#ffffff' : '#0075e9'
            }"
            class="panelIconClass downloadIconClass"
            @clickIcon="downloadPanelImage"
          />
        </el-tooltip>
      </div>
    </div>
    <div
      class="g-w100 panelBody"
      :class="isMax ? (mode === 'dark' ? 'maxDetail' : '') : ''"
      style="height: calc(100% - 32px);"
    >
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import * as echarts from 'echarts'
import html2canvas from 'html2canvas'
import { ElMessage } from 'element-plus'
import { ref, computed } from 'vue'
import { useSettingStore } from '@/store/modules/setting'
import SvgIcon from '@/components/svg-icon/index.vue'

interface Props {
  headerTitle?: string
  showBtn?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  headerTitle: '默认header',
  showBtn: true
})

const emit = defineEmits<{
  (e: 'zoom-out-com', isMax: boolean): void
}>()

const settingStore = useSettingStore()
const mode = computed(() => settingStore.mode)

const isMax = ref(false)
const panelRef = ref<HTMLElement | null>(null)

const maximizeCom = () => {
  emit('zoom-out-com', isMax.value)
  isMax.value = !isMax.value
}

const buildFileName = () => {
  const fallback = 'panel'
  const title = (props.headerTitle || fallback).trim() || fallback
  return `${title.replace(/[\\/:*?"<>|]+/g, '_')}.png`
}

const triggerDownload = (url: string) => {
  const link = document.createElement('a')
  link.href = url
  link.download = buildFileName()
  link.click()
}

const svgToPngDataUrl = (svgElement: SVGElement, width: number, height: number) => {
  return new Promise<string>((resolve, reject) => {
    const serializer = new XMLSerializer()
    const svgString = serializer.serializeToString(svgElement)
    const svgBlob = new Blob([svgString], { type: 'image/svg+xml;charset=utf-8' })
    const svgUrl = URL.createObjectURL(svgBlob)
    const image = new Image()

    image.onload = () => {
      try {
        const canvas = document.createElement('canvas')
        const ratio = Math.max(2, window.devicePixelRatio || 1)
        canvas.width = Math.max(1, Math.round(width * ratio))
        canvas.height = Math.max(1, Math.round(height * ratio))
        const context = canvas.getContext('2d')

        if (!context) {
          URL.revokeObjectURL(svgUrl)
          reject(new Error('canvas context unavailable'))
          return
        }

        context.scale(ratio, ratio)
        context.clearRect(0, 0, width, height)
        context.drawImage(image, 0, 0, width, height)
        URL.revokeObjectURL(svgUrl)
        resolve(canvas.toDataURL('image/png'))
      } catch (error) {
        URL.revokeObjectURL(svgUrl)
        reject(error)
      }
    }

    image.onerror = () => {
      URL.revokeObjectURL(svgUrl)
      reject(new Error('failed to load svg image'))
    }

    image.src = svgUrl
  })
}

const downloadByEchartsInstance = async () => {
  if (!panelRef.value) return false
  const chartDom = panelRef.value.querySelector('[data-zr-dom-id]')?.parentElement
    || panelRef.value.querySelector('[_echarts_instance_]')

  if (!chartDom) {
    return false
  }

  const chart = echarts.getInstanceByDom(chartDom as HTMLElement)
  if (!chart) {
    return false
  }

  const svgElement = chartDom.querySelector('svg')
  if (svgElement) {
    const dataUrl = await svgToPngDataUrl(svgElement as SVGElement, chart.getWidth(), chart.getHeight())
    triggerDownload(dataUrl)
    return true
  }

  const dataUrl = chart.getDataURL({
    type: 'png',
    pixelRatio: Math.max(2, window.devicePixelRatio || 1),
    backgroundColor: 'transparent'
  })
  triggerDownload(dataUrl)
  return true
}

const downloadPanelImage = async () => {
  if (!panelRef.value) return
  try {
    if (await downloadByEchartsInstance()) {
      return
    }
    const canvas = await html2canvas(panelRef.value, {
      useCORS: true,
      backgroundColor: null,
      scale: Math.max(2, window.devicePixelRatio || 1)
    })
    triggerDownload(canvas.toDataURL('image/png'))
  } catch (error) {
    console.error('download panel image failed', error)
    ElMessage.error('下载图片失败')
  }
}
</script>

<style scoped>
.posBg {
  position: absolute;
  z-index: -1;
  top: 0;
  left: 0;
}

.maxPage {
  position: fixed !important;
  z-index: 999 !important;
  top: 0 !important;
  left: 0 !important;
  width: 100% !important;
  height: 100% !important;
  margin: 0;
}

.maxDetail {
  background-image: linear-gradient(
      360deg,
      rgba(0, 68, 115, 0.64) 0%,
      rgba(0, 72, 122, 0.16) 100%
    ),
    url('/src/assets/backgroundImg.png');
  background-repeat: no-repeat;
  background-position: center;
  background-size: cover;
}

.minPage {
  position: relative;
  margin: 0px;
}

.panelTitle {
  width: 70%;
}

.panelActions {
  width: 30%;
  display: flex;
  align-items: center;
  justify-content: flex-end;
}

.panelBody {
  overflow: auto;
  scrollbar-width: thin;
  scrollbar-color: rgba(41, 180, 255, 0.55) rgba(7, 29, 53, 0.18);
}

.panelBody::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

.panelBody::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(41, 180, 255, 0.55);
}

.panelBody::-webkit-scrollbar-track {
  background: rgba(7, 29, 53, 0.18);
}

.panelIconClass {
  cursor: pointer;
  color: #fff;
  margin-left: 14px;
  fill: currentcolor;
}

.fullscreenIconClass {
  width: 18px !important;
  height: 18px !important;
}

.downloadIconClass {
  width: 20px !important;
  height: 20px !important;
}
</style>
