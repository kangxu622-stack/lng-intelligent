// 存放不同环境相同配置
const config = {
  PROCESS_TENANT_TYPE: "tenant",
  PROCESS_PULBIC_KEY: "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAKsr9kM6vJHJb8KgWhY6xVaYCkLEr+QKeuYUoZ2uX3+t4lzbxdf1JeJ2jUs26jWWgkpe3W1UHxXUaapyTG9sFbECAwEAAQ==",
  LOGIN_TYPES: ["password", "corp_oauth"],
  MESSAGE_URL: "https://amm.tjioms-dev.tjltd.cnooc/#/appCallback?rediect=reportlnformation/source",
  appId: "f198c1a239254b0e86529a0668cf4adb",
  ALARM_URL: "https://amm.tjioms-dev.tjltd.cnooc/#/reportPolice/source?access_token=",
  WEB_TAG_NAME: "LNG接收站智能启停系统",
  SYSTEM_NAME: "LNG接收站智能启停系统",
  IS_TEST_ENVIRONMENT: false
}

export default {
  development: {
    ...config,
    PREVIEW_FILE_API: "/b/upload",
    appId: 'f198c1a239254b0e86529a0668cf4adb',
    processAPI: "/dev-api/workflow",
    API: "",
    CDN: ""
  },
  test: {
    ...config,
    PREVIEW_FILE_API: "文件服务地址",
    API: "",
    CDN: ""
  },
  stage: {
    ...config,
    processAPI: "",
    API: "",
    CDN: ""
  },
  release: {
    ...config,
    processAPI: "/prod-api/workflow",
    PREVIEW_FILE_API: "文件服务地址",
    API: 'https://www.dev.ideas.cnpc/api/indexpredict',
    appId: 'f198c1a239254b0e86529a0668cf4adb',
    CDN: "",
    LOGIN_TYPES: ["corp_oauth", "password"]
  },
  production: {
    ...config,
    processAPI: "/prod-api/workflow",
    PREVIEW_FILE_API: "文件服务地址",
    API: 'https://www.dev.ideas.cnpc/api/indexpredict',
    appId: 'f198c1a239254b0e86529a0668cf4adb',
    CDN: "",
    LOGIN_TYPES: ["corp_oauth", "password"]
  }
}

