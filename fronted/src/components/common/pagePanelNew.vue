<template>
  <div
    class="panelBox divBox"
    :class="isMax ? 'maxPage' : 'minPage'"
    :style="{
      background: mode === 'dark'
        ? isMax
          ? '#032a3b !important'
          : 'transparent'
        : '#FFFFFF'
    }"
  >
    <div
      style="width: 100%; height: 100%;"
      :class="isMax ? (mode === 'dark' ? 'maxDetail' : '') : ''"
    >
      <div
        v-if="showBtn"
        style="width: 100%; text-align: right;"
        class="maxDivBox"
      >
        <el-tooltip
          class="item"
          effect="dark"
          :content="isMax ? '最小化' : '最大化'"
          placement="bottom"
        >
          <svg-icon
            :icon-class="isMax ? 'no-expand' : 'expand'"
            :style="{ fill: mode === 'dark' ? '#ffffff' : '#0075e9' }"
            class="panelIconClass"
            @clickIcon="maximizeCom"
          />
        </el-tooltip>
      </div>
      <div
        class="g-w100"
        :style="{
          height: showBtn ? 'calc(100%  - 32px)' : '100%',
          padding: showBtn ? '0 20px 20px 20px' : '0px'
        }"
      >
        <slot />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useSettingStore } from '@/store/modules/setting'
import SvgIcon from '@/components/svg-icon/index.vue'

interface Props {
  headerTitle?: string
  showBtn?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  headerTitle: '默认header',
  showBtn: false
})

const emit = defineEmits<{
  (e: 'zoom-out-com', isMax: boolean): void
}>()

const settingStore = useSettingStore()
const mode = computed(() => settingStore.mode)

const isMax = ref(false)

const maximizeCom = () => {
  emit('zoom-out-com', isMax.value)
  isMax.value = !isMax.value
}
</script>

<style scoped>
.panelBox {
  height: calc(100% - 66px);
  display: flex;
  flex-direction: column;
}

.divBox {
  /* background-image: var(--logo-bg) !important; */
  background-size: unset !important;
  background-repeat: no-repeat !important;
  background-position: right top !important;
}

.maxPage {
  position: fixed;
  z-index: 999;
  top: 0;
  left: 0;
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
  /* margin: 10px 0 0; */
}

.panelIconClass {
  width: 16px !important;
  height: 17px !important;
  cursor: pointer;
  color: #fff;
  margin-right: 10px;
  margin-top: 10px;
  fill: currentcolor;
}
</style>


