import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src')
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
     return 'assets/[name][extname]'
     }
      }
    }
  },
  server: {
    port: 5173,
    strictPort: true
  }
})
