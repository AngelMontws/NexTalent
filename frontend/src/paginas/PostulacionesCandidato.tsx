/**
 * NexTalent — PostulacionesCandidato
 * Consulta las postulaciones y su historial de cambios.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { useState } from 'react';
import { CalendarDays, Check, Clock3, ExternalLink, FileCheck2, Info, MessageCircle, SearchX } from 'lucide-react';
import { ETAPAS_POSTULACION, diasEnEtapa, formatearFecha, calcularCoincidencia, formatearSalario } from '../datos';
import { usarAplicacion } from '../contexto/ContextoAplicacion';
import { AvatarIniciales, TituloPantalla, InsigniaEstado } from '../componentes/ComponentesUI';
import type { Postulacion, Vacante } from '../tipos';

/** Muestra el resumen y el historial de una postulación. */
function ElementoPostulacion({ postulacion: postulacion, vacante: vacante }: { postulacion: Postulacion; vacante: Vacante; }) {
  const { perfil: perfil } = usarAplicacion();
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [historialAbierto, establecerHistorialAbierto] = useState(false);
  const indiceEtapa = ETAPAS_POSTULACION.indexOf(postulacion.estado as typeof ETAPAS_POSTULACION[number]);
  const estaCerrada = postulacion.estado === 'No seleccionado' || postulacion.estado === 'Oferta / Contratado';
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <article className="panel overflow-hidden">
    <div className="p-5 sm:p-6"><div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start"><div className="flex items-start gap-3"><AvatarIniciales iniciales={vacante.inicialesEmpresa} /><div><h2 className="font-bold leading-snug sm:text-lg">{vacante.titulo}</h2><p className="mt-1 text-sm text-slate-500">{vacante.empresa} · {vacante.ubicacion}</p></div></div><InsigniaEstado estado={postulacion.estado} /></div>
      <div className="mt-4 flex flex-wrap items-center gap-x-5 gap-y-2 text-xs text-slate-500"><span>{formatearSalario(vacante.salarioMinimo)} – {formatearSalario(vacante.salarioMaximo)} / mes</span><span>Coincidencia {postulacion.coincidencia ?? calcularCoincidencia(vacante, perfil.habilidades).porcentaje}%</span><span>Folio <strong className="text-slate-700 dark:text-slate-200">{postulacion.folio}</strong></span></div>
      <div className="mt-6"><div className="mb-3 flex items-center justify-between"><p className="text-xs font-bold uppercase tracking-wider text-slate-500">Seguimiento</p><span className="inline-flex items-center gap-1 text-xs text-slate-500"><Clock3 size={13} />{diasEnEtapa(postulacion.fechaActualizacion)} día(s) en esta etapa</span></div>
        <div className="grid grid-cols-5 gap-1.5" aria-label={`Estado actual: ${postulacion.estado}`}>{ETAPAS_POSTULACION.map((stage, indice) => <div key={stage} className="min-w-0"><div className={`h-2 rounded-full ${estaCerrada ? (postulacion.estado === 'Oferta / Contratado' ? 'bg-emerald-500' : 'bg-slate-300 dark:bg-slate-700') : indice <= indiceEtapa ? 'bg-[#CD9901]' : 'bg-slate-100 dark:bg-slate-800'}`} /><p className={`mt-2 text-center text-[9px] font-medium leading-tight sm:text-[11px] ${indice <= indiceEtapa && !estaCerrada ? 'text-[#806000] dark:text-[#F6F0B3]' : 'text-slate-400'}`}>{stage}</p></div>)}</div>
        {estaCerrada ? <p className={`mt-4 rounded-lg px-3 py-2 text-xs ${postulacion.estado === 'Oferta / Contratado' ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300' : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'}`}>{postulacion.estado === 'Oferta / Contratado' ? '¡Felicidades! El proceso terminó en contratación.' : 'El proceso concluyó. Esta vez no fuiste seleccionado; tu folio e historial permanecen disponibles.'}</p> : <p className="mt-4 flex gap-2 text-xs text-slate-500"><Info size={14} className="shrink-0 text-blue-500" />Tu estatus se actualizará cuando el reclutador mueva tu postulación.</p>}
      </div>
    </div>
    <div className="border-t border-slate-100 bg-slate-50/40 px-5 py-3 dark:border-slate-800 dark:bg-slate-900 sm:px-6"><div className="flex items-center justify-between gap-2"><span className="inline-flex items-center gap-1 text-xs text-slate-500"><CalendarDays size={14} /> Enviada el {formatearFecha(postulacion.fechaCreacion)}</span><button type="button" aria-expanded={historialAbierto} onClick={() => establecerHistorialAbierto(valor => !valor)} className="inline-flex min-h-10 items-center gap-1 text-xs font-bold text-[#937000] hover:text-[#806000]">{historialAbierto ? 'Ocultar historial' : 'Ver historial'} <ExternalLink size={13} /></button></div>
      {postulacion.documentos && postulacion.documentos.length > 0 && <p className="mt-2 text-xs text-[#876300]">✓ {postulacion.documentos.length} documento(s) seleccionado(s) para esta postulación (sin envío al servidor)</p>}
      {historialAbierto && <ol className="mt-3 space-y-3 border-l-2 border-[#D4AF36] pb-1 pl-4 dark:border-[#66522B]">{[...postulacion.historial].reverse().map((elemento, indice) => <li key={`${elemento.fecha}-${indice}`} className="relative text-sm before:absolute before:-left-[21px] before:top-1.5 before:h-2.5 before:w-2.5 before:rounded-full before:bg-[#FEF9DF]0"><div className="flex flex-wrap items-center justify-between gap-1"><span className="font-semibold">{elemento.estado}</span><span className="text-xs text-slate-500">{formatearFecha(elemento.fecha)}</span></div></li>)}</ol>}
    </div>
  </article>;
}
/** Agrupa las postulaciones activas y finalizadas del candidato. */
export default function PostulacionesCandidato({ alBuscar: alBuscar }: { alBuscar: () => void; }) {
  const { postulaciones: postulaciones, vacantes: vacantes, perfil: perfil, idCandidatoActual: idCandidatoActual } = usarAplicacion();
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [pestana, establecerPestana] = useState<'todas' | 'activas' | 'cerradas'>('todas');
  const todas = postulaciones.filter(elemento => elemento.idCandidato === idCandidatoActual).sort((elemento, b) => new Date(b.fechaActualizacion).getTime() - new Date(elemento.fechaActualizacion).getTime());
  const activas = todas.filter(elemento => elemento.estado !== 'Oferta / Contratado' && elemento.estado !== 'No seleccionado');
  const cerradas = todas.filter(elemento => elemento.estado === 'Oferta / Contratado' || elemento.estado === 'No seleccionado');
  const elementos = pestana === 'todas' ? todas : pestana === 'activas' ? activas : cerradas;
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="mx-auto max-w-4xl space-y-6"><div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end"><TituloPantalla sobretitulo="Mi actividad" titulo="Mis postulaciones" subtitulo="Consulta el avance de cada proceso, con estatus e historial de cambios visibles." /><button type="button" className="boton-principal sm:shrink-0" onClick={alBuscar}>Buscar más vacantes</button></div>
    <div className="grid grid-cols-3 gap-3"><div className="panel p-4"><p className="text-2xl font-extrabold">{todas.length}</p><p className="mt-1 text-xs text-slate-500">Total</p></div><div className="panel p-4"><p className="text-2xl font-extrabold text-[#937000]">{activas.length}</p><p className="mt-1 text-xs text-slate-500">En proceso</p></div><div className="panel p-4"><p className="text-2xl font-extrabold text-emerald-600">{todas.filter(elemento => elemento.estado === 'Entrevista' || elemento.estado === 'Oferta / Contratado').length}</p><p className="mt-1 text-xs text-slate-500">Avanzadas</p></div></div>
    <div className="flex gap-2 overflow-x-auto border-b border-slate-200 pb-2 dark:border-slate-800" role="tablist" aria-label="Filtrar postulaciones">{([['todas', `Todas (${todas.length})`], ['activas', `En proceso (${activas.length})`], ['cerradas', `Finalizadas (${cerradas.length})`]] as const).map(([clave, etiqueta]) => <button type="button" role="tab" aria-selected={pestana === clave} key={clave} onClick={() => establecerPestana(clave)} className={`min-h-11 whitespace-nowrap rounded-xl px-4 text-sm font-semibold ${pestana === clave ? 'bg-[#CD9901] text-[#302817]' : 'text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800'}`}>{etiqueta}</button>)}</div>
    {elementos.length ? <div className="space-y-4">{elementos.map(postulacionActual => { const vacante = vacantes.find(vacanteDisponible => vacanteDisponible.identificador === postulacionActual.idVacante); return vacante ? <ElementoPostulacion key={postulacionActual.identificador} postulacion={postulacionActual} vacante={vacante} /> : null; })}</div> : <div className="panel flex min-h-[300px] flex-col items-center justify-center p-6 text-center"><SearchX size={30} className="text-slate-300" /><p className="mt-3 font-bold">Aún no hay postulaciones en esta sección</p><p className="mt-1 text-sm text-slate-500">Explora vacantes y encuentra una buena oportunidad.</p><button type="button" onClick={alBuscar} className="boton-principal mt-4">Explorar empleos</button></div>}
    <div className="flex flex-wrap items-start gap-3 rounded-2xl border border-[#E6D9AE] bg-[#FEF9DF] p-4 dark:border-[#66522B] dark:bg-[#514324]/30"><MessageCircle size={20} className="shrink-0 text-[#937000]" /><div><p className="text-sm font-semibold text-[#44340D] dark:text-[#F6F0B3]">Alertas de WhatsApp</p><p className="mt-1 text-xs leading-5 text-[#806000] dark:text-[#F6F0B3]">{perfil.aceptaWhatsapp ? 'Tienes activada la preferencia para recibir avisos. Los avisos automáticos requieren conexión al servidor.' : 'Puedes activar los avisos al confirmar una postulación. Los avisos automáticos requieren conexión al servidor.'}</p></div><Check size={16} className="ml-auto text-blue-500" /></div>
    <p className="text-center text-xs text-slate-400"><FileCheck2 className="mr-1 inline" size={13} /> Los datos de esta interfaz se guardan localmente en tu navegador.</p>
  </div>;
}
