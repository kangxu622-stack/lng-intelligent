import http from './http'

interface ApiResponse<T> {
  code: number
  message: string
  data?: T
}

export interface TrendRecord {
  tagName: string
  value: number
  quality: number
  unit: string
  timestamp: string
}

export interface TrendQueryParams {
  tagName: string
  start?: string
  end?: string
  pageIndex?: number
  pageSize?: number
}

export interface TrendListData {
  items: TrendRecord[]
  totalCount: number
}

export const getTrendData = async (params: TrendQueryParams) => {
  const response = await http.get<ApiResponse<TrendListData>>('/api/trends', {
    params
  })
  return response.data
}
