import http from './http'

export type CalculationMode = 'speed' | 'balanced' | 'quality'
export type PriorityMode = 'low' | 'middle' | 'high'
export type CalculationModeLike = CalculationMode | PriorityMode | string | null | undefined

export const toPriorityMode = (mode: CalculationMode): PriorityMode => {
  switch (mode) {
    case 'speed':
      return 'low'
    case 'quality':
      return 'high'
    case 'balanced':
    default:
      return 'middle'
  }
}

export const getCalculationModeLabel = (mode: CalculationModeLike) => {
  switch (mode) {
    case 'speed':
    case 'low':
    case 'efficiency':
    case '高效':
      return '计算速度优先'
    case 'balanced':
    case 'middle':
    case '平衡':
      return '平衡模式'
    case 'quality':
    case 'high':
    case 'energy_saving':
    case '节能':
      return '结果质量优先'
    default:
      return mode || '-'
  }
}

export interface CalculateRequest {
  planName: string
  conditionName: string
  remark: string
  targetOutputM3: number
  lpTargetPressure: number
  hpTargetPressure: number
  initialLiquidLevel: number
  startTime: string
  dailyDemandCurve: number[]
  calculationMode: CalculationMode | PriorityMode
}

export interface SaveScheduleRequest {
  createdBy: number
  planName: string
  constraintsJson?: Record<string, unknown>
  approvalComment?: string
  calculateInput: {
    planName: string
    conditionName: string
    remark: string
    targetOutputM3: number
    lpTargetPressure: number
    hpTargetPressure: number
    initialLiquidLevel: number
    startTime: string
    dailyDemandCurve: number[]
    calculationMode: CalculationMode | PriorityMode
  }
  simulationResult: SimulationResult
}

export interface HourlyRecord {
  Hour: number
  Timestamp?: string
  Date?: string
  Time?: string
  Unload_Phase?: string
  Demand_Target_m3h?: number
  Actual_LNG_Output_m3h: number
  BOG_pred_kgph: number
  BOG_to_Recondenser_kgph?: number
  BOG_to_Compressor_kgph?: number
  Compressor_Capacity_kgph?: number
  Comp1_Level: number
  Comp2_Level: number
  Comp3_Level?: number
  Elec_Price: number
  HP_Internal_Flow_m3h?: number
  HP_Flow_perPump_m3h: number
  HP_Num: number
  HP_Recirculation_m3h?: number
  Hourly_Cost_CNY: number
  Hourly_Power_kW: number
  Ideal_HP_Pressure_MPa: number
  Ideal_LP_Pressure_MPa: number
  LP_Internal_Flow_m3h?: number
  LP_Flow_perPump_m3h: number
  LP_Num: number
  LP_Recirculation_m3h?: number
  ORV_Count: number
  SW_Big: number
  SW_Small: number
  Tank_Level_m: number
  Unload_m3h: number
  Line1_kW?: number
  Line2_kW?: number
  Line1_Peak_kW?: number
  Line2_Peak_kW?: number
  Step_Peak_Increment_CNY?: number
  Cumulative_Energy_Cost_CNY?: number
  Cumulative_Peak_Cost_CNY?: number
  Line1_Alarm?: number
  Line2_Alarm?: number
  Event_Note?: string
}

export interface Unitized {
  Comp: number[][]
  HP: number[][]
  LP: number[][]
  ORV: number[][]
  SW_Big: number[][]
  SW_Small: number[][]
}

export interface DeviceLabels {
  Comp: string[]
  HP: string[]
  LP: string[]
  ORV: string[]
  SW_Big: string[]
  SW_Small: string[]
  Tank: string[]
}

export interface TankLevelSeries {
  name: string
  values: number[]
}

export interface TankLevelsPayload {
  hours: number[]
  series: TankLevelSeries[]
}

export interface Bog {
  bog_mech_kgph: number[]
  bog_pred_kgph: number[]
  hours: number[]
  static_bor_kgph?: number[]
  wall_heat_bog_kgph?: number[]
  pump_heat_bog_kgph?: number[]
  piston_bog_kgph?: number[]
  flash_bog_kgph?: number[]
}

export interface Summary {
  estimated_total_electric_cost: number
  excel: string | null
  images: Record<string, any>
  pso_iters: number
  pso_particles: number
  run_pso: boolean
  timestamp: string
}

export interface SimulationResult {
  summary: Summary
  hourly: HourlyRecord[]
  unitized: Unitized
  labels?: DeviceLabels
  tank_levels?: TankLevelsPayload
  bog: Bog
  runHistory: number[] | null
}

export interface CalculateResponse {
  code: number
  message: string
  data?: SimulationResult
}

export interface SaveScheduleResponseData {
  conditionId: number
  planId: number
  planCode: string
  message: string
}

export interface SaveScheduleResponse {
  code: number
  message: string
  data?: SaveScheduleResponseData
}

export const calculate = (data: CalculateRequest) => {
  return http.post<CalculateResponse>('/api/simulation/calculate', data)
}

export const saveSchedule = (data: SaveScheduleRequest) => {
  return http.post<SaveScheduleResponse>('/api/simulation/save', data)
}
