import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import STYLE_CONFIG from '@/config/style'
import { COLOR_TOKEN, LIGHT_CHART_COLORS, DARK_CHART_COLORS } from '@/config/color'
import type { ColorSeries, ColorToken } from '@/config/color'

export interface IStateType {
  showBreadcrumb: boolean
  mode: string
  layout: string
  isSidebarCompact: boolean
  splitMenu: boolean
  isFooterAside: boolean
  isSidebarFixed: boolean
  isHeaderFixed: boolean
  showHeader: boolean
  showFooter: boolean
  backgroundTheme: string
  brandTheme: string
  isUseTabsRouter: boolean
  isUseMenu: boolean
  showSettingPanel: boolean
  colorList: ColorSeries
  chartColors: ColorToken
}

export const useSettingStore = defineStore('setting', () => {
  // state
  const showBreadcrumb = ref(STYLE_CONFIG.showBreadcrumb)
  const mode = ref(STYLE_CONFIG.mode)
  const layout = ref(STYLE_CONFIG.layout)
  const isSidebarCompact = ref(STYLE_CONFIG.isSidebarCompact)
  const splitMenu = ref(STYLE_CONFIG.splitMenu)
  const isFooterAside = ref(STYLE_CONFIG.isFooterAside)
  const isSidebarFixed = ref(STYLE_CONFIG.isSidebarFixed)
  const isHeaderFixed = ref(STYLE_CONFIG.isHeaderFixed)
  const showHeader = ref(STYLE_CONFIG.showHeader)
  const showFooter = ref(STYLE_CONFIG.showFooter)
  const backgroundTheme = ref(STYLE_CONFIG.backgroundTheme)
  const brandTheme = ref(STYLE_CONFIG.brandTheme)
  const isUseTabsRouter = ref(STYLE_CONFIG.isUseTabsRouter)
  const isUseMenu = ref(STYLE_CONFIG.isUseMenu)
  const showSettingPanel = ref(false)
  const colorList = ref<ColorSeries>(COLOR_TOKEN)
  const chartColors = ref<ColorToken>(LIGHT_CHART_COLORS)

  // getters
  const showSidebar = computed(() => layout.value !== 'top')
  const showSidebarLogo = computed(() => layout.value === 'side')
  const showHeaderLogo = computed(() => layout.value !== 'side')
  const currentMode = computed(() => {
    if (mode.value === 'auto') {
      const media = window.matchMedia('(prefers-color-scheme:dark)')
      if (media.matches) {
        return 'dark'
      }
      return 'light'
    }
    return mode.value
  })

  // actions
  function update(payload: Partial<IStateType>) {
    if (payload.showBreadcrumb !== undefined) showBreadcrumb.value = payload.showBreadcrumb
    if (payload.mode !== undefined) mode.value = payload.mode
    if (payload.layout !== undefined) layout.value = payload.layout
    if (payload.isSidebarCompact !== undefined) isSidebarCompact.value = payload.isSidebarCompact
    if (payload.splitMenu !== undefined) splitMenu.value = payload.splitMenu
    if (payload.isFooterAside !== undefined) isFooterAside.value = payload.isFooterAside
    if (payload.isSidebarFixed !== undefined) isSidebarFixed.value = payload.isSidebarFixed
    if (payload.isHeaderFixed !== undefined) isHeaderFixed.value = payload.isHeaderFixed
    if (payload.showHeader !== undefined) showHeader.value = payload.showHeader
    if (payload.showFooter !== undefined) showFooter.value = payload.showFooter
    if (payload.backgroundTheme !== undefined) backgroundTheme.value = payload.backgroundTheme
    if (payload.brandTheme !== undefined) brandTheme.value = payload.brandTheme
    if (payload.isUseTabsRouter !== undefined) isUseTabsRouter.value = payload.isUseTabsRouter
    if (payload.isUseMenu !== undefined) isUseMenu.value = payload.isUseMenu
  }

  function toggleSidebarCompact() {
    isSidebarCompact.value = !isSidebarCompact.value
  }

  function toggleUseTabsRouter() {
    isUseTabsRouter.value = !isUseTabsRouter.value
  }

  function showSidebarCompactValue(value: boolean) {
    isSidebarCompact.value = value
  }

  function toggleSettingPanel(value: boolean) {
    showSettingPanel.value = value
  }

  function addColor(payload: ColorSeries) {
    colorList.value = { ...colorList.value, ...payload }
  }

  function changeChartColor(payload: ColorToken) {
    chartColors.value = { ...payload }
  }

  function changeTheme(payload: Partial<IStateType>) {
    changeMode(payload)
    changeBrandTheme(payload)
    update(payload)
  }

  function changeMode(payload: Partial<IStateType>) {
    let theme = payload.mode || mode.value
    if (theme === 'auto') {
      const media = window.matchMedia('(prefers-color-scheme:dark)')
      if (media.matches) {
        theme = 'dark'
      } else {
        theme = 'light'
      }
    }
    const isDarkMode = theme === 'dark'

    document.documentElement.setAttribute('theme-mode', isDarkMode ? 'dark' : '')

    changeChartColor(isDarkMode ? DARK_CHART_COLORS : LIGHT_CHART_COLORS)
  }

  function changeBrandTheme(payload: Partial<IStateType>) {
    const { brandTheme: newBrandTheme } = payload
    if (newBrandTheme) {
      document.documentElement.setAttribute('theme-color', newBrandTheme)
    }
  }

  return {
    // state
    showBreadcrumb,
    layout,
    isSidebarCompact,
    splitMenu,
    isFooterAside,
    isSidebarFixed,
    isHeaderFixed,
    showHeader,
    showFooter,
    backgroundTheme,
    brandTheme,
    isUseTabsRouter,
    isUseMenu,
    showSettingPanel,
    colorList,
    chartColors,
    // getters
    showSidebar,
    showSidebarLogo,
    showHeaderLogo,
    mode: currentMode,
    // actions
    update,
    toggleSidebarCompact,
    toggleUseTabsRouter,
    showSidebarCompactValue,
    toggleSettingPanel,
    addColor,
    changeChartColor,
    changeTheme,
    changeMode,
    changeBrandTheme
  }
})

