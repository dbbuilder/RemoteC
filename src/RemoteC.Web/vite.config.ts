import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 17002,
    host: '0.0.0.0', // Allow access from network
    proxy: {
      '/api': {
        target: 'http://localhost:17001', // Updated to correct port
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'http://localhost:17001', // Updated to correct port
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
  // Handle SPA routing - serve index.html for all routes
  appType: 'spa',
  preview: {
    port: 17002,
    host: '0.0.0.0',
  },
})