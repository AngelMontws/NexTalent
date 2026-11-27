/**
 * NexTalent — BusquedaCandidato
 * Muestra vacantes y filtros, y confirma una postulación con documentos.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { useMemo, useState } from 'react';
import { ArrowLeft, ArrowRight, Bell, BriefcaseBusiness, Check, CheckCircle2, Clock3, MapPin, Search, ShieldCheck, SlidersHorizontal, Sparkles } from 'lucide-react';
import { CATALOGO_HABILIDADES, estaDisponible, formatearSalario, validarEnlaceEmpresa } from '../datos';
import { usarAplicacion } from '../contexto/ContextoAplicacion';
import type { Vacante, ModalidadVacante, JornadaVacante } from '../tipos';
import { AvatarIniciales, TarjetaVacante, TituloPantalla, EmpresaVerificada } from '../componentes/ComponentesUI';
import VentanaModal from '../componentes/VentanaModal';
import CargaDocumentos from '../componentes/CargaDocumentos';
import type { InformacionDocumento } from '../tipos';

/** Establece la estructura de datos de FiltrosBusqueda. */
interface FiltrosBusqueda { modalidad: ModalidadVacante | ''; ubicacion: string; salarioMinimo: number; jornada: JornadaVacante | ''; habilidades: string[]; }
/** Valores por defecto de los filtros de búsqueda. */
const filtrosVacios: FiltrosBusqueda = { modalidad: '', ubicacion: '', salarioMinimo: 0, jornada: '', habilidades: [] };
/** Indica si una vacante cumple el texto y todos los filtros elegidos. */
/** Filtra las vacantes por búsqueda, sueldo, habilidad, modalidad y ubicación. */
const cumpleFiltros = (vacante: Vacante, filtros: FiltrosBusqueda, termino: string) => {
  const texto = `${vacante.titulo} ${vacante.empresa} ${vacante.ubicacion} ${vacante.habilidades.map(habilidad => habilidad.nombre).join(' ')}`.toLowerCase();
  return texto.includes(termino.toLowerCase())
    && (!filtros.ubicacion || vacante.ubicacion === filtros.ubicacion)
    && (!filtros.modalidad || vacante.modalidad === filtros.modalidad)
    && (!filtros.jornada || vacante.jornada === filtros.jornada)
    && filtros.habilidades.every(nombre => vacante.habilidades.some(habilidad => habilidad.nombre === nombre))
    && (!filtros.salarioMinimo || vacante.salarioMaximo >= filtros.salarioMinimo);
};
/** Muestra los filtros y el número de coincidencias para cada alternativa. */
const ControlesFiltros = ({ valor: valor, establecerValor: establecerValor, vacantes: vacantes, termino: termino }: { valor: FiltrosBusqueda; establecerValor: (valor: FiltrosBusqueda) => void; vacantes: Vacante[]; termino: string; }) => {
  /** Ejecuta la operación cantidad dentro del frontend. */
  const cantidad = (campo: Exclude<keyof FiltrosBusqueda, 'habilidades'>, choice: string | number) => vacantes.filter(elemento => cumpleFiltros(elemento, { ...valor, [campo]: choice }, termino)).length;
  const ubicaciones = ['Ciudad de México', 'Estado de México', 'Monterrey', 'Guadalajara', 'Puebla'];
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="space-y-5">
    <div><label className="etiqueta-campo" htmlFor="filter-city">Ubicación</label><select id="filter-city" className="campo" value={valor.ubicacion} onChange={evento => establecerValor({ ...valor, ubicacion: evento.target.value })}><option value="">Todas las ubicaciones</option>{ubicaciones.map(elemento => <option key={elemento} value={elemento}>{elemento} ({cantidad('ubicacion', elemento)})</option>)}</select></div>
    <div><label className="etiqueta-campo" htmlFor="filter-mode">Modalidad</label><select id="filter-mode" className="campo" value={valor.modalidad} onChange={evento => establecerValor({ ...valor, modalidad: evento.target.value as FiltrosBusqueda['modalidad'] })}><option value="">Todas las modalidades</option>{(['Remoto', 'Híbrido', 'Presencial'] as const).map(elemento => <option key={elemento} value={elemento}>{elemento} ({cantidad('modalidad', elemento)})</option>)}</select></div>
    <div><label className="etiqueta-campo" htmlFor="filter-salary">Salario mínimo mensual</label><select id="filter-salary" className="campo" value={valor.salarioMinimo} onChange={evento => establecerValor({ ...valor, salarioMinimo: Number(evento.target.value) })}><option value={0}>Cualquier salario</option>{[10000, 15000, 20000, 25000, 30000].map(elemento => <option value={elemento} key={elemento}>Desde {formatearSalario(elemento)} ({cantidad('salarioMinimo', elemento)})</option>)}</select></div>
    <fieldset className="min-w-0"><legend className="etiqueta-campo">Habilidades</legend>
      <p className="mb-2 text-xs leading-5 text-slate-500">Selecciona varias; la vacante debe incluir todas las elegidas.</p>
      <div className="max-h-44 space-y-1 overflow-y-auto rounded-xl border border-slate-200 p-2 dark:border-slate-700" aria-label="Filtrar por varias habilidades">
        {CATALOGO_HABILIDADES.map(nombre => <label key={nombre} className="flex min-h-10 cursor-pointer items-center gap-2 rounded-lg px-2 hover:bg-slate-50 dark:hover:bg-slate-800"><input type="checkbox" className="h-4 w-4 shrink-0 accent-[#CD9901]" checked={valor.habilidades.includes(nombre)} onChange={evento => establecerValor({ ...valor, habilidades: evento.target.checked ? [...valor.habilidades, nombre] : valor.habilidades.filter(habilidad => habilidad !== nombre) })} /><span className="min-w-0 text-sm">{nombre}</span></label>)}
      </div>
      {valor.habilidades.length > 0 && <p className="mt-2 text-xs font-medium">{valor.habilidades.length} habilidad(es) seleccionada(s)</p>}
    </fieldset>
    <div><label className="etiqueta-campo" htmlFor="filter-shift">Jornada</label><select id="filter-shift" className="campo" value={valor.jornada} onChange={evento => establecerValor({ ...valor, jornada: evento.target.value as FiltrosBusqueda['jornada'] })}><option value="">Todas las jornadas</option>{(['Tiempo completo', 'Medio tiempo', 'Por turnos'] as const).map(elemento => <option key={elemento} value={elemento}>{elemento} ({cantidad('jornada', elemento)})</option>)}</select></div>
  </div>;
};

