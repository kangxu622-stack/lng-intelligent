<template>
  <div :class="layoutCls">
    <t-head-menu
      :class="menuCls"
      :theme="theme"
      expand-type="popup"
      :value="active"
      style="background: var(--bottom-light); margin-right: 0;"
    >
      <template #logo>
        <span
          v-if="showLogo"
          class="header-logo-container"
          style="font-size: 20px; width: auto; caret-color: transparent; display: flex; align-items: center;"
          @click="goPage"
        >
          <img
            src="@/assets/校徽白色-背景透明版.gif"
            alt="logo"
            class="header-logo-icon"
            style="width: 40px; height: 40px; margin-right: 10px;"
          />
          <span v-if="isTestEnvironment" class="logoText">{{ systemName }}</span>
          <span v-if="!isTestEnvironment" class="logoText">LNG接收站智能启停系统</span>
        </span>
      </template>
      <div v-show="layout !== 'side' && settingStore.layout === 'top' && iconvisible" class="scrollicon" style="padding-right: 10px;">
        <i class="el-icon-arrow-left" @mousedown="scrollrightdown()" @mouseup="scrollleftup()" />
      </div>
      <menu-content
        v-show="layout !== 'side'"
        class="header-menu"
        :nav-data="menu"
      />
      <div v-show="layout !== 'side' && settingStore.layout === 'top' && iconvisible" class="scrollicon" style="padding-left: 10px;">
        <i class="el-icon-arrow-right" @mousedown="scrollleftdown()" @mouseup="scrollleftup()" />
      </div>
      <template #operations>
        <div class="operations-container" style="margin-left: 20px;">
          <span class="header-user-name">{{ currentUserName }}</span>
          <t-tooltip placement="bottom" content="退出登录" style="color: var(--white-color); margin-top: 3px; margin-right: 0;">
            <t-button
              theme="default"
              shape="square"
              variant="text"
              class="header-user-btn"
              style="background: transparent; border: 0; color: #fff;"
              @click="handleLogout"
            >
              退出
            </t-button>
          </t-tooltip>
          <t-tooltip placement="bottom" content="系统设置" style="color: var(--white-color); margin-top: 3px; margin-right: 0;">
            <t-button
              theme="default"
              shape="square"
              variant="text"
              style="background: transparent; border: 0;"
              @click="toggleSettingPanel"
            >
              <svg-icon
                :icon-class="settingStore.mode === 'light' ? 'my-setting-new' : 'my-setting-new-dark'"
                class="panelIconClass"
              />
            </t-button>
          </t-tooltip>
        </div>
      </template>
    </t-head-menu>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useSettingStore } from '@/store/modules/setting'
import { prefix, TOKEN_NAME, USER_NAME, USER_INFO_KEY } from '@/config/global'
import type { MenuRoute } from '@/interface'
import { logout } from '@/api/auth'
import MenuContent from './MenuContent.vue'
import SvgIcon from '@/components/svg-icon/index.vue'
import proxy from '@/config/host'
// import { goNewPage } from '@/api/intelligentOilfield/system/layout.js' // 暂时注释，如果 API 不存在

const env = import.meta.env.MODE || 'development'

const props = defineProps<{
  theme?: string
  layout?: string
  showLogo?: boolean
  menu?: MenuRoute[]
  isFixed?: boolean
  isCompact?: boolean
  maxLevel?: number
}>()

const route = useRoute()
const router = useRouter()
const settingStore = useSettingStore()

const systemName = ref(proxy[env as keyof typeof proxy].SYSTEM_NAME || '')
const isTestEnvironment = ref(proxy[env as keyof typeof proxy].IS_TEST_ENVIRONMENT || false)
const iconvisible = ref(false)
const leftnum = ref(10)
const tid = ref<number | null>(null)
const getUserName = () => {
  const name = localStorage.getItem(USER_NAME)
  if (!name || name === 'undefined' || name === 'null') {
    return '未登录'
  }
  return name
}
const currentUserName = ref(getUserName())
let resizeObserver: ResizeObserver | null = null

const active = computed(() => {
  if (!route.path) {
    return ''
  }
  return route.path
    .split('/')
    .filter((_item, index) => index <= (props.maxLevel || 3) && index > 0)
    .map(item => `/${item}`)
    .join('')
})

