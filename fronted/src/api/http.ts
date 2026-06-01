import axios from 'axios'

const http = axios.create({
  // 统一用相对路径，方便前后端同域部署；如需网关可在 .env* 配置 VITE_APP_BASE_API
  baseURL: '',
  timeout: 10000
})

export default http

