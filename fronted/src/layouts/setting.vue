<template>
  <div>
    <t-drawer
      size="408px"
      :footer="false"
      v-model:visible="showSettingPanel"
      header="设置"
      :close-btn="true"
      class="setting-drawer-container"
    >
      <div class="setting-container">
        <t-form
          ref="formRef"
          :data="formData"
          size="large"
          label-align="left"
          @reset="onReset"
          @submit="onSubmit"
        >
          <el-collapse v-model="activeNames">
            <el-collapse-item name="1">
              <template #title>
                <svg-icon
                  :icon-class="settingStore.mode === 'dark' ? 'page-setting' : 'page-setting2'"
                  class="panelIconClass"
                />页面设置
              </template>
              <div class="setting-group-title">
                主题模式
              </div>
              <t-radio-group v-model="formData.mode">
                <div v-for="(item, index) in MODE_OPTIONS" :key="index" class="setting-layout-drawer">
                  <div>
                    <t-radio-button
                      :key="index"
                      :value="item.type"
                    >
                      <!-- <component
                        :is="getModeIcon(item.type)"
                      /> -->
                    </t-radio-button>
                    <p :style="{ textAlign: 'center', marginTop: '8px' }">
                      {{ item.text }}
                    </p>
                  </div>
                </div>
              </t-radio-group>
              <div>
                <div class="setting-group-title">
                  导航布局
                </div>

                <t-radio-group v-model="formData.layout">
                  <div v-for="(item, index) in LAYOUT_OPTION" :key="index" class="setting-layout-drawer">
                    <t-radio-button :key="index" :value="item">
                      <!-- <thumbnail :src="getThumbnailUrl(item)" /> -->
                    </t-radio-button>
                  </div>
                </t-radio-group>
                <div class="setting-group-title">
                  元素开关
                </div>
                <t-form-item v-show="formData.layout === 'side'" label="显示 Header" name="showHeader">
                  <t-switch v-model="formData.showHeader" />
                </t-form-item>
                <t-form-item label="显示 Footer" name="showFooter">
                  <t-switch v-model="formData.showFooter" />
                </t-form-item>
                <t-form-item v-show="formData.layout == 'mix'" label="显示左侧菜单栏" name="isUseMenu">
                  <t-switch v-model="formData.isUseMenu" @change="isUsemenu" />
                </t-form-item>
                <t-form-item label="使用 多标签Tab页" name="isUseTabsRouter">
                  <t-switch v-model="formData.isUseTabsRouter" />
                </t-form-item>
                <t-form-item
                  v-show="formData.showFooter && !formData.isSidebarFixed"
                  label="footer 内收"
                  name="footerPosition"
                >
                  <t-switch v-model="formData.isFooterAside" />
                </t-form-item>
              </div>
            </el-collapse-item>
          </el-collapse>
          <div class="settingBtn" @click="editPanel()">
            <svg-icon
              :icon-class="settingStore.mode === 'dark' ? 'edit-panels' : 'edit-panels2'"
              class="panelIconClass"
            />
            <p>编辑面板</p>
          </div>
        </t-form>
      </div>
    </t-drawer>
  </div>
</template>
<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useSettingStore } from '@/store/modules/setting'
import STYLE_CONFIG from '@/config/style'
// import { insertThemeStylesheet, generateColorMap } from '@/config/color' // 暂时未使用
// import type { ColorSeries, ColorToken } from '@/config/color' // 暂时未使用
// @ts-ignore - tvision-color 没有类型定义
// import { Color } from 'tvision-color' // 暂时未使用
// import Thumbnail from '@/components/intelligentOilfield/thumbnail/index.vue'
// import SvgIcon from '@/components/svg-icon/index.vue'
// import SettingDarkIcon from '@/assets/assets-setting-dark.svg'
// import SettingLightIcon from '@/assets/assets-setting-light.svg'
// import SettingAutoIcon from '@/assets/assets-setting-auto.svg'
// import { setpageConfig } from '@/api/intelligentOilfield/system/user' // 移除了登录相关API

const settingStore = useSettingStore()

const showSettingPanel = computed({
  get: () => settingStore.showSettingPanel,
  set: (val) => settingStore.toggleSettingPanel(val)
})

// const formRef = ref() // 暂时未使用
const activeNames = ref(['1'])

const LAYOUT_OPTION = ['top', 'mix']
const MODE_OPTIONS = [
  { type: 'light', text: '明亮' },
  { type: 'dark', text: '暗黑' }
]

const formData = ref({ ...STYLE_CONFIG })

watch(
  () => formData.value,
  (newVal) => {
    const { isSidebarCompact } = settingStore
    settingStore.changeTheme({ ...newVal, isSidebarCompact })
    // setpageConfigs() // 移除了登录相关API调用
  },
  { deep: true }
)

onMounted(() => {
  document.querySelector('.dynamic-color-btn')?.addEventListener('click', () => {
    // isColoPickerDisplay.value = true // 移除了 isColoPickerDisplay
  })
})

const onReset = (): void => {
  formData.value = {
    ...STYLE_CONFIG
  }
  // ElMessage.success('已恢复初始设置') // 移除了 Element Plus 的消息提示
}

const onSubmit = ({ result, e }: { result: boolean; firstError?: string; e: Event }): void => {
  e.preventDefault()
  if (result === true) {
    // visible.value = false // 移除了 visible
  } else {
    // ElMessage.warning(firstError) // 移除了 Element Plus 的消息提示
  }
}

