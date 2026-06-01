<template>
  <div>
    <!-- eslint-disable -->
    <template v-for="item in list" :key="item.path">
      <template v-if="!item.children || !item.children.length || item.meta?.single">
        <t-menu-item
          v-if="getHref(item)"
          :key="item.path"
          :href="getHref(item)?.[0]"
          :name="item.path"
          :value="item.meta?.single ? (item.redirect || item.path) : item.path"
        >
          <template #icon>
            <svg-icon v-if="typeof item.icon === 'string' && item.icon && item.icon !== '#'" class="svgIconClass" :icon-class="item.icon" />
            <RenderFnIcon :item="item" />
          </template>
        </t-menu-item>
        <t-menu-item
          v-else-if="item.meta.link"
          :key="item.path"
          :name="item.path"
          :value="item.meta?.single ? (item.redirect || item.path) : item.path"
          :to="item.path"
          @click="changeMenu1(item)"
        >
          <template #icon>
            <span style="color: transparent;">{{ item.meta.hasOwnProperty('single') ? '' : '##' }}</span>
            <svg-icon v-if="typeof item.icon === 'string' && item.icon && item.icon !== '#'" class="svgIconClass" :icon-class="item.icon" />
            <RenderFnIcon :item="item" />
          </template>
          {{ item.title }}
        </t-menu-item>
        <t-menu-item
          v-else
          :key="item.path"
          :name="item.path"
          :value="item.meta?.single ? (item.redirect || item.path) : item.path"
          :to="item.path"
          @click="changeMenu(item)"
        >
          <template #icon>
            <span style="color: transparent;">{{ item.meta.hasOwnProperty('single') ? '' : '##' }}</span>
            <span v-if="item.isThirdRouter" style="color: transparent;">##</span>
            <svg-icon
              v-if="typeof item.icon === 'string' && item.icon && item.icon !== '#'"
              class="svgIconClass"
              :icon-class="item.icon"
            />
            <RenderFnIcon :item="item" />
          </template>
          {{ item.title }}
        </t-menu-item>
      </template>
      <t-submenu
        v-else
        :key="item.path"
        :name="item.path"
        :value="item.redirect || item.path"
        :title="settingStore.layout === 'mix' ? item.title : null"
        @mouseover="onmouseoverRight"
        @mouseleave="onmouseleave"
      >
        <template #icon v-if="settingStore.layout === 'mix'">
          <svg-icon
            v-if="typeof item.icon === 'string' && item.icon && item.icon !== '#'"
            class="svgIconClass"
            :icon-class="item.icon"
          />
          <span v-else style="color: transparent;">##</span>
          <render-fn-icon :item="item" />
        </template>
        <template #title v-else>
          <div @click.stop="handleParentMenuClick(item)" style="display: flex; align-items: center; cursor: pointer; width: 100%;">
            <svg-icon
              v-if="typeof item.icon === 'string' && item.icon && item.icon !== '#'"
              class="svgIconClass"
              :icon-class="item.icon"
            />
            <span v-else style="color: transparent;">##</span>
            <RenderFnIcon :item="item" />
            <span>{{ item.title }}</span>
          </div>
        </template>
        <menu-content
          v-if="item.children && settingStore.isSidebarCompact == false && settingStore.layout === 'mix'"
          :nav-data="item.children"
        />
        <div class="menuselect" v-show="!showSidebar || settingStore.isSidebarCompact">
          <div class="menuTitle">
            <svg-icon
              v-if="typeof item.icon === 'string' && item.icon && item.icon !== '#'"
              class="svgIconClass svgIconTitle"
              :icon-class="item.icon"
            />
            <span>{{ item.title }}</span>
          </div>
          <div class="secondmenu" v-for="items in item.children" :key="items.path">
            <div :class="{ secondTitle: true, secondDefault: items.children.length > 0 }" @click="secondMenu(items)">
              {{ items.title }}
            </div>
            <div style="display: flex; flex-wrap: wrap;">
              <template v-if="items.children">
                <div class="thirdmenu" v-for="itemss in items.children" :key="itemss.path">
                  <router-link :to="{ path: itemss.path }">
                    <div>{{ itemss.title }}</div>
                  </router-link>
                </div>
              </template>
            </div>
          </div>
        </div>
      </t-submenu>
    </template>
    <!-- eslint-disable -->
  </div>