/** Enseña el salario, requisitos y controles para postularse. */
function DetalleVacante({ vacante: vacante, alRegresar: alRegresar, alPostular: alPostular }: { vacante: Vacante; alRegresar: () => void; alPostular: () => void; }) {
  const { perfil: perfil, postulaciones: postulaciones, idCandidatoActual: idCandidatoActual } = usarAplicacion();
  const postulacionExistente = postulaciones.some(elemento => elemento.idVacante === vacante.identificador && elemento.idCandidato === idCandidatoActual);
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="panel overflow-hidden lg:sticky lg:top-24">
    <div className="border-b border-slate-100 p-5 dark:border-slate-800 sm:p-6">
      <button onClick={alRegresar} className="mb-5 inline-flex min-h-11 items-center gap-2 text-sm font-semibold text-[#9A7100] md:hidden" type="button"><ArrowLeft size={17} /> Volver a resultados</button>
      <div className="flex items-start gap-3"><AvatarIniciales iniciales={vacante.inicialesEmpresa} tamanio="lg" /><div className="min-w-0 flex-1"><h2 className="text-xl font-extrabold leading-tight tracking-tight">{vacante.titulo}</h2><div className="mt-1 flex flex-wrap items-center gap-2 text-sm text-slate-500">{validarEnlaceEmpresa(vacante.sitioWeb) ? <a href={validarEnlaceEmpresa(vacante.sitioWeb)!} target="_blank" rel="noopener noreferrer" className="font-semibold text-[#987200] underline-offset-2 hover:underline focus-visible:underline" title={`Visitar la página web de ${vacante.empresa}`}>{vacante.empresa}</a> : <span>{vacante.empresa}</span>}<EmpresaVerificada pequeno /></div></div></div>
      <div className="mt-5 text-2xl font-extrabold tracking-tight">{formatearSalario(vacante.salarioMinimo)} – {formatearSalario(vacante.salarioMaximo)}</div>
      <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">MXN mensuales · {vacante.tipoSalario.toLowerCase()}</p>
      <div className="mt-4 flex flex-wrap gap-2"><span className="insignia bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200"><MapPin size={13} />{vacante.ubicacion}</span><span className="insignia bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200">{vacante.modalidad}</span><span className="insignia bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200">{vacante.jornada}</span></div>
      <button type="button" disabled={postulacionExistente} className="boton-principal mt-5 w-full" onClick={alPostular}>{postulacionExistente ? <><CheckCircle2 size={17} /> Ya te postulaste</> : <><BriefcaseBusiness size={17} /> Postularme ahora <ArrowRight size={16} /></>}</button>
      <p className="mt-2 text-center text-xs text-slate-400">Adjunta tu CV y documentos profesionales cuando correspondan</p>
    </div>
    <div className="max-h-none space-y-6 p-5 dark:text-slate-200 sm:p-6 2xl:max-h-[calc(100vh-520px)] 2xl:min-h-[280px] 2xl:overflow-auto">
      <section><h3 className="text-sm font-bold">Sobre esta oportunidad</h3><p className="mt-2 text-sm leading-7 text-slate-600 dark:text-slate-300">{vacante.descripcion}</p></section>
      <section><h3 className="text-sm font-bold">Habilidades requeridas</h3><div className="mt-3 flex flex-wrap gap-2">{vacante.habilidades.map(elemento => <span key={elemento.nombre} className={`insignia ${perfil.habilidades.some(habilidadUsuario => habilidadUsuario.nombre === elemento.nombre) ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300' : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'}`}>{perfil.habilidades.some(habilidadUsuario => habilidadUsuario.nombre === elemento.nombre) && <Check size={12} />} {elemento.nombre} · {elemento.nivel}</span>)}</div></section>
      <section><h3 className="text-sm font-bold">Lo que harás</h3><ul className="mt-2 list-inside list-disc space-y-2 text-sm leading-relaxed text-slate-600 dark:text-slate-300">{vacante.responsabilidades.map(elemento => <li key={elemento}>{elemento}</li>)}</ul></section>
      <section><h3 className="text-sm font-bold">Beneficios</h3><ul className="mt-2 space-y-2">{vacante.prestaciones.map(b => <li key={b} className="flex gap-2 text-sm text-slate-600 dark:text-slate-300"><Check size={16} className="shrink-0 text-emerald-600" />{b}</li>)}</ul></section>
      <div className="rounded-xl border border-slate-200 p-3 dark:border-slate-700"><div className="flex items-center gap-2 text-sm font-semibold"><ShieldCheck className="text-emerald-600" size={17} /> Empresa verificada</div><p className="mt-1 text-xs text-slate-500">Índice de respuesta: <strong>{vacante.indiceRespuesta}%</strong> de postulaciones respondidas.</p><p className="mt-1 flex items-center gap-1 text-xs text-slate-500"><Clock3 size={12} /> Vacante activa hasta el {new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'long' }).format(new Date(vacante.fechaVencimiento))}</p></div>
    </div>
  </div>;
}

