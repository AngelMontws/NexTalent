/**
 * NexTalent — cliente
 * Hace las peticiones al gateway con el JWT y convierte los errores (ProblemDetails) en mensajes.
 * En desarrollo, Vite reenvía /api y /hubs al gateway; en producción se sirven en el mismo dominio.
 */
import { leerToken } from './sesion';

/** Error con el código HTTP y, si el perfil está incompleto, la lista de lo que falta. */
export class ErrorApi extends Error {
  constructor(public readonly estado: number, mensaje: string, public readonly faltantes: string[] = []) {
    super(mensaje);
  }
}

/** Mensajes para cuando el servidor no manda detalle. */
const MENSAJES: Record<number, string> = {
  0: 'No hay conexión con el servidor. Revisa que el backend esté encendido.',
  401: 'Tu sesión terminó. Vuelve a iniciar sesión.',
  403: 'No tienes permiso para esta acción.',
  404: 'No se encontró la información solicitada.',
  503: 'El servicio no está disponible por ahora. Intenta de nuevo en un momento.'
};

/** Se llama cuando el backend responde 401, para regresar al inicio de sesión. */
let alVencerSesion: () => void = () => {};
export const registrarAlVencerSesion = (accion: () => void) => { alVencerSesion = accion; };

export async function pedir<T>(ruta: string, opciones: { metodo?: string; cuerpo?: unknown; sinToken?: boolean } = {}): Promise<T> {
  const token = opciones.sinToken ? null : leerToken();
  const encabezados: Record<string, string> = { Accept: 'application/json' };
  if (opciones.cuerpo !== undefined) encabezados['Content-Type'] = 'application/json';
  if (token) encabezados.Authorization = `Bearer ${token}`;

  let respuesta: Response;
  try {
    respuesta = await fetch(ruta, {
      method: opciones.metodo ?? 'GET',
      headers: encabezados,
      body: opciones.cuerpo !== undefined ? JSON.stringify(opciones.cuerpo) : undefined
    });
  } catch {
    throw new ErrorApi(0, MENSAJES[0]);
  }

  if (respuesta.status === 401 && token) alVencerSesion();
  const texto = await respuesta.text();
  let datos: unknown = undefined;
  try { datos = texto ? JSON.parse(texto) : undefined; } catch { /* La respuesta no era JSON. */ }

  if (!respuesta.ok) {
    // ProblemDetails (title/detail) o la respuesta de Notificaciones ({ exito, mensaje }).
    const problema = (datos ?? {}) as { title?: string; detail?: string; mensaje?: string; faltantes?: string[] };
    const mensaje = problema.detail || problema.mensaje || problema.title || MENSAJES[respuesta.status] || `Error ${respuesta.status}`;
    throw new ErrorApi(respuesta.status, mensaje, problema.faltantes ?? []);
  }
  return datos as T;
}

/** Texto listo para mostrar al usuario a partir de cualquier error. */
export function mensajeDeError(error: unknown): string {
  if (error instanceof ErrorApi) {
    return error.faltantes.length ? `${error.message} Falta: ${error.faltantes.join(', ')}.` : error.message;
  }
  return 'Ocurrió un error inesperado.';
}
