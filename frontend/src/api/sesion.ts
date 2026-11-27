/**
 * NexTalent — sesion
 * Guarda el JWT de Identidad y lee sus datos (sub, role, empresa_id, exp).
 * Se usa sessionStorage: la sesión se cierra al cerrar la pestaña y el token no queda guardado para siempre.
 */
import type { RolUsuario } from '../tipos';

/** Datos de la persona que inició sesión, tal como vienen en el token. */
export interface Sesion {
  token: string;
  idUsuario: string;
  rol: RolUsuario;
  idEmpresa: string | null;
  correo: string;
  /** Fecha de vencimiento en milisegundos. */
  expira: number;
}

const CLAVE_TOKEN = 'nextalent-token';

/** Decodifica la parte central del JWT. No valida la firma: eso lo hace el backend en cada petición. */
function leerDatosToken(token: string): Record<string, unknown> | null {
  try {
    const parte = token.split('.')[1];
    const base64 = parte.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(parte.length / 4) * 4, '=');
    const bytes = Uint8Array.from(atob(base64), letra => letra.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes));
  } catch {
    return null;
  }
}

/** Convierte un token en sesión. Devuelve null si está vencido o si el rol no tiene pantallas. */
export function crearSesion(token: string, correo = ''): Sesion | null {
  const datos = leerDatosToken(token);
  if (!datos || typeof datos.sub !== 'string' || typeof datos.exp !== 'number') return null;
  const rol = String(datos.role ?? '').toLowerCase();
  if (rol !== 'candidato' && rol !== 'reclutador') return null;
  const expira = datos.exp * 1000;
  if (expira <= Date.now()) return null;
  return {
    token, idUsuario: datos.sub, rol, expira,
    idEmpresa: typeof datos.empresa_id === 'string' ? datos.empresa_id : null,
    correo: typeof datos.email === 'string' ? datos.email : correo
  };
}

/** Recupera la sesión de esta pestaña, si sigue vigente. */
export function leerSesionGuardada(): Sesion | null {
  try {
    const guardado = sessionStorage.getItem(CLAVE_TOKEN);
    if (!guardado) return null;
    const { token, correo } = JSON.parse(guardado) as { token: string; correo: string };
    const sesion = crearSesion(token, correo);
    if (!sesion) sessionStorage.removeItem(CLAVE_TOKEN);
    return sesion;
  } catch {
    return null;
  }
}

export function guardarSesion(sesion: Sesion) {
  try { sessionStorage.setItem(CLAVE_TOKEN, JSON.stringify({ token: sesion.token, correo: sesion.correo })); } catch { /* Sin almacenamiento: la sesión dura hasta recargar. */ }
}

export function borrarSesion() {
  try { sessionStorage.removeItem(CLAVE_TOKEN); } catch { /* Nada que borrar. */ }
}

/** Token vigente para las peticiones, o null. */
export const leerToken = () => leerSesionGuardada()?.token ?? null;
