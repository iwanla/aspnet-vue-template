import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Dev server: http://localhost:5173 (sesuai SpaProxyServerUrl di .csproj).
// /api, /scalar, dan /openapi diproxy ke ASP.NET Core http://localhost:5000 agar tanpa CORS saat dev
// dan agar halaman Scalar/OpenAPI bisa dibuka dari origin SPA.
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      // Rute API server (di bawah prefix /api) diproxy ke ASP.NET Core agar dev tanpa CORS.
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      },
      // Dokumentasi API yang hanya di-map saat Development.
      '/scalar': {
        target: 'http://localhost:5000',
        changeOrigin: true
      },
      '/openapi': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  },
  build: {
    // Langsung ke wwwroot server agar `dotnet publish` single-artifact.
    // wwwroot/ di-gitignore (lihat wwwroot/.gitignore).
    outDir: '../AspNetVue.Server/wwwroot',
    emptyOutDir: true
  }
})
