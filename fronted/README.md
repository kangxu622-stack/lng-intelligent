# LNG调度系统

基于 Vue 3 + TypeScript + Vite 构建的前端项目。

## 技术栈

- **框架**: Vue 3 (Composition API)
- **语言**: TypeScript
- **构建工具**: Vite
- **UI 组件库**: TDesign Vue Next, Element Plus
- **状态管理**: Pinia
- **路由**: Vue Router 4
- **图表**: ECharts, Highcharts

## 快速开始

### 开发环境

```bash
# 安装依赖
npm install

# 启动开发服务器
npm run dev
```

### 构建生产版本

```bash
# 构建
npm run build

# 预览构建结果
npm run preview
```

## 部署文档

查看 **[DEPLOY.md](./DEPLOY.md)** 了解完整部署流程。

### 快速部署

```bash
# 一键部署（推荐）
chmod +x quick-deploy-kylin.sh
./quick-deploy-kylin.sh dev    # 开发环境
./quick-deploy-kylin.sh prod   # 生产环境
```

## 项目结构

```
V1.0/
├── src/                    # 源代码目录
│   ├── components/        # 公共组件
│   ├── layouts/           # 布局组件
│   ├── pages/             # 页面组件
│   ├── stores/            # Pinia 状态管理
│   ├── router/            # 路由配置
│   └── utils/             # 工具函数
├── public/                 # 静态资源
├── dist/                   # 构建输出目录
├── build.sh               # 构建脚本
├── deploy.sh              # 部署脚本
├── start.sh               # 开发服务器启动脚本
└── package.json           # 项目配置
```

## 可用脚本

- `npm run dev` - 启动开发服务器
- `npm run build` - 构建生产版本
- `npm run build:prod` - 构建生产版本（生产模式）
- `npm run preview` - 预览构建结果
- `npm run lint` - 代码检查

## 环境变量

复制 `env.production.example` 为 `.env.production` 并修改相应配置。

## 文档

- [Vue 3 文档](https://vuejs.org/)
- [TypeScript 文档](https://www.typescriptlang.org/)
- [Vite 文档](https://vitejs.dev/)
- [TDesign Vue Next](https://tdesign.tencent.com/vue-next/overview)
- [Element Plus](https://element-plus.org/)

## 许可证

私有项目
