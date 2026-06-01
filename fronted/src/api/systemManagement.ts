import http from './http'

interface ApiResponse<T> {
  code: number
  message: string
  data?: T
}

export interface RoleRecord {
  roleId: number
  roleCode: string
  roleName: string
  isActive: boolean
  userCount: number
}

export interface RoleUpsertInput {
  roleCode: string
  roleName: string
  isActive: boolean
}

export interface UserRecord {
  userId: number
  username: string
  roleId?: number | null
  roleCode?: string | null
  roleName?: string | null
  email?: string | null
  phone?: string | null
  department?: string | null
  isActive: boolean
}

export interface UserUpsertInput {
  username: string
  password?: string
  roleId?: number | null
  email?: string | null
  phone?: string | null
  department?: string | null
  isActive: boolean
}

export const getRoleList = async (params?: { keyword?: string; isActive?: boolean }) => {
  const response = await http.get<ApiResponse<RoleRecord[]>>('/api/system-management/roles', { params })
  return response.data
}

export const createRole = async (data: RoleUpsertInput) => {
  const response = await http.post<ApiResponse<RoleRecord>>('/api/system-management/roles', data)
  return response.data
}

export const updateRole = async (roleId: number, data: RoleUpsertInput) => {
  const response = await http.put<ApiResponse<RoleRecord>>(`/api/system-management/roles/${roleId}`, data)
  return response.data
}

export const deleteRole = async (roleId: number) => {
  const response = await http.delete<ApiResponse<null>>(`/api/system-management/roles/${roleId}`)
  return response.data
}

export const getUserList = async (params?: { username?: string; phone?: string; isActive?: boolean }) => {
  const response = await http.get<ApiResponse<UserRecord[]>>('/api/system-management/users', { params })
  return response.data
}

export const createUser = async (data: UserUpsertInput) => {
  const response = await http.post<ApiResponse<UserRecord>>('/api/system-management/users', data)
  return response.data
}

export const updateUser = async (userId: number, data: UserUpsertInput) => {
  const response = await http.put<ApiResponse<UserRecord>>(`/api/system-management/users/${userId}`, data)
  return response.data
}

export const deleteUser = async (userId: number) => {
  const response = await http.delete<ApiResponse<null>>(`/api/system-management/users/${userId}`)
  return response.data
}
