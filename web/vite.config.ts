import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      // Em dev, o Vite roda numa porta própria e o back-end (dotnet run) em outra.
      // Sem esse proxy, chamadas para /api/... cairiam no próprio Vite (404) ou
      // exigiriam uma base URL absoluta + CORS no back. Encaminhando /api para o
      // back local, o front consome tudo com URLs relativas (/api/...) tanto em
      // dev quanto em produção — onde o próprio back serve os estáticos e não há
      // dois servidores para começo de conversa.
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
    },
  },
})
