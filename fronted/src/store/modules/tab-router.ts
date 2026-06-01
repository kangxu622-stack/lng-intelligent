import { defineStore } from 'pinia'
import { ref } from 'vue'

export type TRouterInfo = {
  path: string
  routeIdx: number
  title: string
  name?: string
  isAlive?: boolean
  isHome?: boolean
  query?: any
}

export const useTabRouterStore = defineStore('tabRouter', () => {
  const tabRouterList = ref<TRouterInfo[]>([])
  const isRefreshing = ref(false)
  const homeRoute = ref<TRouterInfo[]>([])

  const ignoreCacheRoutes = ['wellMonitoring']

  function toggleTabRouterAlive(routeIdx: number) {
    isRefreshing.value = !isRefreshing.value
    if (tabRouterList.value[routeIdx]) {
      tabRouterList.value[routeIdx].isAlive = !tabRouterList.value[routeIdx].isAlive
    }
  }

  function appendTabRouterList(newRoute: TRouterInfo) {
    const needAlive = !ignoreCacheRoutes.includes(newRoute.name || '')
    const existingRoute = tabRouterList.value.find((route: TRouterInfo) => route.path === newRoute.path)
    if (!existingRoute) {
      tabRouterList.value = tabRouterList.value.concat({ ...newRoute, isAlive: needAlive })
      return
    }

    existingRoute.title = newRoute.title
    existingRoute.name = newRoute.name
    existingRoute.query = newRoute.query
    existingRoute.isAlive = existingRoute.isAlive ?? needAlive
  }

  function subtractCurrentTabRouter(newRoute: TRouterInfo) {
    const { routeIdx } = newRoute
    tabRouterList.value = tabRouterList.value.slice(0, routeIdx).concat(tabRouterList.value.slice(routeIdx + 1))
  }

  function subtractTabRouterBehind(newRoute: TRouterInfo) {
    const { routeIdx } = newRoute
    tabRouterList.value = tabRouterList.value.slice(0, routeIdx + 1)
  }

  function subtractTabRouterAhead(newRoute: TRouterInfo) {
    const { routeIdx } = newRoute
    tabRouterList.value = homeRoute.value.concat(tabRouterList.value.slice(routeIdx))
  }

  function subtractTabRouterOther(newRoute: TRouterInfo) {
    const { routeIdx } = newRoute
    if (homeRoute.value[0]?.path !== tabRouterList.value?.[routeIdx]?.path) {
      const routeItem = tabRouterList.value?.[routeIdx]
      if (routeItem) {
        tabRouterList.value = homeRoute.value.concat([routeItem])
      }
    } else {
      tabRouterList.value = homeRoute.value
    }
  }

  function removeTabRouterList() {
    tabRouterList.value = homeRoute.value
  }

  function initTabRouterList(newRoute: TRouterInfo[]) {
    tabRouterList.value = newRoute
  }

  function initHomeRoute(route: TRouterInfo) {
    homeRoute.value = [route]
  }

  return {
    tabRouterList,
    isRefreshing,
    homeRoute,
    toggleTabRouterAlive,
    appendTabRouterList,
    subtractCurrentTabRouter,
    subtractTabRouterBehind,
    subtractTabRouterAhead,
    subtractTabRouterOther,
    removeTabRouterList,
    initTabRouterList,
    initHomeRoute
  }
})