const layoutCls = computed(() => [`${prefix}-header-layout`])

const menuCls = computed(() => [
  {
    [`${prefix}-header-menu`]: !props.isFixed,
    [`${prefix}-header-menu-fixed`]: props.isFixed,
    [`${prefix}-header-menu-fixed-side`]: props.layout === 'side' && props.isFixed,
    [`${prefix}-header-menu-fixed-side-compact`]: props.layout === 'side' && props.isFixed && props.isCompact
  }
])

const containerWidth = () => {
  nextTick(() => {
    const menuElement = document.getElementsByClassName('header-menu')[0] as HTMLElement
    if (menuElement) {
      const scrolldom = menuElement.scrollWidth
      const menudom = menuElement.parentElement?.clientWidth || 0
      iconvisible.value = scrolldom > menudom
    }
  })
}

const scrollleftdown = () => {
  const scrollDom = document.getElementsByClassName('header-menu')[0] as HTMLElement
  if (scrollDom) {
    if (leftnum.value < 0) {
      leftnum.value = 10
    }
    if (leftnum.value > 0 && leftnum.value < 1700) {
      tid.value = window.setInterval(() => {
        leftnum.value += 1
        scrollDom.scrollLeft = leftnum.value
      }, 0)
    }
  }
}

const scrollleftup = () => {
  if (tid.value !== null) {
    clearInterval(tid.value)
    tid.value = null
  }
}

const scrollrightdown = () => {
  const scrollDom = document.getElementsByClassName('header-menu')[0] as HTMLElement
  if (scrollDom) {
    if (leftnum.value > 1700) {
      leftnum.value = 1699
    }
    if (leftnum.value > 10 && leftnum.value < 1700) {
      tid.value = window.setInterval(() => {
        leftnum.value -= 1
        scrollDom.scrollLeft = leftnum.value
      }, 0)
    }
  }
}

const toggleSettingPanel = () => {
  settingStore.toggleSettingPanel(true)
}

const handleLogout = async () => {
  const rawUserInfo = localStorage.getItem(USER_INFO_KEY)
  let userInfo: { userId?: number; username?: string } | null = null

  if (rawUserInfo) {
    try {
      userInfo = JSON.parse(rawUserInfo)
    } catch (_error) {
      userInfo = null
    }
  }

  try {
    await logout({
      userId: userInfo?.userId,
      username: userInfo?.username || localStorage.getItem(USER_NAME) || undefined
    })
  } catch (_error) {
    ElMessage.info('已清除本地登录状态')
  } finally {
    localStorage.removeItem(TOKEN_NAME)
    localStorage.removeItem(USER_NAME)
    localStorage.removeItem(USER_INFO_KEY)
    currentUserName.value = '未登录'
    await router.push('/login')
  }

  ElMessage.success('已退出登录')
}

const goPage = () => {
  // 暂时注释 API 调用，直接跳转到首页
  // goNewPage().then((res: any) => {
  //   if (res.data?.data) {
  //     window.location.href = res.data.data
  //   } else {
  //     router.push('/home/overview')
  //   }
  // }).catch(() => {
  //   router.push('/home/overview')
  // })
  router.push('/home/overview')
}

watch(
  () => props.layout,
  (newVal) => {
    if (newVal === 'top') {
      nextTick(() => {
        const node = document.getElementsByClassName('header-menu')[0]
        if (resizeObserver && node) {
          resizeObserver.observe(node)
        }
      })
    } else {
      resizeObserver?.disconnect()
    }
  },
  { immediate: true }
)

onMounted(() => {
  resizeObserver = new ResizeObserver(() => {
    containerWidth()
  })
  if (props.layout === 'top') {
    nextTick(() => {
      const node = document.getElementsByClassName('header-menu')[0]
      if (resizeObserver && node) {
        resizeObserver.observe(node)
      }
    })
  }
  containerWidth()
})

onBeforeUnmount(() => {
  if (resizeObserver) {
    resizeObserver.disconnect()
  }
  scrollleftup()
})
</script>
<style lang="less">
@import "@/style/variables.less";

