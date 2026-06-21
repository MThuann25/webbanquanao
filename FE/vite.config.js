import { defineConfig } from 'vite';
import basicSsl from '@vitejs/plugin-basic-ssl';

export default defineConfig({
  plugins: [
    basicSsl()
  ],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5218',
        changeOrigin: true,
        secure: false
      },
      '/chatHub': {
        target: 'http://localhost:5218',
        ws: true,
        secure: false
      }
    }
  }
});
