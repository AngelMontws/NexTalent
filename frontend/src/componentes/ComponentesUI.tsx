/**
 * NexTalent — ComponentesUI
 * Reúne tarjetas, distintivos, encabezados y elementos visuales reutilizables.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { ChevronRight, MapPin, ShieldCheck } from 'lucide-react';
import { diasEnEtapa, formatearFecha, formatearSalario, validarEnlaceEmpresa } from '../datos';
import type { EstadoPostulacion, Vacante, PerfilUsuario } from '../tipos';

/** Representa el estado de una postulación con una etiqueta visual. */
export const InsigniaEstado = ({ estado: estado }: { estado: EstadoPostulacion; }) => {
  const estiloEstado: Record<EstadoPostulacion, string> = {
    'Enviada': 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200',
    'Vista': 'bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-200',
    'En revisión': 'bg-[#FEF9DF] text-[#745500] dark:bg-[#514324] dark:text-[#F6F0B3]',
    'Entrevista': 'bg-violet-50 text-violet-700 dark:bg-violet-950 dark:text-violet-200',
    'Oferta / Contratado': 'bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-200',
    'No seleccionado': 'bg-rose-50 text-rose-700 dark:bg-rose-950 dark:text-rose-200'
  };
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <span className={`insignia whitespace-nowrap ${estiloEstado[estado]}`}>{estado}</span>;
};
/** Muestra el distintivo de empresa verificada. */
export const EmpresaVerificada = ({ pequeno: pequeno = false }: { pequeno?: boolean; }) => <span className="inline-flex items-center gap-1 text-xs font-medium text-emerald-700 dark:text-emerald-400"><ShieldCheck size={pequeno ? 12 : 14} /> Verificada</span>;
/** Muestra las iniciales como imagen alternativa del perfil. */
export const AvatarIniciales = ({ iniciales: iniciales, tamanio: tamanio = 'md' }: { iniciales: string; tamanio?: 'sm' | 'md' | 'lg'; }) => <span className={`flex shrink-0 items-center justify-center rounded-xl bg-[#F6F0B3] font-bold text-[#745500] dark:bg-[#514324] dark:text-[#F6F0B3] ${tamanio === 'sm' ? 'h-9 w-9 text-xs' : tamanio === 'lg' ? 'h-14 w-14 text-base' : 'h-11 w-11 text-sm'}`}>{iniciales}</span>;
/** Resume una vacante con salario, ubicación y coincidencia de habilidades. */
export const TarjetaVacante = ({ vacante: vacante, perfil: perfil, seleccionadas: seleccionadas, alHacerClic: alHacerClic }: { vacante: Vacante; perfil: PerfilUsuario; seleccionadas: boolean; alHacerClic: () => void; }) => {
  // El candidato puede postularse aunque no tenga las habilidades solicitadas.
  void perfil;  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <article role="button" tabIndex={0} onClick={alHacerClic} onKeyDown={evento => {
    if (evento.target === evento.currentTarget && (evento.key === 'Enter' || evento.key === ' ')) {
      evento.preventDefault(); alHacerClic();
    }
  }} aria-pressed={seleccionadas} className={`block w-full rounded-2xl border p-4 text-left transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#CD9901] sm:p-5 ${seleccionadas ? 'border-[#CD9901] bg-[#FEF9DF]/50 shadow-sm dark:bg-[#514324]/20' : 'border-slate-200 bg-white hover:border-[#D4AF36] hover:shadow-suave dark:border-slate-800 dark:bg-slate-900 dark:hover:border-[#CD9901]'}`}>
    <div className="flex items-start gap-3"><AvatarIniciales iniciales={vacante.inicialesEmpresa} /><div className="min-w-0 flex-1"><div className="flex items-start justify-between gap-2"><h3 className="line-clamp-2 text-sm font-bold leading-snug sm:text-base">{vacante.titulo}</h3><ChevronRight size={17} className="mt-0.5 shrink-0 text-slate-400" /></div><div className="mt-1 flex flex-wrap items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400">{validarEnlaceEmpresa(vacante.sitioWeb) ? <a href={validarEnlaceEmpresa(vacante.sitioWeb)!} target="_blank" rel="noopener noreferrer" onClick={evento => evento.stopPropagation()} onKeyDown={evento => evento.stopPropagation()} className="relative z-10 font-semibold text-[#987200] underline-offset-2 hover:underline focus-visible:underline" title={`Visitar la página web de ${vacante.empresa}`}>{vacante.empresa}</a> : <span>{vacante.empresa}</span>}<ShieldCheck size={13} className="text-emerald-600" aria-label="Empresa verificada" /></div></div></div>
    <div className="mt-4 text-base font-extrabold tracking-tight text-slate-900 dark:text-white">{formatearSalario(vacante.salarioMinimo)} – {formatearSalario(vacante.salarioMaximo)}<span className="ml-1 text-xs font-normal text-slate-500">/ mes</span></div>
    <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-2 text-xs text-slate-500 dark:text-slate-400"><span className="inline-flex items-center gap-1"><MapPin size={13} />{vacante.ubicacion}</span><span>{vacante.modalidad}</span><span>{vacante.jornada}</span></div>
    <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-3 dark:border-slate-800"><span className="text-xs text-slate-500">Publicada {formatearFecha(vacante.fechaCreacion)}</span><span className="text-xs font-semibold text-[#987200]">Ver detalles →</span></div>
  </article>;
};
/** Presenta una métrica de forma compacta. */
export const Indicador = ({ valor: valor, etiqueta: etiqueta }: { valor: string | number; etiqueta: string; }) => <div className="panel p-4 sm:p-5"><p className="text-2xl font-extrabold tracking-tight">{valor}</p><p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{etiqueta}</p></div>;
/** Calcula cuántos días han pasado desde el último cambio de etapa. */
export const AntiguedadEtapa = ({ fecha: fecha }: { fecha: string; }) => <span className="text-xs text-slate-500 dark:text-slate-400">{diasEnEtapa(fecha)} día(s) en esta etapa</span>;
/** Construye el título y subtítulo comunes de una pantalla. */
export const TituloPantalla = ({ sobretitulo: sobretitulo, titulo: titulo, subtitulo: subtitulo }: { sobretitulo?: string; titulo: string; subtitulo?: string; }) => <div className="space-y-1">{sobretitulo && <p className="text-xs font-semibold uppercase tracking-[.14em] text-[#9A7100] dark:text-[#D4AF36]">{sobretitulo}</p>}<h1 className="text-2xl font-extrabold tracking-tight text-slate-950 dark:text-white sm:text-[28px]">{titulo}</h1>{subtitulo && <p className="max-w-2xl text-sm leading-relaxed text-slate-500 dark:text-slate-400">{subtitulo}</p>}</div>;
