import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Dev server: http://localhost:5173 (sesuai SpaProxyServerUrl di .csproj).
// /api diproxy ke ASP.NET Core http://localhost:5000 agar tanpa CORS saat dev.
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
      // Rute API server (tanpa prefix /api) diproxy ke ASP.NET Core agar tanpa CORS saat dev.
      '^/(todos|users|health)(/.*)?$': {
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
