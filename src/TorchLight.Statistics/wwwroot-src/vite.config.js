import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
  base: './',
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@assets': path.resolve(__dirname, './public/assets')
    }
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        entryFileNames: 'js/[name].js',
        chunkFileNames: 'js/[name].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name.endsWith('.css')) {
            return 'css/[name][extname]'
          }
          // SVG、圖片等資源檔案
          if (/\.(svg|png|jpg|jpeg|gif|webp|ico)$/.test(assetInfo.name)) {
            return 'assets/[name][extname]'
          }
          return 'assets/[name][extname]'
        }
      }
    },
    // 設定資源檔案大小限制（小於此大小會被 inline）
    assetsInlineLimit: 4096
  },
  server: {
    port: 5173,
    strictPort: true
  },
  // 設定 public 目錄
  publicDir: 'public'
})
