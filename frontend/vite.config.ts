// Configuración de Vite: activa React en el servidor de desarrollo y la compilación.
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { loginTemporal } from './desarrollo/loginTemporal';

// En desarrollo, /api y /hubs se reenvían al gateway (docker compose, puerto 5000).
// Así el navegador ve un solo origen (localhost:5173) y no hace falta CORS.
const gateway = 'http://localhost:5000';
const raizRepositorio = fileURLToPath(new URL('..', import.meta.url));

export default defineConfig({
  // loginTemporal solo corre con `npm run dev` y se quita cuando Identidad tenga su login.
  plugins: [react(), loginTemporal(gateway, raizRepositorio)],
  server: {
    proxy: {
      '/api': gateway,
      '/hubs': { target: gateway, ws: true },
    },
  },
});