.header-menu {
  flex: 1 1 1;
  display: inline-flex;
  align-items: center;
  overflow-x: hidden;

  li {
    height: 63px;
    display: flex;
    justify-content: center;
    align-items: center;
  }
}

.operations-container {
  display: flex;
  align-items: center;
  margin-right: 0;

  .header-user-name {
    color: #fff;
    font-size: 14px;
    margin-right: 8px;
    white-space: nowrap;
  }

  .t-popup__reference {
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .t-button {
    margin: 0 8px;

    &.header-user-btn {
      margin: 0;
      background: transparent;
      border: 0;
    }
  }

  .t-icon {
    font-size: 20px;

    &.general {
      margin-right: 16px;
    }
  }
}

.header-operate-left {
  display: flex;
  margin-left: -20px;
  align-items: normal;
  line-height: 0;

  .collapsed-icon {
    font-size: 20px;
  }
}

.header-logo-container {
  width: 184px;
  height: 55px;
  align-items: center;
  display: flex;
  margin-left: 24px;
  color: var(--white-color);

  .logoText {
    width: auto;
    height: 28px;
    font-size: 20px;
    font-family: PingFangSC-Semibold, "PingFang SC";
    font-weight: 600;
    color: #fff;
    line-height: 28px;
    text-align: center;
  }

  .t-logo {
    width: 100%;
    height: 100%;

    &:hover {
      cursor: pointer;
    }
  }

  &:hover {
    cursor: pointer;
  }
}

.header-user-account {
  display: inline-flex;
  align-items: center;
  color: var(--td-text-color-primary);
  font-size: 12px;
  font-family: PingFangSC-Medium, "PingFang SC";
  font-weight: 500;

  .t-icon {
    margin-left: 4px;
    font-size: 16px;
  }
}

.t-head-menu__inner {
  border-bottom: 1px solid var(--td-border-level-1-color);
  height: 60px;
}

.t-menu--light {
  .header-user-account {
    color: var(--td-text-color-primary);
  }
}

.t-menu--dark {
  .t-head-menu__inner {
    border-bottom: 1px solid var(--td-gray-color-10);
    height: 60px;
  }

  .header-user-account {
    color: rgba(255, 255, 255, 0.55);
  }

  .t-button {
    --ripple-color: var(--td-gray-color-10) !important;

    &:hover {
      background: transparent !important;
    }
  }
}

.operations-dropdown-container-item {
  width: 100%;
  display: flex;
  align-items: center;

  .t-icon {
    margin-right: 8px;
  }

  .t-dropdown__item {
    width: 100%;
    margin-bottom: 0;

    .t-dropdown__item__content {
      display: flex;
      justify-content: center;
    }

    .t-dropdown__item__content__text {
      display: flex;
      align-items: center;
      font-size: 14px;
    }
  }

  &:last-child {
    .t-dropdown__item {
      margin-bottom: 8px;
    }
  }
}
</style>
<style scoped>
.panelIconClass {
  width: 24px !important;
  height: 24px !important;
  cursor: pointer;
  color: #fff;
}

.operations-container .t-button {
  margin: 0 5px;
}

.scrollicon {
  display: flex;
  justify-content: center;
  align-items: center;
}

.searchStyle {
  position: absolute;
  top: 10px;
  right: 8px;
  color: var(--light-blue-color);
  font-weight: 700;
  font-size: 18px;
  cursor: pointer;
  z-index: 1;
}

.el-dialog__body .el-row {
  display: flex;
  justify-content: space-between;
}

.contentDiv {
  width: 100%;
  height: 100%;
}

.blueDiv {
  width: 100%;
  height: 610px;
  padding: 23px 24px;
  overflow: scroll;
}

.blueText {
  font-size: 16px;
  font-family: PingFangSC-Medium, "PingFang SC";
  font-weight: 500;
  color: var(--light-blue-color);
}

.blueLine {
  width: 5px;
  height: 16px;
  background: var(--light-blue-color);
  margin-right: 5px;
}

.contentText {
  font-size: 14px;
  font-family: PingFangSC-Regular, "PingFang SC";
  font-weight: 400;
  color: #606266;
}

.contentMargin {
  margin-left: 20px;
}

.stepsDiv {
  height: 64px;
}
</style>
