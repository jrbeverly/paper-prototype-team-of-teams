import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  define: { global: 'globalThis' },
  plugins: [vue()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5080',
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
})
