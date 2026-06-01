import http from './http'

interface ApiResponse<T> {
  code: number
  message: string
  data?: T
}

export interface SchedulePlan {
  planId: number
  planName: string
  planCode: string
  createdBy: number
  createdAt: string
  updatedAt: string
  status: string
  calculationMode: string | null
  totalOutputM3: number | null
  totalPowerKwh: number | null
  totalCostCny: number | null
  optimizationScore: number | null
  fitnessValue: number | null
}

export interface SchedulePlanDetail {
  detailId: number
  hour: number
  equipmentId: number
  equipmentCode: string
  deviceGroup: string
  action: string
  sequenceOrder: number
  delayTimeSec: number
  targetFlowM3h: number | null
  targetLoadPct: number | null
  targetPressureMpa: number | null
}

export interface ScheduleReportSheet {
  sheetName: string
  displayName: string
  columns: string[]
  rows: Record<string, unknown>[]
}

export interface ScheduleListParams {
  status?: string
  pageIndex?: number
  pageSize?: number
}

export interface SchedulePlanListData {
  items: SchedulePlan[]
  totalCount: number
}

export interface SchedulePlanDetailData {
  plan: SchedulePlan
  details: SchedulePlanDetail[]
  reportSheets: ScheduleReportSheet[]
}

export const getSchedulePlans = async (params: ScheduleListParams) => {
  const response = await http.get<ApiResponse<SchedulePlanListData>>('/api/schedules', {
    params
  })
  return response.data
}

export const getSchedulePlanDetail = async (id: number) => {
  const response = await http.get<ApiResponse<SchedulePlanDetailData>>(`/api/schedules/${id}`)
  return response.data
}

export const downloadSchedulePlanCsv = async (id: number) => {
  const response = await http.get(`/api/schedules/${id}/export/csv`, {
    responseType: 'blob'
  })
  return response
}

export const deleteSchedulePlan = async (id: number) => {
  const response = await http.delete<ApiResponse<null>>(`/api/schedules/${id}`)
  return response.data
}
