import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { createSvgIconsPlugin } from 'vite-plugin-svg-icons'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    createSvgIconsPlugin({
      iconDirs: [path.resolve(__dirname, 'src/assets/icons/svg')],
      symbolId: "icon-[dir]-[name]"
    })
  ],
  resolve: {
    alias: {
      "~": path.resolve(__dirname, "./"),
      "@": path.resolve(__dirname, "./src")
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:52333',
        changeOrigin: true
      },
      '/hubs': {
        target: 'http://localhost:52333',
        changeOrigin: true,
        ws: true
      }
    }
  },
  css: {
    preprocessorOptions: {
      less: {
        modifyVars: {}
      },
      scss: {
        charset: false
      }
    },
    postcss: {
      plugins: [
        {
          postcssPlugin: "internal:charset-removal",
          AtRule: {
            charset: (atRule: any) => {
              if (atRule.name === "charset") {
                atRule.remove();
              }
            }
          }
        }
      ]
    }
  },
  build: {
    cssCodeSplit: false,
    minify: "terser",
    terserOptions: {
      format: {
        comments: false
      },
      compress: {
        drop_console: true,
        drop_debugger: true
      }
    } as any
  }
})
