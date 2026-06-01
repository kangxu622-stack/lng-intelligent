import http from './http'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  code: number
  message: string
  data: {
    username: string
    message: string
    roleCode: string
    roleId: number
    roleName: string
    userId: number
  }
}


export interface RegisterRequest {
  username: string
  password: string
  email?: string
  phone?: string
  department?: string
  roleCode?: string
}

export interface RegisterResponse {
  userId: number
  username: string
  roleId: number
  roleCode: string
  roleName: string
  message: string
}

export interface LogoutRequest {
  userId?: number
  username?: string
}

export interface LogoutResponse {
  success: boolean
  userId?: number | null
  username?: string | null
  message: string
}

export interface UserInfo {
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

export interface UserInfoResponse {
  code: number
  message: string
  data?: UserInfo
}

export const login = (data: LoginRequest) => {
  return http.post<LoginResponse>('/api/auth/login', data)
}

export const register = (data: RegisterRequest) => {
  return http.post<RegisterResponse>('/api/auth/register', data)
}

export const logout = (data: LogoutRequest) => {
  return http.post<LogoutResponse>('/api/auth/logout', data)
}

export const getUserInfo = (params: { userId?: number; username?: string }) => {
  return http.get<UserInfoResponse>('/api/auth/user-info', { params })
}
