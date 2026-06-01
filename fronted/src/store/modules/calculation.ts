import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useCalculationStore = defineStore('calculation', () => {
  // 是否应该触发计算
  const shouldCalculate = ref(false)

  // 设置需要计算
  const setShouldCalculate = (value: boolean) => {
    shouldCalculate.value = value
  }

  // 重置状态
  const reset = () => {
    shouldCalculate.value = false
  }

  return {
    shouldCalculate,
    setShouldCalculate,
    reset
  }
})