</template>

<script setup lang="ts">
import { computed, watch, onMounted, h, defineComponent } from 'vue'
import type { PropType } from 'vue'
import { useRouter } from 'vue-router'
import { useSettingStore } from '@/store/modules/setting'
import type { MenuRoute } from '@/interface'
import SvgIcon from '@/components/svg-icon/index.vue'

const props = defineProps<{
  navData?: MenuRoute[]
}>()

const router = useRouter()
const settingStore = useSettingStore()

const getMenuList = (list: MenuRoute[] | undefined, basePath?: string): MenuRoute[] => {
  if (!list) {
    return []
  }
  return list
    .map(item => {
      let path
      path = basePath ? `${basePath}/${item.path}` : item.path
      if (path.indexOf('//') !== -1) {
        path = `/${path.split('//')[1]}`
      }
      return {
        path,
        title: item.meta?.title,
        icon: item.meta?.icon || '',
        children: getMenuList(item.children, path),
        meta: item.meta,
        name: item?.name,
        isThirdRouter: item?.isThirdRouter,
        redirect: item.redirect,
        hidden: item.hidden
      }
    })
    .filter(item => item.meta && item.hidden !== true)
}

// RenderFnIcon 缁勪欢
const RenderFnIcon = defineComponent({
  props: {
    item: {
      type: Object as PropType<MenuRoute>,
      required: true
    }
  },
  setup(props) {
    return () => {
      const icon = props.item.icon
      if (typeof icon === 'function') {
        return h(icon, {
          class: 't-icon'
        })
      }
      if (icon && typeof icon === 'object' && icon !== null && 'render' in icon) {
        const iconObj = icon as { render?: () => any }
        if (typeof iconObj.render === 'function') {
          return h(icon as any, {
            class: 't-icon'
          })
        }
      }
      return undefined
    }
  }
})

const list = computed(() => getMenuList(props.navData))

const showSidebar = computed(() => settingStore.showSidebar)

const onmouseoverRight = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  const circle = document.getElementsByClassName('el-carousel__arrow')
  for (let i = 0; i < circle.length; i++) {
    ;(circle[i] as HTMLElement).style.zIndex = '0'
  }
  if (target.className === 't-menu__item  t-is-active') {
    if (showSidebar.value && settingStore.mode === 'dark' && settingStore.isSidebarCompact) {
      const parent = target.parentElement
      if (parent && parent.lastElementChild) {
        ;(parent.lastElementChild as HTMLElement).style.border = '1px solid'
        ;(parent.lastElementChild as HTMLElement).style.borderImage = 'linear-gradient(180deg, rgba(116, 190, 243, 0.5), rgba(0, 180, 255, 1)) 1 1'
      }
    }
  }
  if (target.className === 't-menu__item t-is-opened' || target.className === 't-menu__item') {
    if (showSidebar.value && settingStore.mode === 'dark' && settingStore.isSidebarCompact) {
      const parent = target.parentElement
      if (parent && parent.lastElementChild) {
        ;(parent.lastElementChild as HTMLElement).style.border = '1px solid'
        ;(parent.lastElementChild as HTMLElement).style.borderImage = 'linear-gradient(180deg, rgba(116, 190, 243, 0.5), rgba(0, 180, 255, 1)) 1 1'
      }
    }
    if (settingStore.layout === 'top' && target.tagName !== 'LI') {
      if (target.tagName === 'DIV') {
        const parent = target.parentElement
        if (parent && parent.lastElementChild) {
          ;(parent.lastElementChild as HTMLElement).style.left = `${target.getBoundingClientRect().left - 140}px`
        }
        const b = Number(target.getBoundingClientRect().left) + 146
        if (
          target.className.indexOf('t-menu__item') > -1 &&
          target.parentElement?.parentElement?.className !== 'header-menu'
        ) {
          const nextSibling = target.nextSibling as HTMLElement
          if (nextSibling) {
            nextSibling.style.top = `${target.getBoundingClientRect().top - 7}px`
            nextSibling.style.left = `${b}px`
          }
        }
      }
    }
  }
}

