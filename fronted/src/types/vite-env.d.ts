/// <reference types="vite/client" />

declare module 'virtual:svg-icons-register' {
  const content: any
  export default content
}

declare module '@/components/common/index.js' {
  export const baseComponents: Record<string, any>
}

