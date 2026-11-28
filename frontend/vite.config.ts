// Configuración de Vite: activa React en el servidor de desarrollo y la compilación.
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// En desarrollo, /api, /hubs y /whatsapp se reenvían al gateway (docker compose, puerto 5000).
// Así el navegador ve un solo origen (localhost:5173) y no hace falta CORS.
const gateway = 'http://localhost:5000';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': gateway,
      '/hubs': { target: gateway, ws: true },
      '/whatsapp': gateway,
    },
  },
});