const onmouseleave = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  if (showSidebar.value && settingStore.mode === 'dark' && settingStore.isSidebarCompact) {
    const firstChild = target.firstElementChild
    if (firstChild && firstChild.lastElementChild) {
      const lastChild = firstChild.lastElementChild as HTMLElement
      lastChild.style.borderWidth = ''
      lastChild.style.borderStyle = ''
      lastChild.style.borderImageSource = ''
      lastChild.style.borderImage = ''
    }
  }
  const circle = document.getElementsByClassName('el-carousel__arrow')
  for (let i = 0; i < circle.length; i++) {
    ;(circle[i] as HTMLElement).style.zIndex = ''
  }
}

const changeMenu = (_value: MenuRoute) => {
  // 涓€绾ц彍鍗曟帴鍙ｉ摼鎺?
  // 鏆傛椂娉ㄩ噴鎺夛紝鍥犱负 permission store 涓嶅瓨鍦?
  // if (value.meta?.single && value.children?.[0]) {
  //   settingStore.setRouterLink(value.children[0].meta?.link)
  // }
}

const changeMenu1 = (_value: MenuRoute) => {
  // 浜岀骇鑿滃崟/涓夌骇鑿滃崟鎺ュ彛閾炬帴
  // 鏆傛椂娉ㄩ噴鎺夛紝鍥犱负 permission store 涓嶅瓨鍦?
  // settingStore.setRouterLink(value.meta?.link)
}

const getHref = (item: MenuRoute) => {
  return item.path.match(/(http|https):\/\/([\w.]+\/?)\S*/)
}

const addTip = () => {
  if (settingStore.isSidebarCompact) {
    return
  }
  const list = document.querySelectorAll('.t-menu__content')
  list.forEach(item => {
    if ((item as HTMLElement).scrollWidth > (item as HTMLElement).offsetWidth) {
      item.setAttribute('title', (item as HTMLElement).innerText)
    }
  })
}

const secondMenu = (item: MenuRoute) => {
  if (item.children && item.children.length === 0) {
    router.push({ path: item.path })
  }
}

const handleParentMenuClick = (item: MenuRoute) => {
  const targetPath = item.redirect || item.path
  if (targetPath) {
    router.push({ path: targetPath })
  }
}

watch(
  () => settingStore.isSidebarCompact,
  () => {
    addTip()
  }
)

onMounted(() => {
  addTip()
})
</script>
<style lang="less" scoped>
.svgIconClass {
  width: 2.5em !important;
  height: 1.3em !important;
}

.menuselect {
  padding: 0 12px;
}

.secondmenu {
  margin-bottom: 10px;
  padding-bottom: 6px;
}

.menuselect .secondTitle:nth-last-child(1) {
  border: none;
  margin-bottom: 0;
  padding-bottom: 0;
}

.secondmenu > .secondTitle {
  font-weight: 900;
  line-height: 32px;
  margin-bottom: 5px;
  cursor: pointer;
}

.secondDefault {
  cursor: default !important;
}

.thirdmenu {
  width: 30%;
  max-width: 300px;
  display: flex;
  flex-wrap: wrap;
  margin: 0 6px;
}

.thirdmenu a {
  width: 100%;
  display: block;
  height: 32px;
  font-size: 12px;
  font-family: PingFangSC-Regular, "PingFang SC";
  font-weight: 400;
  line-height: 32px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  margin-right: 0;
}

.t-button-link,
a {
  height: auto;
  display: inline-block;
}

.header-menu::-webkit-scrollbar {
  display: none;
}

:deep(.t-menu__content) {
  max-width: 144px;
}

.menuTitle {
  margin: 10px 0;
  padding-bottom: 10px;
  font-size: 16px;
  font-weight: 600;
}

.svgIconTitle {
  margin-left: -10px;
}
</style>