// const getModeIcon = (mode: string) => {
//   if (mode === 'light') {
//     return SettingLightIcon
//   }
//   if (mode === 'dark') {
//     return SettingDarkIcon
//   }
//   return SettingAutoIcon
// }

// const getThumbnailUrl = (name: string) => {
//   return new URL(`../assets/intelligentOilfield/${name}.png`, import.meta.url).href
// }

// const handleCloseDrawer = (): void => {
//   settingStore.toggleSettingPanel(false)
// } // 暂时未使用，drawer 使用 v-model:visible

// 移除了 setpageConfigs 方法，因为它依赖登录相关API
// const setpageConfigs = (): void => {
//   const data = {
//     brandTheme: formData.value.brandTheme,
//     isHeaderFixed: formData.value.isHeaderFixed,
//     isSidebarFixed: formData.value.isSidebarFixed,
//     isUseMenu: formData.value.isUseMenu,
//     isUseTabsRouter: formData.value.isUseTabsRouter,
//     layout: formData.value.layout,
//     mode: formData.value.mode,
//     showBreadcrumb: formData.value.showBreadcrumb,
//     showFooter: formData.value.showFooter,
//     splitMenu: formData.value.splitMenu,
//     userId: settingStore.userId // 假设 userId 从 store 获取
//   }
//   setpageConfig(data).then(() => {})
// }

// const changeColor = (hex: string) => {
//   const { mode } = settingStore

//   const newPalette = Color.getPaletteByGradation({
//     colors: [hex],
//     step: 10
//   })[0]
//   const colorMap = generateColorMap(hex, newPalette, mode as 'light' | 'dark')

//   settingStore.addColor({ [hex]: colorMap } as ColorSeries)

//   insertThemeStylesheet(hex, colorMap as ColorToken, mode as 'light' | 'dark')

//   settingStore.changeTheme({ ...formData.value, brandTheme: hex })
// } // 暂时未使用

const isUsemenu = () => {
  settingStore.toggleSidebarCompact()
}

const editPanel = () => {
  // this.$bus.$emit('emitBus') // 移除了事件总线
  settingStore.toggleSettingPanel(false)
}
</script>

<style lang="less">
@import "@/style/variables.less";

.tdesign-setting {
  z-index: 100;
  position: fixed;
  bottom: 200px;
  right: 0;
  height: 40px;
  width: 40px;
  border-radius: 20px 0 0 20px;
  transition: all 0.3s;

  .t-icon {
    margin-left: 8px;
  }

  .tdesign-setting-text {
    font-size: 12px;
    display: none;
  }

  &:hover {
    width: 96px;

    .tdesign-setting-text {
      display: inline-block;
    }
  }
}

.setting-layout-color-group {
  display: inline-flex;
  justify-content: center;
  align-items: center;
  border-radius: 50% !important;
  padding: 6px !important;
  border: 2px solid transparent !important;

  > .t-radio-button__label {
    display: inline-flex;
  }
}

.tdesign-setting-close {
  position: fixed;
  bottom: 200px;
  right: 300px;
}

.setting-group-title {
  font-size: 14px;
  line-height: 22px;
  margin: 32px 0 24px;
  text-align: left;
  font-family: "PingFang SC";
  font-style: normal;
  font-weight: 500;
  color: var(--td-text-color-primary);
}

.setting-group-color {
  position: relative;

  > div {
    position: absolute;
    z-index: 2;
    right: 0;
  }
}

.setting-link {
  cursor: pointer;
  color: var(--td-brand-color);
  margin-bottom: 8px;
}

.setting-info {
  position: absolute;
  padding: 24px;
  bottom: 0;
  left: 0;
  line-height: 20px;
  font-size: 12px;
  text-align: center;
  color: var(--td-text-color-placeholder);
  width: 100%;
}

.setting-drawer-container {
  .setting-container {
    padding-bottom: 100px;
  }

  .t-radio-group.t-size-m {
    min-height: 32px;
    width: 100%;
    height: auto;
    justify-content: space-around;
    align-items: center;
  }

  .setting-layout-drawer {
    display: flex;
    flex-direction: column;
    align-items: center;
    margin-bottom: 16px;

    .t-radio-button {
      display: inline-flex;
      max-height: 78px;
      padding: 8px;
      border-radius: @border-radius;
      border: 2px solid rgba(100, 90, 90, 0.5);
      height: auto;

      > .t-radio-button__label {
        display: inline-flex;
      }
    }

    .t-is-checked {
      border: 2px solid var(--td-brand-color) !important;
    }

    .t-form__controls-content {
      justify-content: flex-end;
    }
  }

  .t-form__controls-content {
    justify-content: flex-end;
  }
}

.setting-route-theme {
  .t-form__label {
    min-width: 310px !important;
    color: var(--td-text-color-secondary);
  }
}

.setting-color-theme {
  .setting-layout-drawer {
    .t-radio-button {
      height: 32px;
    }

    &:last-child {
      margin-right: 0;
    }
  }
}

.settingBtn {
  height: 50px;
  line-height: 50px;
  cursor: pointer;
  font-size: 16px;
  font-weight: 400;
  display: flex;
  align-items: center;
}

.el-collapse-item__content {
  font-size: 14px;
}

.el-collapse {
  .t-form__item {
    margin-left: 30px;
  }
}
</style>
