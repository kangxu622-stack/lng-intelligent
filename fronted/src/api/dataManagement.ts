import http from './http'

interface ApiResponse<T> {
  code: number
  message: string
  data?: T
}

export interface DataTreeNode {
  key: string
  label: string
  isLeaf: boolean
  tableName?: string | null
  children: DataTreeNode[]
}

export interface DataTableColumn {
  name: string
  label: string
  dataType: string
  isNullable: boolean
  isPrimaryKey: boolean
  isAutoIncrement: boolean
  isForeignKey: boolean
  isPrimaryForeignKey: boolean
  referencedTableName?: string | null
  referencedColumnName?: string | null
  referencedTableDisplayName?: string | null
  defaultValue?: string | null
  comment: string
}

export interface DataTableDefinition {
  tableName: string
  displayName: string
  categoryKey: string
  categoryName: string
  primaryKeyName: string
  columns: DataTableColumn[]
}

export interface DataTablePage {
  definition: DataTableDefinition
  items: Record<string, any>[]
  totalCount: number
}

export interface DataTableQuery {
  pageIndex?: number
  pageSize?: number
  keyword?: string
}

export const getDataTree = async () => {
  const response = await http.get<ApiResponse<DataTreeNode[]>>('/api/data-management/tree')
  return response.data
}

export const getDataTablePage = async (tableName: string, params: DataTableQuery) => {
  const response = await http.get<ApiResponse<DataTablePage>>(`/api/data-management/tables/${tableName}`, {
    params
  })
  return response.data
}

export const createDataRow = async (tableName: string, data: Record<string, any>) => {
  const response = await http.post<ApiResponse<Record<string, any>>>(`/api/data-management/tables/${tableName}`, {
    data
  })
  return response.data
}

export const updateDataRow = async (tableName: string, id: string | number, data: Record<string, any>) => {
  const response = await http.put<ApiResponse<Record<string, any>>>(`/api/data-management/tables/${tableName}/${id}`, {
    data
  })
  return response.data
}

export const deleteDataRow = async (tableName: string, id: string | number) => {
  const response = await http.delete<ApiResponse<null>>(`/api/data-management/tables/${tableName}/${id}`)
  return response.data
}
