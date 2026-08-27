import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// El front habla con /api y Vite lo reenvia al Host. Asi el codigo no conoce ninguna IP ni
// puerto, y en produccion sirve el mismo bundle detras de cualquier proxy.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5190,
    proxy: {
      '/api': {
        target: process.env.VITE_DEV_API_TARGET || 'http://localhost:5090',
        changeOrigin: true,
      },
    },
  },
})
