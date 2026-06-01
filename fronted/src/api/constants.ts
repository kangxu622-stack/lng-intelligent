import http from './http'

export interface ConstantItem {
  param: string
  value: string
  unit: string
}

interface ApiResponse<T> {
  code: number
  message: string
  data: T
}

export const getConstants = async () => {
  const res = await http.get<ApiResponse<ConstantItem[]>>('/api/constants')
  return res.data
}

