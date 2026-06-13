import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api':      { target: 'http://localhost:5110', changeOrigin: true, secure: false },
      '/auth':     { target: 'http://localhost:5110', changeOrigin: true, secure: false },
      '/branding': { target: 'http://localhost:5110', changeOrigin: true, secure: false },
      '/uploads':  { target: 'http://localhost:5110', changeOrigin: true, secure: false },
      '/dogs':     { target: 'http://localhost:5110', changeOrigin: true, secure: false,
                     bypass: (req) => req.url && req.url.startsWith('/dogs/') && req.url.match(/\.(jpg|jpeg|png|webp)$/) ? undefined : (req.url?.startsWith('/dogs') && !req.url?.match(/\/dogs\/\d/) ? undefined : req.url) },
      '/expenses': { target: 'http://localhost:5110', changeOrigin: true, secure: false,
                     bypass: (req) => req.url && req.url.match(/\.(jpg|jpeg|png|webp)$/) ? undefined : req.url },
    },
  },
  build: { outDir: 'dist', emptyOutDir: true },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: [],
  },
});