/** Construye el buscador y abre el formulario de postulación. */
export default function BusquedaCandidato({ alVerPostulaciones: alVerPostulaciones }: { alVerPostulaciones: () => void; }) {
  const { vacantes: vacantes, perfil: perfil, postulaciones: postulaciones, postular: postular, actualizarPerfil: actualizarPerfil, idCandidatoActual: idCandidatoActual } = usarAplicacion();
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [termino, establecerTermino] = useState('');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [filtros, establecerFiltros] = useState<FiltrosBusqueda>(filtrosVacios);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [filtrosAbiertos, establecerFiltrosAbiertos] = useState(false);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [idSeleccionado, establecerIdSeleccionado] = useState<string>('job-1');
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [detalleMovil, establecerDetalleMovil] = useState(false);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [postulando, establecerPostulando] = useState(false);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [folio, establecerFolio] = useState<string | null>(null);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [documentos, establecerDocumentos] = useState<InformacionDocumento[]>([]);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [errorDocumentos, establecerErrorDocumentos] = useState('');
  const activas = useMemo(() => vacantes.filter(estaDisponible), [vacantes]);
  const filtradas = useMemo(() => activas.filter(elemento => cumpleFiltros(elemento, filtros, termino)), [activas, termino, filtros]);
  const elegida = filtradas.find(elemento => elemento.identificador === idSeleccionado) || filtradas[0];
  const totalPostuladas = postulaciones.filter(elemento => elemento.idCandidato === idCandidatoActual).length;
  /** Comprueba el CV obligatorio antes de registrar la solicitud. */
  const realizarPostulacion = async () => { if (!elegida) return; if (!documentos.some(elemento => elemento.tipo === 'cv')) { establecerErrorDocumentos('Selecciona tu currículum vitae antes de postularte.'); return; } const resultado = await postular(elegida.identificador, documentos); if (resultado) { establecerFolio(resultado); establecerPostulando(false); establecerDocumentos([]); establecerErrorDocumentos(''); } };
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="space-y-6">
    <div className="flex flex-wrap items-end justify-between gap-4"><TituloPantalla sobretitulo="Explora oportunidades" titulo={`Hola, ${perfil.nombre.split(' ')[0]} 👋`} subtitulo="Encuentra empleos con salario visible, empresas verificadas y seguimiento real de tu postulación." /><div className="hidden items-center gap-2 rounded-xl bg-[#FEF9DF] px-4 py-3 text-sm text-[#745500] dark:bg-[#514324]/40 dark:text-[#F6F0B3] sm:flex"><Sparkles size={17} /><span><strong>{totalPostuladas}</strong> postulaciones en tu perfil</span></div></div>
    <div className="relative"><Search size={19} className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" /><input aria-label="Buscar por puesto, empresa o habilidad" className="campo h-13 !rounded-2xl !pl-12 !pr-4 !text-sm shadow-suave" value={termino} onChange={evento => establecerTermino(evento.target.value)} placeholder="Puesto, empresa o habilidad…" /></div>
    <div className="md:grid md:grid-cols-[minmax(0,.94fr)_minmax(0,1.06fr)] md:items-start md:gap-4 2xl:grid-cols-[210px_minmax(0,.97fr)_minmax(0,1.03fr)] 2xl:gap-5">
      <aside className="panel hidden p-5 2xl:sticky 2xl:top-24 2xl:block"><div className="mb-5 flex items-center justify-between"><h2 className="font-bold">Filtrar vacantes</h2><SlidersHorizontal size={16} className="text-slate-400" /></div><ControlesFiltros valor={filtros} establecerValor={establecerFiltros} vacantes={activas} termino={termino} /><button type="button" onClick={() => establecerFiltros(filtrosVacios)} className="boton-discreto mt-5 w-full">Limpiar filtros</button></aside>
      <div className={`${detalleMovil ? 'hidden md:block' : 'block'} min-w-0 space-y-3`}>
        <div className="flex items-center justify-between gap-2 pb-1"><p className="text-sm font-semibold">{filtradas.length} vacante{filtradas.length !== 1 && 's'} encontrada{filtradas.length !== 1 && 's'}</p><button type="button" className="boton-secundario !min-h-10 !px-3 2xl:hidden" onClick={() => establecerFiltrosAbiertos(true)}><SlidersHorizontal size={16} /> Filtros {(filtros.habilidades.length + [filtros.ubicacion, filtros.modalidad, filtros.salarioMinimo, filtros.jornada].filter(Boolean).length) > 0 && `(${filtros.habilidades.length + [filtros.ubicacion, filtros.modalidad, filtros.salarioMinimo, filtros.jornada].filter(Boolean).length})`}</button></div>
        {filtradas.length ? filtradas.map(elemento => <TarjetaVacante key={elemento.identificador} vacante={elemento} perfil={perfil} seleccionadas={elegida?.identificador === elemento.identificador} alHacerClic={() => { establecerIdSeleccionado(elemento.identificador); establecerDetalleMovil(true); }} />) : <div className="panel p-8 text-center"><Search className="mx-auto text-slate-300" size={29} /><h3 className="mt-3 font-bold">No encontramos vacantes</h3><p className="mt-1 text-sm text-slate-500">Prueba con otros filtros o una búsqueda más amplia.</p><button type="button" onClick={() => { establecerFiltros(filtrosVacios); establecerTermino('') ;}} className="boton-secundario mt-4">Restablecer búsqueda</button></div>}
      </div>
      <div className={`${detalleMovil ? 'block' : 'hidden'} md:block md:mt-0 min-w-0`}>{elegida ? <DetalleVacante vacante={elegida} alRegresar={() => establecerDetalleMovil(false)} alPostular={() => establecerPostulando(true)} /> : <div className="panel flex min-h-[300px] items-center justify-center p-6 text-sm text-slate-400">Selecciona una vacante para ver sus detalles.</div>}</div>
    </div>
    {filtrosAbiertos && <VentanaModal titulo="Filtros de búsqueda" narrow onClose={() => establecerFiltrosAbiertos(false)}><ControlesFiltros valor={filtros} establecerValor={establecerFiltros} vacantes={activas} termino={termino} /><div className="mt-6 flex gap-2"><button type="button" className="boton-secundario flex-1" onClick={() => establecerFiltros(filtrosVacios)}>Limpiar</button><button type="button" className="boton-principal flex-1" onClick={() => establecerFiltrosAbiertos(false)}>Ver {filtradas.length} resultados</button></div></VentanaModal>}
    {postulando && elegida && <VentanaModal titulo="Confirmar postulación" narrow onClose={() => establecerPostulando(false)}><div className="space-y-5"><div className="rounded-xl bg-slate-50 p-4 dark:bg-slate-800"><p className="text-sm font-bold">{elegida.titulo}</p><p className="mt-1 text-sm text-slate-500">{elegida.empresa} · {formatearSalario(elegida.salarioMinimo)} – {formatearSalario(elegida.salarioMaximo)}</p></div><div><p className="text-xs font-semibold uppercase tracking-wider text-slate-500">Tu perfil se enviará con estos datos</p><p className="mt-3 font-semibold">{perfil.nombre}</p><p className="mt-1 text-sm text-slate-500">{perfil.correo} · {perfil.ubicacion}</p><div className="mt-3 flex flex-wrap gap-1.5">{perfil.habilidades.map(elemento => <span key={elemento.nombre} className="insignia bg-[#FEF9DF] text-[#745500] dark:bg-[#514324] dark:text-[#F6F0B3]">{elemento.nombre}</span>)}</div></div><label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 p-3 text-sm dark:border-slate-700"><input className="mt-1 h-4 w-4 accent-[#CD9901]" type="checkbox" checked={perfil.aceptaWhatsapp} onChange={evento => actualizarPerfil({ aceptaWhatsapp: evento.target.checked })} /><span><strong className="block">Recibir novedades por WhatsApp</strong><span className="mt-1 block text-xs leading-5 text-slate-500">Autorizo mensajes sobre cambios de estatus. Puedo darme de baja cuando quiera. (Simulación: no se enviarán mensajes).</span></span></label><CargaDocumentos alCambiar={establecerDocumentos} />{errorDocumentos && <p role="alert" className="text-sm font-semibold text-red-600">{errorDocumentos}</p>}<button type="button" className="boton-principal w-full" onClick={realizarPostulacion}><CheckCircle2 size={17} /> Confirmar y postularme</button></div></VentanaModal>}
    {folio && <VentanaModal titulo="¡Postulación enviada!" narrow onClose={() => establecerFolio(null)}><div className="space-y-4 text-center"><div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-600"><CheckCircle2 size={31} /></div><p className="text-sm text-slate-600 dark:text-slate-300">Tu perfil se envió correctamente. Guarda este folio para consultar el avance.</p><div className="rounded-xl border border-dashed border-[#D4AF36] bg-[#FEF9DF] p-4 text-lg font-extrabold tracking-[.12em] text-[#745500] dark:border-[#D4AF36] dark:bg-[#514324] dark:text-[#F6F0B3]">{folio}</div><div className="flex gap-2"><button type="button" className="boton-secundario flex-1" onClick={() => establecerFolio(null)}>Seguir buscando</button><button type="button" className="boton-principal flex-1" onClick={() => { establecerFolio(null); alVerPostulaciones() ;}}>Mis postulaciones <ArrowRight size={15} /></button></div><p className="flex items-center justify-center gap-1 text-xs text-slate-400"><Bell size={13} /> Los cambios de estado aparecerán en tu tablero.</p></div></VentanaModal>}
  </div>;
}
