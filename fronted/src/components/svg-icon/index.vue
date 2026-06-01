<template>
  <svg class="svg-icon" aria-hidden="true" @click="clickIcon">
    <use :xlink:href="iconName" />
  </svg>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  iconClass: string
  className?: string
}>()

const emit = defineEmits<{
  clickIcon: []
}>()

const iconName = computed(() => `#icon-${props.iconClass}`)

// const svgClass = computed(() => {
//   if (props.className) {
//     return `svg-icon ${props.className}`
//   }
//   return 'svg-icon'
// })

const clickIcon = () => {
  emit('clickIcon')
}
</script>
<style scoped>
.svg-icon {
  width: 1em;
  height: 1em;
  vertical-align: -0.15em;
  overflow: hidden;
  fill: currentcolor;
}
</style>

