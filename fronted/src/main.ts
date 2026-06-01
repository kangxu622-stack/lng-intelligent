import { createApp, defineAsyncComponent } from 'vue'
import App from './App.vue'
import router from './router'
import pinia from './store'

import TDesign from 'tdesign-vue-next'
import 'tdesign-vue-next/es/style/index.css'

import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'

import '@/style/index.less'
import '@/style/tdesgin-global.less'
import '@/style/layout.less' // 放在 tdesgin-global.less 之后，确保样式不被覆盖
import '@/style/custom-global.less'
import '@/style/common-style.less'

import 'virtual:svg-icons-register'
import config from '../package.json'
import { useSettingStore } from './store/modules/setting'
import STYLE_CONFIG from './config/style'
import proxy from './config/host'
import { baseComponents } from '@/components/common/index.js'

const env = import.meta.env.MODE || 'development'

const app = createApp(App)

app.use(pinia)
app.use(router)
app.use(TDesign)
app.use(ElementPlus)

// 全局动态注册 common 目录下的基础组件（cmPanel、pagePanel 等）
Object.keys(baseComponents).forEach(key => {
  // baseComponents 中的值是通过 import.meta.glob 生成的异步组件加载函数
  // 这里使用 defineAsyncComponent 包一层，避免一次性加载所有组件
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  app.component(key, defineAsyncComponent(baseComponents[key] as any))
})

// 初始化主题
const settingStore = useSettingStore()
settingStore.changeTheme({ ...STYLE_CONFIG })

// 设置网页标题
const envConfig = proxy[env as keyof typeof proxy]
document.title = envConfig?.WEB_TAG_NAME || 'LNG接收站智能启停系统'

// 控制台打印当前系统版本号
console.log('当前系统版本：', config.version)

// 移除首次加载动画
const removeLoading = () => {
  const firstLoading = document.querySelector('#first-loading')
  if (firstLoading) {
    firstLoading.remove()
  }
}

// 立即尝试移除
removeLoading()

// 如果还在，等待 DOM 加载完成后移除
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', removeLoading)
} else {
  setTimeout(removeLoading, 100)
}

// 如果还在，等待 Vue 应用挂载后移除
setTimeout(removeLoading, 500)

// 监听iframe消息
window.addEventListener('message', (event) => {
  if (event.data?.type === 'iframeLoaded') {
    const firstLoading = document.querySelector('#first-loading')
    if (firstLoading) {
      setTimeout(() => {
        firstLoading.remove()
      }, 500)
    }
  }
  if (event.data?.type === 'changeTheme') {
    settingStore.changeTheme({ mode: event.data.mode })
  }
}, false)

app.mount('#app')
