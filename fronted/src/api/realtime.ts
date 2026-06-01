import http from './http'

export interface TagSnapshot {
  tagName: string
  value: number
  timestamp: string
  quality: number
  unit: string
}

export const getRealtimeStatus = async () => {
  const response = await http.get<TagSnapshot[]>('/api/realtime/status', {
    timeout: 10000
  })
  return response.data
}
