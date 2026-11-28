/**
 * NexTalent — ConfiguracionPerfil
 * Permite editar la información y la fotografía del candidato o reclutador.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { useEffect, useState, type FormEvent } from 'react';
import { ImagePlus, LockKeyhole, Save, UserRound, X } from 'lucide-react';
import { usarAplicacion } from '../contexto/ContextoAplicacion';
import VinculacionWhatsapp from '../componentes/VinculacionWhatsapp';
import { validarEnlaceEmpresa } from '../datos';
import type { RolUsuario, PerfilUsuario, PreferenciasReclutador } from '../tipos';

/** Establece la estructura de datos de FormularioCandidato. */
type FormularioCandidato = { nombrePila: string; apellidoPaterno: string; apellidoMaterno: string; edad: string; titulo: string; curp: string; calle: string; numeroExterior: string; colonia: string; municipio: string; entidadFederativa: string; codigoPostal: string; celular: string; correo: string; aceptaWhatsapp: boolean; };
/** Ejecuta la operación obtenerFormularioCandidato dentro del frontend. */
const obtenerFormularioCandidato = (elemento: PerfilUsuario): FormularioCandidato => ({ nombrePila: elemento.nombrePila || elemento.nombre.split(' ')[0] || '', apellidoPaterno: elemento.apellidoPaterno || elemento.nombre.split(' ')[1] || '', apellidoMaterno: elemento.apellidoMaterno || elemento.nombre.split(' ')[2] || '', edad: elemento.edad || '', titulo: elemento.titulo || '', curp: elemento.curp || '', calle: elemento.calle || '', numeroExterior: elemento.numeroExterior || '', colonia: elemento.colonia || '', municipio: elemento.municipio || '', entidadFederativa: elemento.entidadFederativa || '', codigoPostal: elemento.codigoPostal || '', celular: elemento.celular || '', correo: elemento.correo || '', aceptaWhatsapp: elemento.aceptaWhatsapp });
/** Define el orden y las etiquetas del formulario del candidato. */
const CAMPOS_CANDIDATO: { clave: keyof FormularioCandidato, etiqueta: string, tipo?: string, ejemplo?: string, anchoCompleto?: boolean ;}[] = [
  { clave: 'nombrePila', etiqueta: 'Nombre(s)', ejemplo: 'Nombre completo' }, { clave: 'apellidoPaterno', etiqueta: 'Apellido paterno' }, { clave: 'apellidoMaterno', etiqueta: 'Apellido materno' }, { clave: 'edad', etiqueta: 'Edad', tipo: 'number', ejemplo: '18' }, { clave: 'titulo', etiqueta: 'Ocupación', ejemplo: 'Ej. Estudiante, ingeniero', anchoCompleto: true }, { clave: 'curp', etiqueta: 'CURP', ejemplo: '18 caracteres' }, { clave: 'calle', etiqueta: 'Calle', anchoCompleto: true }, { clave: 'numeroExterior', etiqueta: 'Número exterior' }, { clave: 'colonia', etiqueta: 'Colonia' }, { clave: 'municipio', etiqueta: 'Municipio / alcaldía' }, { clave: 'entidadFederativa', etiqueta: 'Estado' }, { clave: 'codigoPostal', etiqueta: 'Código postal', ejemplo: '5 dígitos' }, { clave: 'celular', etiqueta: 'Teléfono celular', tipo: 'tel', ejemplo: '10 dígitos' }, { clave: 'correo', etiqueta: 'Correo electrónico', tipo: 'correo', anchoCompleto: true }
];
/** Valida tamaño y formato de la foto antes de mostrarla. */
function EditorFotografia({ nombre: nombre, fotografia: fotografia, alCambiarFotografia: alCambiarFotografia }: { nombre: string, fotografia?: string, alCambiarFotografia: (valor: string) => void ;}) {
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [error, establecerError] = useState('');
  /** Actualiza el dato elegido del formulario en memoria. */
  const actualizar = (elemento?: File) => { if (!elemento) return; if (!['image/jpeg', 'image/png', 'image/webp'].includes(elemento.type)) { establecerError('Selecciona JPG, PNG o WebP.'); return ;} if (elemento.size > 2 * 1024 * 1024) { establecerError('La fotografía debe pesar máximo 2 MB.'); return ;} const lector = new FileReader(); lector.onload = () => { alCambiarFotografia(String(lector.result)); establecerError('') ;}; lector.readAsDataURL(elemento) ;};
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="flex flex-col gap-4 sm:flex-row sm:items-center"><div className="grid h-24 w-24 flex-none place-items-center overflow-hidden rounded-3xl border border-[#DDCEA3] bg-[#F6F0B3] text-3xl font-extrabold text-[#6B4E00]">{fotografia ? <img src={fotografia} alt="Foto de perfil" className="h-full w-full object-cover" /> : nombre.trim().charAt(0).toUpperCase() || <UserRound />}</div><div className="min-w-0 flex-1"><p className="text-base font-bold">Personaliza tu fotografía</p><p className="mt-1 text-xs leading-5 text-slate-500 dark:text-slate-300">JPG, PNG o WebP · máximo 2 MB. Solo se muestra en esta sesión.</p><div className="mt-3 flex flex-wrap gap-2"><label className="boton-secundario cursor-pointer"><ImagePlus size={16} /> Elegir foto<input type="file" accept="image/png,image/jpeg,image/webp" className="sr-only" onChange={evento => actualizar(evento.target.files?.[0])} /></label>{fotografia && <button className="boton-discreto" type="button" onClick={() => alCambiarFotografia('')}><X size={16} /> Quitar foto</button>}</div>{error && <p role="alert" className="mt-2 text-xs text-red-600">{error}</p>}</div></div>;
}
/** Presenta los campos editables y las preferencias de comunicación. */
export default function ConfiguracionPerfil({ rol: rol }: { rol: RolUsuario ;}) {
  const { perfil, guardarPerfil, actualizarPerfil, preferenciasReclutador, actualizarPreferenciasReclutador } = usarAplicacion();
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [candidato, establecerCandidato] = useState<FormularioCandidato>(() => obtenerFormularioCandidato(perfil));
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [reclutador, establecerReclutador] = useState<PreferenciasReclutador>(preferenciasReclutador);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [fotografia, setPhoto] = useState(rol === 'candidato' ? perfil.datosFotografia || '' : preferenciasReclutador.datosFotografia || '');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [error, establecerError] = useState('');
  // Efecto: sincroniza un cambio de datos con la interfaz o el navegador.
  useEffect(() => { setPhoto(rol === 'candidato' ? perfil.datosFotografia || '' : preferenciasReclutador.datosFotografia || '') ;}, [rol]);
  /** Actualiza el dato elegido del formulario en memoria. */
  const actualizar = (clave: keyof FormularioCandidato, val: string | boolean) => establecerCandidato(elemento => ({ ...elemento, [clave]: val }));
  /** Ejecuta la operación enviarFormulario dentro del frontend. */
  const enviarFormulario = (evento: FormEvent) => {    
evento.preventDefault(); establecerError('');
    if (rol === 'candidato') {
      if (!candidato.nombrePila.trim() || !candidato.apellidoPaterno.trim() || !candidato.apellidoMaterno.trim()) { establecerError('Completa tu nombre y ambos apellidos.'); return ;}
      if (candidato.edad && (+candidato.edad < 18 || +candidato.edad > 100)) { establecerError('La edad debe estar entre 18 y 100 años.'); return ;}
      if (candidato.curp && !/^[A-Z]{4}\d{6}[HM][A-Z]{5}[A-Z\d]\d$/i.test(candidato.curp)) { establecerError('Revisa los 18 caracteres de la CURP.'); return ;}
      if (!/^\d{10}$/.test(candidato.celular.replace(/\s/g, ''))) { establecerError('El teléfono celular es obligatorio: escribe 10 dígitos.'); return ;}
      if (candidato.codigoPostal && !/^\d{5}$/.test(candidato.codigoPostal)) { establecerError('El código postal debe tener 5 dígitos.'); return ;}
      if (!/^\S+@\S+\.\S+$/.test(candidato.correo)) { establecerError('Introduce un correo electrónico válido.'); return ;}
      guardarPerfil({ ...candidato, nombre: [candidato.nombrePila, candidato.apellidoPaterno, candidato.apellidoMaterno].join(' ').replace(/\s+/g, ' ').trim(), celular: candidato.celular.replace(/\s/g, ''), ubicacion: [candidato.municipio, candidato.entidadFederativa].filter(Boolean).join(', ') || perfil.ubicacion, datosFotografia: fotografia });
    } else {
      if (!reclutador.nombreContacto.trim() || !reclutador.empresa.trim()) { establecerError('Escribe tu nombre y empresa.'); return ;}
      if (reclutador.aceptaWhatsapp && !/^\d{10}$/.test(reclutador.celular.replace(/\s/g, ''))) { establecerError('Para activar WhatsApp proporciona un celular de 10 dígitos.'); return ;}
      if (!/^\S+@\S+\.\S+$/.test(reclutador.correo)) { establecerError('Escribe un correo válido.'); return ;}
      if (reclutador.sitioWeb?.trim() && !validarEnlaceEmpresa(reclutador.sitioWeb)) {
        establecerError('Escribe la página web completa de la empresa, empezando por https://'); return;
      }
      actualizarPreferenciasReclutador({ ...reclutador, celular: reclutador.celular.replace(/\s/g, ''), datosFotografia: fotografia });
    }
  };
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="mx-auto max-w-[940px] min-w-0 space-y-6">
    <div><p className="text-xs font-extrabold uppercase tracking-[.2em] text-[#987200] dark:text-[#D4AF36]">Configuración</p><h1 className="mt-2 text-2xl font-extrabold tracking-tight sm:text-3xl">Personalización de cuenta</h1><p className="mt-2 text-sm leading-6 text-slate-500 dark:text-slate-300">Actualiza tu foto, datos y preferencias. Los cambios de esta interfaz no se envían a servidores.</p></div>
    <form onSubmit={enviarFormulario} className="space-y-5">
      <section className="panel min-w-0 p-5 sm:p-7"><h2 className="mb-5 text-lg font-bold">Foto de perfil</h2><EditorFotografia nombre={rol === 'candidato' ? candidato.nombrePila : reclutador.nombreContacto} fotografia={fotografia} alCambiarFotografia={setPhoto} /></section>
      {rol === 'candidato' ? <section className="panel min-w-0 p-5 sm:p-7"><div className="mb-6"><h2 className="text-lg font-bold">Información personal</h2><p className="mt-1 text-xs text-slate-500 dark:text-slate-300">Completa o actualiza tus datos personales.</p></div><div className="grid min-w-0 grid-cols-1 gap-x-5 gap-y-5 sm:grid-cols-2">{CAMPOS_CANDIDATO.map(elemento => <label key={elemento.clave} className={`block min-w-0 ${elemento.anchoCompleto ? 'sm:col-span-2' : ''}`}><span className="etiqueta-campo">{elemento.etiqueta}</span><input className="campo" type={elemento.tipo || 'text'} value={String(candidato[elemento.clave] ?? '')} onChange={evento => actualizar(elemento.clave, elemento.clave === 'curp' ? evento.target.value.toUpperCase() : evento.target.value)} placeholder={elemento.ejemplo || elemento.etiqueta} maxLength={elemento.clave === 'curp' ? 18 : elemento.clave === 'codigoPostal' ? 5 : elemento.clave === 'celular' ? 10 : undefined} /></label>)}</div></section>
        : <section className="panel min-w-0 p-5 sm:p-7"><h2 className="mb-5 text-lg font-bold">Información del reclutador</h2><div className="grid min-w-0 gap-5 sm:grid-cols-2"><label className="block sm:col-span-2"><span className="etiqueta-campo">Tu nombre</span><input className="campo" required value={reclutador.nombreContacto} onChange={evento => establecerReclutador(elemento => ({ ...elemento, nombreContacto: evento.target.value }))} /></label><label className="block sm:col-span-2"><span className="etiqueta-campo">Empresa</span><input className="campo" required value={reclutador.empresa} onChange={evento => establecerReclutador(elemento => ({ ...elemento, empresa: evento.target.value }))} /></label><label className="block sm:col-span-2"><span className="etiqueta-campo">Página web de la empresa (opcional)</span><input type="url" className="campo" placeholder="https://www.miempresa.com" value={reclutador.sitioWeb || ''} onChange={evento => establecerReclutador(anterior => ({ ...anterior, sitioWeb: evento.target.value }))} /><span className="mt-1 block text-xs text-slate-500">El candidato podrá abrir este enlace al hacer clic en el nombre de la empresa.</span></label><label><span className="etiqueta-campo">Correo de contacto</span><input type="email" className="campo" required value={reclutador.correo} onChange={evento => establecerReclutador(elemento => ({ ...elemento, correo: evento.target.value }))} /></label><label><span className="etiqueta-campo">Teléfono celular</span><input type="tel" inputMode="numeric" maxLength={10} className="campo" placeholder="10 dígitos" value={reclutador.celular} onChange={evento => establecerReclutador(elemento => ({ ...elemento, celular: evento.target.value }))} /></label></div></section>}
      <VinculacionWhatsapp rol={rol} celularInicial={rol === 'candidato' ? candidato.celular : reclutador.celular}
        alVincular={celular => rol === 'candidato'
          ? actualizarPerfil({ aceptaWhatsapp: true })
          : actualizarPreferenciasReclutador({ aceptaWhatsapp: true, celular })} />
      <div className="flex flex-wrap items-start justify-between gap-4"><div className="flex max-w-lg items-start gap-2 text-xs leading-5 text-slate-500 dark:text-slate-300"><LockKeyhole size={15} className="mt-0.5 shrink-0" /> La CURP, dirección y fotografía no se guardan de forma persistente. Para cuentas reales hace falta autenticación y almacenamiento seguro.</div><div>{error && <p role="alert" className="mb-2 max-w-xs text-xs text-red-600">{error}</p>}<button type="submit" className="boton-principal w-full sm:w-auto"><Save size={17} /> Guardar cambios</button></div></div>
    </form>
  </div>;
}
