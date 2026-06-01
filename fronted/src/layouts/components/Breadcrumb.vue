<template>
  <t-breadcrumb :max-item-width="'150'" class="tdesign-breadcrumb">
    <t-breadcrumb-item v-for="item in crumbs" :key="item.to" :to="item.to">
      {{ item.title }}
    </t-breadcrumb-item>
  </t-breadcrumb>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

interface BreadcrumbItem {
  path: string
  to: string
  title: string
}

const crumbs = computed((): BreadcrumbItem[] => {
  const pathArray = route.path.split('/')
  pathArray.shift()

  const breadcrumbs = pathArray.reduce((breadcrumbArray: BreadcrumbItem[], path: string, idx: number) => {
    breadcrumbArray.push({
      path,
      to: breadcrumbArray[idx - 1] ? `/${breadcrumbArray[idx - 1]?.path}/${path}` : `/${path}`,
      title: (route.matched[idx]?.meta?.title as string) || path
    })
    return breadcrumbArray
  }, [])
  return breadcrumbs
})
</script>
<style scoped>
.tdesign-breadcrumb {
  margin-bottom: 8px;
}
</style>
