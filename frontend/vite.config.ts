import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The dev server proxies /api to the backend so the app works with zero
// CORS/config fuss. (The backend also allows http://localhost:5173 via CORS
// if you prefer calling it directly — set VITE_API_URL.)
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5085',
        changeOrigin: true,
      },
    },
  },
});
