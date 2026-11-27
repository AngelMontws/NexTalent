/**
 * NexTalent — loginTemporal (SOLO DESARROLLO, `npm run dev`)
 *
 * Mientras Identidad no tenga POST /auth/login, este plugin responde /api/auth/login con las cuentas de
 * prueba que siembra IdentidadDbInitializer (mismos GUID, correos y contraseña). Firma el JWT con la
 * JWT_KEY del .env de la raíz, así que Postulaciones y el hub lo validan igual que uno real.
 *
 * - Primero intenta el login real por el gateway. Si Identidad ya responde (cualquier código que no sea 404),
 *   se usa esa respuesta y este plugin no hace nada.
 * - No forma parte de `npm run build`: nunca llega a producción.
 * - BORRAR cuando Identidad publique su login.
 */
import { createHmac } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { IncomingMessage, ServerResponse } from 'node:http';
import type { Plugin } from 'vite';

interface Cuenta { sub: string; role: 'Candidato' | 'Reclutador'; empresa_id?: string; }

/** Las cuentas de IdentidadDbInitializer (sin el administrador, que no tiene pantallas). */
const CUENTAS: Record<string, Cuenta> = {
  'candidato1@nextalent.mx': { sub: '33333333-3333-3333-3333-333333333301', role: 'Candidato' },
  'candidato2@nextalent.mx': { sub: '33333333-3333-3333-3333-333333333302', role: 'Candidato' },
  'candidato3@nextalent.mx': { sub: '33333333-3333-3333-3333-333333333303', role: 'Candidato' },
  'candidato4@nextalent.mx': { sub: '33333333-3333-3333-3333-333333333304', role: 'Candidato' },
  'reclutador1@empresa1.mx': { sub: '22222222-2222-2222-2222-222222222201', role: 'Reclutador', empresa_id: '11111111-1111-1111-1111-111111111101' },
  'reclutador2@empresa2.mx': { sub: '22222222-2222-2222-2222-222222222202', role: 'Reclutador', empresa_id: '11111111-1111-1111-1111-111111111102' }
};

/** Lee KEY=valor del .env de la raíz del repositorio. */
function leerEnv(raiz: string): Record<string, string> {
  const ruta = resolve(raiz, '.env');
  if (!existsSync(ruta)) return {};
  return Object.fromEntries(readFileSync(ruta, 'utf8').split(/\r?\n/)
    .map(linea => linea.match(/^\s*([A-Z0-9_]+)\s*=\s*(.*)\s*$/))
    .filter((m): m is RegExpMatchArray => m !== null)
    .map(m => [m[1], m[2].replace(/^["']|["']$/g, '')]));
}

const base64Url = (datos: Buffer | string) => Buffer.from(datos).toString('base64url');

function firmar(cuenta: Cuenta, correo: string, env: Record<string, string>): string {
  const ahora = Math.floor(Date.now() / 1000);
  const encabezado = base64Url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const datos = base64Url(JSON.stringify({
    ...cuenta, email: correo, iss: env.JWT_ISSUER || 'NexTalent.Identidad', aud: env.JWT_AUDIENCE || 'NexTalent.Api',
    iat: ahora, nbf: ahora, exp: ahora + 8 * 3600
  }));
  const firma = createHmac('sha256', env.JWT_KEY).update(`${encabezado}.${datos}`).digest('base64url');
  return `${encabezado}.${datos}.${firma}`;
}

const leerCuerpo = (peticion: IncomingMessage) => new Promise<string>((ok, falla) => {
  let texto = '';
  peticion.on('data', parte => { texto += parte; if (texto.length > 10_000) peticion.destroy(); });
  peticion.on('end', () => ok(texto));
  peticion.on('error', falla);
});

const responder = (respuesta: ServerResponse, estado: number, cuerpo: unknown) => {
  respuesta.statusCode = estado;
  respuesta.setHeader('Content-Type', 'application/json; charset=utf-8');
  respuesta.end(JSON.stringify(cuerpo));
};

export function loginTemporal(gateway: string, raizRepositorio: string): Plugin {
  let avisado = false;
  return {
    name: 'nextalent-login-temporal',
    apply: 'serve',
    configureServer(servidor) {
      servidor.middlewares.use('/api/auth/login', async (peticion, respuesta, siguiente) => {
        if (peticion.method !== 'POST') return siguiente();
        const cuerpo = await leerCuerpo(peticion);

        // 1) Login real de Identidad, si ya existe.
        try {
          const real = await fetch(`${gateway}/api/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: cuerpo });
          if (real.status !== 404) {
            respuesta.statusCode = real.status;
            respuesta.setHeader('Content-Type', real.headers.get('content-type') ?? 'application/json');
            return respuesta.end(await real.text());
          }
        } catch { /* Gateway apagado: se sigue con las cuentas de prueba. */ }

        // 2) Cuentas de prueba.
        const env = leerEnv(raizRepositorio);
        if (!env.JWT_KEY || env.JWT_KEY.length < 32) return responder(respuesta, 500, { title: 'Falta JWT_KEY (32+ caracteres) en el .env de la raíz.' });
        let datos: { email?: string; password?: string } = {};
        try { datos = JSON.parse(cuerpo); } catch { /* Cuerpo vacío o inválido. */ }
        const correo = String(datos.email ?? '').trim().toLowerCase();
        const cuenta = CUENTAS[correo];
        const contrasena = env.SEED_DEFAULT_PASSWORD || 'NexTalent2026!';
        if (!cuenta || datos.password !== contrasena) return responder(respuesta, 401, { title: 'Correo o contraseña incorrectos.' });

        if (!avisado) {
          servidor.config.logger.warn('[login temporal] Identidad aún no tiene /auth/login: se usan las cuentas de prueba.');
          avisado = true;
        }
        return responder(respuesta, 200, { accessToken: firmar(cuenta, correo, env) });
      });
    }
  };
}
