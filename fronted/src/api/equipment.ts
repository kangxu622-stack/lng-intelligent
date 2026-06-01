import http from './http'

interface ApiResponse<T> {
  code: number
  message: string
  data?: T
}

export interface EquipmentType {
  typeId: number
  typeCode: string
  typeName: string
  expectedCount: number
}

export interface EquipmentRecord {
  equipmentId: number
  equipmentCode: string
  equipmentName: string
  typeId: number
  typeName: string
  parentEquipmentId: number | null
  systemCode: string
  processArea: string
  manufacturer: string
  model: string
  location: string
  status: string
  isControllable: boolean
  installDate: string | null
  commissionedDate: string | null
  remark: string
}

export interface EquipmentListParams {
  typeId?: number
  status?: string
  systemCode?: string
  keyword?: string
  pageIndex?: number
  pageSize?: number
}

export interface EquipmentListData {
  items: EquipmentRecord[]
  totalCount: number
}

export interface EquipmentMonitoringItem {
  equipmentId: number
  equipmentCode: string
  equipmentName: string
  typeCode: string
  typeName: string
  processArea: string
  location: string
  status: string
  isOnline: boolean
  flowRate: number | null
  pressure: number | null
  currentPower: number | null
  temperature: number | null
  liquidLevel: number | null
  updateTime: string | null
  remark: string
  tagBindings: EquipmentMetricTagBinding
}

export interface EquipmentMetricTagBinding {
  flowRateTagName: string | null
  pressureTagName: string | null
  currentPowerTagName: string | null
  temperatureTagName: string | null
  liquidLevelTagName: string | null
  statusTagName: string | null
}

export interface EquipmentMonitoringGroup {
  groupCode: string
  groupName: string
  processArea: string
  items: EquipmentMonitoringItem[]
}

export interface EquipmentMonitoringResponse {
  groups: EquipmentMonitoringGroup[]
}

export interface EquipmentMonitoringTrendPoint {
  timestamp: string
  flowRate: number | null
  pressure: number | null
  currentPower: number | null
  temperature: number | null
  liquidLevel: number | null
  status: string
}

export interface EquipmentMonitoringTrend {
  equipmentId: number
  equipmentCode: string
  equipmentName: string
  items: EquipmentMonitoringTrendPoint[]
}

export const getEquipmentTypes = async () => {
  const response = await http.get<ApiResponse<EquipmentType[]>>('/api/equipment/types')
  return response.data
}

export const getEquipmentList = async (params: EquipmentListParams) => {
  const response = await http.get<ApiResponse<EquipmentListData>>('/api/equipment/list', {
    params
  })
  return response.data
}

export const getEquipmentDetail = async (id: number) => {
  const response = await http.get<ApiResponse<EquipmentRecord>>(`/api/equipment/${id}`)
  return response.data
}

export const getEquipmentMonitoring = async (params?: { typeCode?: string }) => {
  const response = await http.get<ApiResponse<EquipmentMonitoringResponse>>('/api/equipment/monitoring', {
    params
  })
  return response.data
}

export const getEquipmentMonitoringTrend = async (
  id: number,
  params?: {
    start?: string
    end?: string
  }
) => {
  const response = await http.get<ApiResponse<EquipmentMonitoringTrend>>(`/api/equipment/monitoring/${id}/history`, {
    params
  })
  return response.data
}
