/**
 * NexTalent — PublicacionVacante
 * Permite crear y validar una vacante con salario obligatorio.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { useMemo, useState, type FormEvent } from 'react';
import { ArrowRight, CheckCircle2, CircleHelp, FilePlus2, Info, Plus, Save, ShieldCheck, Trash2 } from 'lucide-react';
import { CATALOGO_HABILIDADES, estaDisponible, formatearSalario } from '../datos';
import { usarAplicacion } from '../contexto/ContextoAplicacion';
import type { ModalidadVacante, JornadaVacante, NuevaVacante, Habilidad, NivelHabilidad } from '../tipos';
import { AvatarIniciales, TituloPantalla, EmpresaVerificada } from '../componentes/ComponentesUI';

const empty: NuevaVacante = { titulo: '', ubicacion: 'Ciudad de México', modalidad: 'Híbrido', jornada: 'Tiempo completo', salarioMinimo: 0, salarioMaximo: 0, tipoSalario: 'Bruto', habilidades: [], descripcion: '', responsabilidades: [''], prestaciones: ['Prestaciones de ley'] };
/** Identificador local para recuperar un borrador de vacante. */
const CLAVE_BORRADOR = 'nextalent_publish_draft';
/** Ejecuta la operación obtenerBorrador dentro del frontend. */
const obtenerBorrador = (): NuevaVacante => { try { const val = localStorage.getItem(CLAVE_BORRADOR); if (val) return { ...empty, ...JSON.parse(val) as NuevaVacante }; } catch { /* ignore */ } return empty; };
/** Edita los datos de la vacante, guarda borradores y valida su publicación. */
export default function PublicacionVacante({ alVerTablero: alVerTablero }: { alVerTablero: () => void; }) {
  const { vacantes: vacantes, publicar: publicar, notificar: notificar, preferenciasReclutador: preferenciasReclutador } = usarAplicacion();
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [formulario, establecerFormulario] = useState<NuevaVacante>(obtenerBorrador);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [nombreHabilidad, establecerNombreHabilidad] = useState('');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [nivelHabilidad, establecerNivelHabilidad] = useState<NivelHabilidad>('Intermedio');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [errores, establecerErrores] = useState<string[]>([]);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [finalizado, establecerFinalizado] = useState(false);
  const propias = vacantes.filter(elemento => elemento.idEmpresa === 'empresa-impulso' || (!elemento.idEmpresa && elemento.empresa === preferenciasReclutador.empresa));
  const activas = useMemo(() => propias.filter(estaDisponible), [propias]);
  /** Actualiza el dato elegido del formulario en memoria. */
  const actualizar = (partial: Partial<NuevaVacante>) => establecerFormulario(elemento => ({ ...elemento, ...partial }));
  /** Ejecuta la operación agregarHabilidad dentro del frontend. */
  const agregarHabilidad = () => {
    if (!nombreHabilidad || formulario.habilidades.some(elemento => elemento.nombre === nombreHabilidad)) return;
    actualizar({ habilidades: [...formulario.habilidades, { nombre: nombreHabilidad, nivel: nivelHabilidad } as Habilidad] });
    establecerNombreHabilidad('');
  };
  /** Ejecuta la operación guardarBorrador dentro del frontend. */
  const guardarBorrador = () => { try { localStorage.setItem(CLAVE_BORRADOR, JSON.stringify(formulario)); notificar('Borrador guardado en este navegador'); } catch { notificar('No se pudo guardar el borrador'); } };
  /** Ejecuta la operación enviarFormulario dentro del frontend. */
  const enviarFormulario = (evento: FormEvent) => {
    evento.preventDefault();
    const erroresNuevos = [
      !formulario.titulo.trim() && 'Escribe el título del puesto.',
      formulario.salarioMinimo <= 0 && 'El salario mínimo debe ser mayor a cero.',
      formulario.salarioMaximo < formulario.salarioMinimo && 'El salario máximo debe ser mayor o igual al mínimo.',
      formulario.salarioMaximo <= 0 && 'El salario máximo debe ser mayor a cero.',
      !formulario.ubicacion.trim() && 'Indica la ubicación.',
      !formulario.descripcion.trim() && 'Añade una descripción.',
      formulario.habilidades.length === 0 && 'Selecciona al menos una habilidad.'
    ].filter(Boolean) as string[];
    establecerErrores(erroresNuevos);
    if (erroresNuevos.length) { window.scrollTo({ top: 0, behavior: 'smooth' }); return; }
    publicar({ ...formulario, responsabilidades: formulario.responsabilidades.filter(Boolean), prestaciones: formulario.prestaciones.filter(Boolean) });
    try { localStorage.removeItem(CLAVE_BORRADOR); } catch { /* ignore */ }
    establecerFormulario(empty); establecerFinalizado(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="space-y-6"><div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end"><TituloPantalla sobretitulo="" titulo="Publicar vacante" subtitulo="Crea oportunidades claras. Todas las publicaciones deben incluir un rango salarial visible." /><span className="inline-flex items-center gap-2 rounded-full bg-emerald-50 px-3 py-2 text-xs font-semibold text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300"><ShieldCheck size={16} /> Empresa autorizada para publicar</span></div>
    {finalizado && <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300"><span className="flex items-center gap-2 font-semibold"><CheckCircle2 size={19} /> Vacante publicada y visible en la búsqueda.</span><button type="button" onClick={() => { establecerFinalizado(false); alVerTablero() ;}} className="inline-flex min-h-10 items-center gap-1 font-bold underline underline-offset-4">Ir a candidatos <ArrowRight size={15} /></button></div>}
    {errores.length > 0 && <div role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-200"><p className="font-bold">Revisa los siguientes campos:</p><ul className="mt-2 list-inside list-disc space-y-1">{errores.map(elemento => <li key={elemento}>{elemento}</li>)}</ul></div>}
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1.65fr)_minmax(300px,0.85fr)]"><form onSubmit={enviarFormulario} className="space-y-5" noValidate>
      <section className="panel p-5 sm:p-6"><div className="mb-5 flex items-center gap-3"><span className="flex h-9 w-9 items-center justify-center rounded-xl bg-[#FEF9DF] text-[#937000] dark:bg-[#514324]"><FilePlus2 size={19} /></span><div><h2 className="font-bold">Información principal</h2><p className="text-xs text-slate-500">Datos que verá cada candidato.</p></div></div>
        <div className="space-y-4"><div><label htmlFor="job-title" className="etiqueta-campo">Título del puesto *</label><input id="job-title" className="campo" placeholder="Ej. Desarrollador Frontend Jr." value={formulario.titulo} onChange={evento => actualizar({ titulo: evento.target.value })} /></div><div><label htmlFor="job-desc" className="etiqueta-campo">Descripción de la vacante *</label><textarea id="job-desc" rows={4} className="campo resize-y" placeholder="Describe el propósito del puesto, el equipo y las actividades principales…" value={formulario.descripcion} onChange={evento => actualizar({ descripcion: evento.target.value })} /></div><div className="grid gap-4 sm:grid-cols-2"><div><label htmlFor="job-location" className="etiqueta-campo">Ubicación *</label><input id="job-location" className="campo" value={formulario.ubicacion} onChange={evento => actualizar({ ubicacion: evento.target.value })} /></div><div><label htmlFor="job-mode" className="etiqueta-campo">Modalidad *</label><select id="job-mode" className="campo" value={formulario.modalidad} onChange={evento => actualizar({ modalidad: evento.target.value as ModalidadVacante })}>{(['Presencial', 'Híbrido', 'Remoto'] as const).map(elemento => <option key={elemento}>{elemento}</option>)}</select></div></div><div><label htmlFor="job-shift" className="etiqueta-campo">Jornada *</label><select id="job-shift" className="campo" value={formulario.jornada} onChange={evento => actualizar({ jornada: evento.target.value as JornadaVacante })}>{(['Tiempo completo', 'Medio tiempo', 'Por turnos'] as const).map(elemento => <option key={elemento}>{elemento}</option>)}</select></div></div>
      </section>
      <section className="panel p-5 sm:p-6"><div className="mb-5 flex items-center justify-between"><div><h2 className="font-bold">Rango salarial <span className="text-rose-500">*</span></h2><p className="mt-1 text-xs text-slate-500">Obligatorio en todas las vacantes.</p></div><span className="insignia bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300">Transparencia</span></div><div className="grid gap-4 sm:grid-cols-2"><div><label htmlFor="salary-min" className="etiqueta-campo">Salario mínimo (MXN / mes)</label><input id="salary-min" type="number" min="1" step="500" value={formulario.salarioMinimo || ''} onChange={evento => actualizar({ salarioMinimo: Number(evento.target.value) })} placeholder="18000" className="campo" /></div><div><label htmlFor="salary-max" className="etiqueta-campo">Salario máximo (MXN / mes)</label><input id="salary-max" type="number" min="1" step="500" value={formulario.salarioMaximo || ''} onChange={evento => actualizar({ salarioMaximo: Number(evento.target.value) })} placeholder="24000" className="campo" /></div></div><div className="mt-4"><label htmlFor="salary-type" className="etiqueta-campo">Tipo de salario</label><select id="salary-type" className="campo" value={formulario.tipoSalario} onChange={evento => actualizar({ tipoSalario: evento.target.value as NuevaVacante['tipoSalario'] })}><option value="Bruto">Bruto</option><option value="Neto">Neto</option></select></div><p className="mt-3 flex items-center gap-2 text-xs text-slate-500"><Info size={14} className="shrink-0" /> El sueldo será público. El mínimo debe ser mayor a 0 y el máximo no puede ser inferior.</p></section>
      <section className="panel p-5 sm:p-6"><h2 className="font-bold">Habilidades requeridas *</h2><p className="mt-1 text-xs text-slate-500">Selecciona habilidades del catálogo con su nivel esperado.</p><div className="mt-4 grid grid-cols-1 gap-2 sm:grid-cols-[1fr_145px_auto]"><select aria-label="Habilidad" className="campo" value={nombreHabilidad} onChange={evento => establecerNombreHabilidad(evento.target.value)}><option value="">Seleccionar habilidad</option>{CATALOGO_HABILIDADES.map(elemento => <option key={elemento}>{elemento}</option>)}</select><select aria-label="Nivel de habilidad" className="campo" value={nivelHabilidad} onChange={evento => establecerNivelHabilidad(evento.target.value as NivelHabilidad)}>{(['Básico', 'Intermedio', 'Avanzado'] as const).map(elemento => <option key={elemento}>{elemento}</option>)}</select><button type="button" className="boton-secundario" disabled={!nombreHabilidad} onClick={agregarHabilidad}><Plus size={16} /> Añadir</button></div>{formulario.habilidades.length ? <div className="mt-4 flex flex-wrap gap-2">{formulario.habilidades.map(sk => <span key={sk.nombre} className="insignia bg-[#FEF9DF] text-[#806000] dark:bg-[#514324] dark:text-[#F6F0B3]">{sk.nombre} · {sk.nivel}<button type="button" onClick={() => actualizar({ habilidades: formulario.habilidades.filter(elemento => elemento.nombre !== sk.nombre) })} aria-label={`Quitar ${sk.nombre}`} className="ml-1 inline-flex h-6 w-6 items-center justify-center rounded-full hover:bg-[#F6F0B3] dark:hover:bg-[#302817]"><Trash2 size={12} /></button></span>)}</div> : <p className="mt-4 text-xs text-slate-400">Aún no has agregado habilidades.</p>}</section>
      <section className="panel p-5 sm:p-6"><h2 className="font-bold">Actividades y beneficios</h2><p className="mt-1 text-xs text-slate-500">Información adicional (opcional).</p><div className="mt-4 space-y-4"><div><label htmlFor="job-activities" className="etiqueta-campo">Actividades principales (una por línea)</label><textarea id="job-activities" rows={3} className="campo" placeholder="Colaborar con el equipo..." value={formulario.responsabilidades.join('\n')} onChange={evento => actualizar({ responsabilidades: evento.target.value.split('\n') })} /></div><div><label htmlFor="job-benefits" className="etiqueta-campo">Beneficios (uno por línea)</label><textarea id="job-benefits" rows={3} className="campo" value={formulario.prestaciones.join('\n')} onChange={evento => actualizar({ prestaciones: evento.target.value.split('\n') })} /></div></div></section>
      <div className="flex flex-col-reverse justify-end gap-3 sm:flex-row"><button type="button" className="boton-secundario" onClick={guardarBorrador}><Save size={16} /> Guardar borrador</button><button type="submit" className="boton-principal"><Plus size={18} /> Publicar vacante</button></div>
    </form>
      <aside className="space-y-4 xl:sticky xl:top-24"><div className="panel p-5"><div className="flex items-center gap-3"><AvatarIniciales iniciales="ID" /><div><p className="text-sm font-bold">{preferenciasReclutador.empresa}</p><EmpresaVerificada pequeno /></div></div><div className="mt-5 border-t border-slate-100 pt-4 dark:border-slate-800"><h3 className="text-sm font-bold">Vista previa</h3><p className="mt-3 text-sm font-bold">{formulario.titulo || 'Título de tu vacante'}</p><p className="mt-1 text-xs text-slate-500">{formulario.ubicacion} · {formulario.modalidad}</p><p className="mt-3 text-base font-extrabold text-[#937000]">{formulario.salarioMinimo > 0 && formulario.salarioMaximo > 0 ? `${formatearSalario(formulario.salarioMinimo)} – ${formatearSalario(formulario.salarioMaximo)}` : 'Rango salarial pendiente'}</p><p className="mt-1 text-xs text-slate-500">MXN / mes · {formulario.tipoSalario.toLowerCase()}</p><div className="mt-3 flex flex-wrap gap-1.5">{formulario.habilidades.slice(0, 5).map(elemento => <span key={elemento.nombre} className="insignia bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-200">{elemento.nombre}</span>)}</div></div></div><div className="rounded-2xl border border-[#E6D9AE] bg-[#FEF9DF] p-5 dark:border-[#66522B] dark:bg-[#514324]/30"><div className="flex gap-2"><CircleHelp size={19} className="shrink-0 text-[#937000]" /><div><p className="text-sm font-bold text-[#44340D] dark:text-[#F6F0B3]">¿Cuándo vence una vacante?</p><p className="mt-2 text-xs leading-6 text-[#614900] dark:text-[#F6F0B3]">Se cerrará automáticamente a los 30 días si no se renueva. Los candidatos pendientes deben recibir respuesta al cierre (integración futura con API).</p></div></div></div><div className="panel p-5"><p className="text-sm font-semibold">Tus vacantes activas</p><p className="mt-3 text-3xl font-extrabold">{activas.length}</p><p className="mt-1 text-xs text-slate-500">{propias.length} publicaciones totales</p></div></aside>
    </div>
  </div>;
}
