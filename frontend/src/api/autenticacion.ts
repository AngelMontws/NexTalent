/**
 * NexTalent — autenticacion
 * Inicio de sesión y registro con Identidad (Backend 1), por el gateway en /api/auth.
 * POST /api/auth/login { email, password } → 200 { accessToken } (Identidad, #63).
 */
import { ErrorApi, pedir } from './cliente';
import { crearSesion, type Sesion } from './sesion';

interface RespuestaLogin { accessToken?: string; token?: string; }

export async function iniciarSesion(correo: string, contrasena: string): Promise<Sesion> {
  let respuesta: RespuestaLogin;
  try {
    respuesta = await pedir<RespuestaLogin>('/api/auth/login', { metodo: 'POST', cuerpo: { email: correo, password: contrasena }, sinToken: true });
  } catch (error) {
    if (error instanceof ErrorApi && (error.estado === 400 || error.estado === 401)) {
      throw new ErrorApi(error.estado, 'Correo o contraseña incorrectos.');
    }
    if (error instanceof ErrorApi && error.estado === 404) {
      throw new ErrorApi(404, 'El inicio de sesión todavía no está disponible en el servidor.');
    }
    throw error;
  }

  const token = respuesta.accessToken ?? respuesta.token;
  const sesion = token ? crearSesion(token, correo) : null;
  if (!sesion) throw new ErrorApi(403, 'Esta cuenta no tiene acceso a esta aplicación.');
  return sesion;
}

/** Datos del registro de candidato (HU-01.01). Contrato propuesto: POST /api/auth/registro → 201, luego login. */
export interface DatosRegistro {
  email: string; password: string; nombres: string; apellidoPaterno: string; apellidoMaterno: string;
  edad: number; ocupacion: string; curp: string; calle: string; numeroExterior: string; colonia: string;
  municipio: string; entidadFederativa: string; codigoPostal: string; telefonoWhatsapp: string;
  whatsappOptIn: boolean; avisoPrivacidadAceptado: boolean;
}

export async function registrarCandidato(datos: DatosRegistro): Promise<Sesion> {
  try {
    await pedir<unknown>('/api/auth/registro', { metodo: 'POST', cuerpo: datos, sinToken: true });
  } catch (error) {
    if (error instanceof ErrorApi && error.estado === 404) {
      throw new ErrorApi(404, 'El registro todavía no está disponible en el servidor.');
    }
    if (error instanceof ErrorApi && error.estado === 409) {
      throw new ErrorApi(409, 'Ya existe una cuenta con ese correo.');
    }
    throw error;
  }
  return iniciarSesion(datos.email, datos.password);
}
