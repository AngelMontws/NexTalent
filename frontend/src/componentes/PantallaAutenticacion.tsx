/**
 * NexTalent — PantallaAutenticacion
 * Presenta ingreso y registro con Identidad (por el gateway), CAPTCHA y recuperación.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { useMemo, useState, type FormEvent } from 'react';
import { ArrowLeft, Eye, EyeOff, LockKeyhole, Mail, RotateCcw, ShieldCheck, UserPlus } from 'lucide-react';
import { iniciarSesion, registrarCandidato } from '../api/autenticacion';
import { mensajeDeError } from '../api/cliente';
import type { Sesion } from '../api/sesion';

/** Establece la estructura de datos de PantallaAcceso. */
type PantallaAcceso = 'ingreso' | 'registro' | 'recuperacion';
/** Establece la estructura de datos de CamposRegistro. */
type CamposRegistro = {
  nombre: string; paterno: string; materno: string; edad: string; ocupacion: string; curp: string;
  calle: string; numero: string; colonia: string; municipio: string; estado: string; codigoPostal: string;
  celular: string; correo: string; contrasena: string; confirmacion: string; whatsapp: boolean; privacidad: boolean;
};
const inicial: CamposRegistro = { nombre: '', paterno: '', materno: '', edad: '', ocupacion: '', curp: '', calle: '', numero: '', colonia: '', municipio: '', estado: '', codigoPostal: '', celular: '', correo: '', contrasena: '', confirmacion: '', whatsapp: false, privacidad: false };
/** Patrón básico para revisar el formato de una CURP. */
const EXPRESION_CURP = /^[A-Z]{4}\d{6}[HM][A-Z]{5}[A-Z\d]\d$/i;
const celular = /^\d{10}$/;
const estiloCampo = 'campo';
/** Crea dos números pequeños y calcula su suma esperada. */
const generarPreguntaCaptcha = () => {
  const numeroUno = Math.floor(Math.random() * 8) + 2, numeroDos = Math.floor(Math.random() * 7) + 1;
  return { numeroUno, numeroDos, respuesta: String(numeroUno + numeroDos) };
};
/** Administra el acceso de prueba, el registro y la recuperación simulada. */
export default function PantallaAutenticacion({ alAcceder: alAcceder, oscuro: oscuro, alCambiarModoOscuro: alCambiarModoOscuro }: { alAcceder: (sesion: Sesion) => void; oscuro: boolean; alCambiarModoOscuro: () => void; }) {
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [pantalla, establecerPantalla] = useState<PantallaAcceso>('ingreso');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [captcha, establecerCaptcha] = useState(generarPreguntaCaptcha);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [respuestaCaptcha, establecerRespuestaCaptcha] = useState('');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [error, establecerError] = useState('');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [aviso, establecerAviso] = useState('');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [correo, establecerCorreo] = useState('');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [contrasena, establecerContrasena] = useState('');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [mostrarContrasena, establecerMostrarContrasena] = useState(false);
  // Mientras espera la respuesta del servidor, se bloquea el botón para no enviar dos veces.
  const [enviando, establecerEnviando] = useState(false);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [campos, establecerCampos] = useState<CamposRegistro>(inicial);
  /** Crea un CAPTCHA nuevo y borra la respuesta anterior. */
  const renovarCaptcha = () => { establecerCaptcha(generarPreguntaCaptcha()); establecerRespuestaCaptcha('') ;};
  /** Cambia entre ingreso, registro y recuperación. */
  const cambiarPantalla = (to: PantallaAcceso) => { establecerPantalla(to); establecerError(''); establecerAviso(''); renovarCaptcha(); window.scrollTo(0, 0) ;};
  /** Actualiza el dato elegido del formulario en memoria. */
  const actualizar = (nombre: keyof CamposRegistro, valor: string | boolean) => establecerCampos(elemento => ({ ...elemento, [nombre]: valor }));
  /** Lista de campos solicitados durante el registro. */
  const datosFormulario = useMemo(() => [
    { etiqueta: 'Nombre(s) *', nombreCampo: 'nombre', tipo: 'text', ejemplo: 'Tu nombre' },
    { etiqueta: 'Apellido paterno *', nombreCampo: 'paterno', tipo: 'text', ejemplo: 'Primer apellido' },
    { etiqueta: 'Apellido materno *', nombreCampo: 'materno', tipo: 'text', ejemplo: 'Segundo apellido' },
    { etiqueta: 'Edad *', nombreCampo: 'edad', tipo: 'number', ejemplo: '18' },
    { etiqueta: 'Ocupación *', nombreCampo: 'ocupacion', tipo: 'text', ejemplo: 'Estudiante, analista, desarrollador…' },
    { etiqueta: 'CURP *', nombreCampo: 'curp', tipo: 'text', ejemplo: '18 caracteres' },
    { etiqueta: 'Calle *', nombreCampo: 'calle', tipo: 'text', ejemplo: 'Nombre de la calle' },
    { etiqueta: 'Número exterior *', nombreCampo: 'numero', tipo: 'text', ejemplo: 'Núm. exterior' },
    { etiqueta: 'Colonia *', nombreCampo: 'colonia', tipo: 'text', ejemplo: 'Colonia' },
    { etiqueta: 'Municipio / alcaldía *', nombreCampo: 'municipio', tipo: 'text', ejemplo: 'Municipio o alcaldía' },
    { etiqueta: 'Estado *', nombreCampo: 'estado', tipo: 'text', ejemplo: 'Entidad federativa' },
    { etiqueta: 'Código postal *', nombreCampo: 'codigoPostal', tipo: 'text', ejemplo: '5 dígitos' },
    { etiqueta: 'Teléfono celular *', nombreCampo: 'celular', tipo: 'tel', ejemplo: '10 dígitos' },
    { etiqueta: 'Correo electrónico *', nombreCampo: 'correo', tipo: 'email', ejemplo: 'tucorreo@ejemplo.com' },
    { etiqueta: 'Contraseña *', nombreCampo: 'contrasena', tipo: 'password', ejemplo: '8 caracteres o más' },
    { etiqueta: 'Confirmar contraseña *', nombreCampo: 'confirmacion', tipo: 'password', ejemplo: 'Repite tu contraseña' }
  ] as const, []);
  /** Comprueba los campos y el CAPTCHA, y pide el token a Identidad. El rol viene en el token. */
  const enviarIngreso = async (evento: FormEvent) => {
    evento.preventDefault(); establecerError('');
    if (!/^\S+@\S+\.\S+$/.test(correo) || contrasena.length < 8) { establecerError('Escribe un correo válido y una contraseña de al menos 8 caracteres.'); return; }
    if (respuestaCaptcha.trim() !== captcha.respuesta) { establecerError('CAPTCHA incorrecto. Inténtalo otra vez.'); renovarCaptcha(); return; }
    establecerEnviando(true);
    try { alAcceder(await iniciarSesion(correo.trim(), contrasena)); }
    catch (falla) { establecerError(mensajeDeError(falla)); renovarCaptcha(); }
    finally { establecerEnviando(false); }
  };
  /** Valida los datos del formulario, crea la cuenta en Identidad e inicia sesión. */
  const enviarRegistro = async (evento: FormEvent) => {
    evento.preventDefault(); establecerError('');
    const obligatorios = ['nombre', 'paterno', 'materno', 'edad', 'ocupacion', 'curp', 'calle', 'numero', 'colonia', 'municipio', 'estado', 'codigoPostal', 'celular', 'correo', 'contrasena', 'confirmacion'] as const;
    if (obligatorios.some(clave => !String(campos[clave]).trim())) { establecerError('Completa todos los campos marcados con *.'); return ;}
    if (Number(campos.edad) < 18 || Number(campos.edad) > 100) { establecerError('La edad debe estar entre 18 y 100 años.'); return ;}
    if (!EXPRESION_CURP.test(campos.curp.trim())) { establecerError('Escribe una CURP de 18 caracteres con formato válido.'); return ;}
    if (!/^\d{5}$/.test(campos.codigoPostal)) { establecerError('El código postal debe tener 5 dígitos.'); return ;}
    if (!celular.test(campos.celular.replace(/\s/g, ''))) { establecerError('El celular debe tener 10 dígitos.'); return ;}
    if (!/^\S+@\S+\.\S+$/.test(campos.correo)) { establecerError('Escribe un correo válido.'); return ;}
    if (campos.contrasena.length < 8 || !/[A-Za-z]/.test(campos.contrasena) || !/\d/.test(campos.contrasena)) { establecerError('La contraseña debe tener 8 caracteres o más, con letras y números.'); return ;}
    if (campos.contrasena !== campos.confirmacion) { establecerError('Las contraseñas no coinciden.'); return ;}
    if (!campos.privacidad) { establecerError('Acepta el aviso de privacidad.'); return ;}
    if (respuestaCaptcha.trim() !== captcha.respuesta) { establecerError('CAPTCHA incorrecto.'); renovarCaptcha(); return ;}
    // Los datos viajan solo a Identidad por HTTPS; no se guardan en el navegador.
    establecerEnviando(true);
    try {
      alAcceder(await registrarCandidato({
        email: campos.correo.trim(), password: campos.contrasena, nombres: campos.nombre.trim(), apellidoPaterno: campos.paterno.trim(),
        apellidoMaterno: campos.materno.trim(), edad: Number(campos.edad), ocupacion: campos.ocupacion.trim(), curp: campos.curp.trim().toUpperCase(),
        calle: campos.calle.trim(), numeroExterior: campos.numero.trim(), colonia: campos.colonia.trim(), municipio: campos.municipio.trim(),
        entidadFederativa: campos.estado.trim(), codigoPostal: campos.codigoPostal, telefonoWhatsapp: campos.celular.replace(/\s/g, ''),
        whatsappOptIn: campos.whatsapp, avisoPrivacidadAceptado: campos.privacidad
      }));
    } catch (falla) { establecerError(mensajeDeError(falla)); renovarCaptcha(); }
    finally { establecerEnviando(false); }
  };
  /** Simula una solicitud de recuperación sin enviar correos. */
  const recuperarContrasena = (evento: FormEvent) => { evento.preventDefault(); establecerError(''); if (!/^\S+@\S+\.\S+$/.test(correo)) { establecerError('Introduce un correo electrónico válido.'); return ;} if (respuestaCaptcha.trim() !== captcha.respuesta) { establecerError('CAPTCHA incorrecto.'); renovarCaptcha(); return ;} establecerAviso('La recuperación de contraseña todavía no está disponible. Pide ayuda al administrador.') ;};
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="min-h-screen bg-[#F2F2F2] px-3 py-5 dark:bg-[#171510] sm:px-5 sm:py-9">
    <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3 pb-5 sm:pb-7"><button type="button" onClick={() => cambiarPantalla('ingreso')} aria-label="Volver a iniciar sesión" className="flex items-center gap-3 rounded-xl bg-transparent p-1 text-left transition hover:bg-[#F6F0B3]/40 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#CD9901]"><img src="/nextalent-logo.png" className="h-14 w-14 object-contain" alt="NexTalent" /><div><div className="text-xl font-black tracking-tight">Nex<span className="text-[#9A7100] dark:text-[#D4AF36]">Talent</span></div><p className="text-xs text-slate-500 dark:text-slate-400">Tu siguiente oportunidad</p></div></button><button type="button" className="boton-secundario" onClick={alCambiarModoOscuro}>{oscuro ? '☀ Modo claro' : '☾ Modo oscuro'}</button></div>
    <div className={`mx-auto grid max-w-5xl min-w-0 overflow-hidden rounded-[24px] border border-[#E6DDBF] bg-[#FFFFEF] shadow-xl dark:border-[#4B3C22] dark:bg-[#242018] ${pantalla === 'registro' ? 'lg:grid-cols-[minmax(0,.7fr)_minmax(0,1.3fr)]' : 'lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]'}`}>
      <div className="flex flex-col justify-between gap-6 lg:gap-12 bg-gradient-to-br from-[#CD9901] via-[#D4AF36] to-[#F6F0B3] p-8 text-[#302817] sm:p-11"><div><span className="inline-flex rounded-full border border-[#302817]/20 bg-white/25 px-3 py-1 text-xs font-bold">BOLSA DE TRABAJO · MÉXICO</span><h1 className="mt-9 max-w-md text-4xl font-black leading-tight tracking-tight sm:text-[46px]">El talento encuentra su lugar.</h1><p className="mt-5 max-w-md text-sm leading-7 text-[#44340D]">Vacantes verificadas, salarios claros y seguimiento de cada postulación en un mismo lugar.</p></div><div className="space-y-3 text-sm font-semibold"><p>✓ Rango salarial en cada vacante</p><p>✓ Seguimiento de tu proceso</p><p>✓ Alertas por WhatsApp con tu autorización</p></div></div>
      <div className="min-w-0 p-5 sm:p-7 lg:p-9">
        {pantalla !== 'ingreso' && <button type="button" onClick={() => cambiarPantalla('ingreso')} className="mb-5 inline-flex items-center gap-2 text-sm font-bold text-[#967000] dark:text-[#F6F0B3]"><ArrowLeft size={17} /> Volver a iniciar sesión</button>}
        <h2 className="text-2xl font-extrabold">{pantalla === 'ingreso' ? 'Iniciar sesión' : pantalla === 'registro' ? 'Crear cuenta' : 'Recuperar contraseña'}</h2><p className="mb-7 mt-2 text-sm text-slate-500 dark:text-slate-400">{pantalla === 'ingreso' ? 'Bienvenido a tu espacio NexTalent.' : pantalla === 'registro' ? 'Completa tus datos para crear tu perfil de candidato.' : 'Ingresa tu correo para recuperar el acceso.'}</p>
        {error && <div role="alert" className="mb-5 rounded-xl border border-red-200 bg-red-50 px-3 py-3 text-sm text-red-700">{error}</div>}
        {aviso && <div role="status" className="mb-5 rounded-xl bg-[#F6F0B3] p-3 text-sm text-[#44340D]">{aviso}</div>}
        {pantalla === 'ingreso' && <form onSubmit={enviarIngreso} className="space-y-4">
          <label className="block"><span className="etiqueta-campo">Correo electrónico</span><div className="relative"><Mail className="absolute left-3 top-3.5 text-slate-400" size={17} /><input className="campo !pl-10" type="email" value={correo} onChange={evento => establecerCorreo(evento.target.value)} placeholder="tucorreo@ejemplo.com" required /></div></label>
          <label className="block"><span className="etiqueta-campo">Contraseña</span><div className="relative"><LockKeyhole className="absolute left-3 top-3.5 text-slate-400" size={17} /><input className="campo !pl-10 !pr-11" type={mostrarContrasena ? 'text' : 'password'} value={contrasena} onChange={evento => establecerContrasena(evento.target.value)} minLength={8} required placeholder="Tu contraseña" /><button type="button" aria-label={mostrarContrasena ? 'Ocultar contraseña' : 'Mostrar contraseña'} className="absolute right-2 top-1.5 grid h-9 w-9 place-items-center" onClick={() => establecerMostrarContrasena(valor => !valor)}>{mostrarContrasena ? <EyeOff size={18} /> : <Eye size={18} />}</button></div></label>
          <div className="flex justify-end"><button type="button" className="text-sm font-semibold text-[#956B00] underline dark:text-[#F6F0B3]" onClick={() => cambiarPantalla('recuperacion')}>¿Olvidaste tu contraseña?</button></div>
          {mostrarCaptcha()}
          <button className="boton-principal w-full" type="submit" disabled={enviando}><ShieldCheck size={17} /> {enviando ? 'Entrando…' : 'Iniciar sesión'}</button>
          <div className="text-center text-sm text-slate-500">¿No tienes cuenta? <button type="button" className="font-bold text-[#956B00] dark:text-[#F6F0B3]" onClick={() => cambiarPantalla('registro')}>Crear cuenta</button></div>
        </form>}
        {pantalla === 'recuperacion' && <form onSubmit={recuperarContrasena} className="space-y-4"><label className="block"><span className="etiqueta-campo">Correo electrónico</span><input className="campo" value={correo} type="email" required onChange={evento => establecerCorreo(evento.target.value)} /></label>{mostrarCaptcha()}<button type="submit" className="boton-principal w-full"><Mail size={17} /> Solicitar recuperación</button></form>}
        {pantalla === 'registro' && <form onSubmit={enviarRegistro} className="space-y-5" noValidate>
          <div className="rounded-xl bg-[#F6F0B3]/70 p-3 text-xs leading-5 text-[#453513]">Los datos son confidenciales. Se envían de forma segura al servidor de NexTalent y no se guardan en este navegador.</div>
          <div className="grid min-w-0 grid-cols-1 gap-x-5 gap-y-4 sm:grid-cols-2">{datosFormulario.map(elemento => <label key={elemento.nombreCampo} className={`block ${['calle', 'colonia', 'ocupacion'].includes(elemento.nombreCampo) ? 'sm:col-span-2' : ''}`}><span className="etiqueta-campo">{elemento.etiqueta}</span><input className={estiloCampo} type={elemento.tipo} value={campos[elemento.nombreCampo]} onChange={evento => actualizar(elemento.nombreCampo, elemento.nombreCampo === 'curp' ? evento.target.value.toUpperCase() : evento.target.value)} placeholder={elemento.ejemplo} min={elemento.nombreCampo === 'edad' ? 18 : undefined} max={elemento.nombreCampo === 'edad' ? 100 : undefined} maxLength={elemento.nombreCampo === 'curp' ? 18 : elemento.nombreCampo === 'codigoPostal' ? 5 : elemento.nombreCampo === 'celular' ? 10 : undefined} autoComplete="off" required /></label>)}</div>
          <label className="flex items-start gap-3 rounded-xl border border-[#E7D9A8] p-3 text-sm dark:border-[#66522B]"><input type="checkbox" className="mt-1 h-4 w-4 accent-[#CD9901]" checked={campos.whatsapp} onChange={evento => actualizar('whatsapp', evento.target.checked)} /><span>Autorizo recibir notificaciones sobre mis postulaciones por WhatsApp. <span className="block text-xs text-slate-500">El teléfono celular es obligatorio; las notificaciones requieren autorización.</span></span></label>
          <label className="flex items-start gap-3 rounded-xl border border-[#E7D9A8] p-3 text-sm dark:border-[#66522B]"><input type="checkbox" className="mt-1 h-4 w-4 accent-[#CD9901]" checked={campos.privacidad} onChange={evento => actualizar('privacidad', evento.target.checked)} /><span>Acepto el aviso de privacidad. *</span></label>
          {mostrarCaptcha()}
          <button className="boton-principal w-full" type="submit" disabled={enviando}><UserPlus size={18} /> {enviando ? 'Creando cuenta…' : 'Crear cuenta de candidato'}</button>
        </form>}
      </div>
    </div>
  </div>;
  /** Muestra una suma sencilla como CAPTCHA visual de demostración. */
  function mostrarCaptcha() { return <div className="rounded-xl border border-[#E5D7A9] bg-[#FFFDF3] p-3 dark:border-[#66522B] dark:bg-[#30291F]"><div className="mb-2 flex items-center justify-between"><span className="text-xs font-bold"><ShieldCheck size={14} className="mr-1 inline" /> Verificación CAPTCHA</span><button type="button" className="inline-flex items-center gap-1 text-xs text-[#806000] dark:text-[#F6F0B3]" onClick={renovarCaptcha}><RotateCcw size={13} /> Cambiar</button></div><div className="flex min-w-0 flex-wrap items-center gap-2"><span className="min-w-28 rounded-lg bg-[#F6F0B3] px-3 py-2 text-center text-base font-black tracking-widest text-[#302817]">{captcha.numeroUno} + {captcha.numeroDos} = ?</span><input aria-label="Respuesta al CAPTCHA" className="campo min-w-[120px] flex-1" inputMode="numeric" type="number" value={respuestaCaptcha} onChange={evento => establecerRespuestaCaptcha(evento.target.value)} placeholder="Respuesta" required /></div></div>; }
}
