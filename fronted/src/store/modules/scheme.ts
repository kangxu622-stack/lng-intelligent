import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { SimulationResult } from '@/api/simulation'

export interface InitialCondition {
  planName: string
  conditionName: string
  remark: string
  targetOutputM3: number
  lpTargetPressure: number
  hpTargetPressure: number
  initialLiquidLevel: number
  startTime: string
  dailyDemandCurve: number[]
  calculationMode: 'speed' | 'balanced' | 'quality'
}

export interface BogData {
  hours: number[]
  bog_mech_kgph: number[]
  bog_pred_kgph: number[]
}

export const useSchemeStore = defineStore('scheme', () => {
  // State
  const initialCondition = ref<InitialCondition>({
    planName: '',
    conditionName: '',
    remark: '',
    targetOutputM3: 60000,
    lpTargetPressure: 1.20,
    hpTargetPressure: 12.00,
    initialLiquidLevel: 0,
    startTime: '2026-05-06 00:00:00',
    dailyDemandCurve: [],
    calculationMode: 'balanced'
  })

  const simulationResult = ref<SimulationResult | null>(null)
  const bogData = ref<BogData>({
    hours: [],
    bog_mech_kgph: [],
    bog_pred_kgph: []
  })
  const loading = ref(false)

  // Getters
  const hasInitialCondition = computed(() => {
    return !!initialCondition.value.planName && !!initialCondition.value.conditionName
  })

  const schemeParams = computed(() => {
    return {
      planName: initialCondition.value.planName,
      conditionName: initialCondition.value.conditionName,
      remark: initialCondition.value.remark,
      targetOutputM3: initialCondition.value.targetOutputM3,
      lpTargetPressure: initialCondition.value.lpTargetPressure,
      hpTargetPressure: initialCondition.value.hpTargetPressure,
      initialLiquidLevel: initialCondition.value.initialLiquidLevel,
      startTime: initialCondition.value.startTime,
      dailyDemandCurve: initialCondition.value.dailyDemandCurve,
      calculationMode: initialCondition.value.calculationMode
    }
  })

  const hasBogData = computed(() => {
    return bogData.value.hours.length > 0 && bogData.value.bog_mech_kgph.length > 0
  })

  // Actions
  const setInitialCondition = (data: Partial<InitialCondition>) => {
    initialCondition.value = { ...initialCondition.value, ...data }
  }

  const setSimulationResult = (result: SimulationResult | null) => {
    simulationResult.value = result
  }

  const setBogData = (data: BogData) => {
    bogData.value = data
  }

  const clearInitialCondition = () => {
    initialCondition.value = {
      planName: '',
      conditionName: '',
      remark: '',
      targetOutputM3: 60000,
      lpTargetPressure: 1.20,
      hpTargetPressure: 12.00,
      initialLiquidLevel: 0,
      startTime: '2026-05-06 00:00:00',
      dailyDemandCurve: [],
      calculationMode: 'balanced'
    }
    simulationResult.value = null
    bogData.value = { hours: [], bog_mech_kgph: [], bog_pred_kgph: [] }
  }

  return {
    initialCondition,
    simulationResult,
    bogData,
    loading,
    hasInitialCondition,
    hasBogData,
    schemeParams,
    setInitialCondition,
    setSimulationResult,
    setBogData,
    clearInitialCondition
  }
})
