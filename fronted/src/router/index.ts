import { createRouter, createWebHashHistory } from 'vue-router'
import Layout from '@/layouts/index.vue'
import { TOKEN_NAME } from '@/config/global'

const myFiles = import.meta.glob('./modules/*.ts', { eager: true })
let modules: any[] = []

Object.keys(myFiles).forEach((file) => {
  const module = (myFiles[file] as any).default
  if (module && Array.isArray(module)) {
    modules = [...modules, ...module]
  }
})

export const asyncRouterList = modules

const defaultRouterList = [
  {
    path: '/',
    redirect: '/login'
  },
  {
    path: '/login',
    meta: { hidden: true, title: '登录' },
    component: () => import('@/pages/Login/index.vue')
  },
  {
    path: '/redirect',
    component: Layout,
    meta: { hidden: true },
    children: [
      {
        path: '/redirect/:path(.*)',
        component: () => import('@/pages/redirect.vue')
      }
    ]
  },
  ...asyncRouterList
]

const router = createRouter({
  history: createWebHashHistory(import.meta.env.BASE_URL || '/'),
  routes: defaultRouterList,
  scrollBehavior() {
    return { top: 0, left: 0 }
  }
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem(TOKEN_NAME)
  const whiteList = ['/login']

  console.log('路由跳转:', from.path, '->', to.path)

  if (token && to.path === '/login') {
    next('/home/overview')
    return
  }

  if (!token && !whiteList.includes(to.path)) {
    next('/login')
    return
  } 

  next()
})

router.afterEach((to) => {
  console.log('路由跳转完成:', to.path)
})

router.onError((error) => {
  const targetPath = router.currentRoute.value.fullPath
  if (targetPath && error.message.includes('Failed to fetch dynamically imported module')) {
    window.location.href = router.resolve(targetPath).href
    window.location.reload()
  }
})

export function resetRouter() {
  defaultRouterList.forEach((route: any) => {
    if (route.name && router.hasRoute(route.name)) {
      router.removeRoute(route.name)
    }
  })

  defaultRouterList.forEach((route: any) => {
    router.addRoute(route)
  })
}

export default router
