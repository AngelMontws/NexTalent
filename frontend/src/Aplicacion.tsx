/**
 * NexTalent — Aplicacion
 * Organiza las pantallas, el menú según el rol de la sesión y el modo oscuro.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { useEffect, useState } from 'react';
import { Bell, Check, CircleUserRound, Settings2, MessageCircle, ClipboardList, Compass, LayoutDashboard, Menu, Moon, PlusCircle, ShieldCheck, Sun, X, LogOut } from 'lucide-react';
import BusquedaCandidato from './paginas/BusquedaCandidato';
import PostulacionesCandidato from './paginas/PostulacionesCandidato';
import PublicacionVacante from './paginas/PublicacionVacante';
import TableroReclutador from './paginas/TableroReclutador';
import ConfiguracionPerfil from './paginas/ConfiguracionPerfil';
import NotificacionesReclutador from './paginas/NotificacionesReclutador';
import { usarAplicacion } from './contexto/ContextoAplicacion';
import PantallaAutenticacion from './componentes/PantallaAutenticacion';
import type { Sesion } from './api/sesion';
import type { RolUsuario } from './tipos';

/** Establece la estructura de datos de Pagina. */
type Pagina = 'buscar' | 'postulaciones' | 'publicar' | 'kanban' | 'perfil' | 'notificaciones';
/** Opciones de navegación disponibles según el tipo de usuario. */
const MENUS_POR_ROL: Record<RolUsuario, { pagina: Pagina; etiqueta: string; icon: typeof Compass; }[]> = {
  candidato: [{ pagina: 'buscar', etiqueta: 'Buscar vacantes', icon: Compass }, { pagina: 'postulaciones', etiqueta: 'Mis postulaciones', icon: ClipboardList }, { pagina: 'perfil', etiqueta: 'Mi cuenta', icon: Settings2 }],
  reclutador: [{ pagina: 'kanban', etiqueta: 'Postulaciones', icon: LayoutDashboard }, { pagina: 'publicar', etiqueta: 'Publicar vacante', icon: PlusCircle }, { pagina: 'notificaciones', etiqueta: 'Notificaciones', icon: MessageCircle }, { pagina: 'perfil', etiqueta: 'Mi cuenta', icon: Settings2 }]
};
/** Primera pantalla de cada rol. */
const INICIO_POR_ROL: Record<RolUsuario, Pagina> = { candidato: 'buscar', reclutador: 'kanban' };
/** Ejecuta la operación leerModoOscuro dentro del frontend. */
const leerModoOscuro = () => { try { return localStorage.getItem('nextalent-dark') === 'true'; } catch { return false; } };
/** Muestra el logotipo y el nombre de NexTalent. */
function Marca({ compacta = false, alPulsar }: { compacta?: boolean; alPulsar: () => void; }) {
  // El logo regresa a la primera opción del rol con sesión iniciada.
  return <button type="button" onClick={alPulsar} aria-label="Ir al inicio de tu espacio de trabajo" className="flex items-center gap-2.5 rounded-xl border-0 bg-transparent p-1 text-left text-inherit transition hover:bg-[#F6F0B3]/40 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#CD9901]"><img src="/nextalent-logo.png" alt="Logo NexTalent" className="h-11 w-11 shrink-0 object-contain" />{!compacta && <div><div className="text-lg font-extrabold tracking-tight">Nex<span className="text-[#9A7100] dark:text-[#D4AF36]">Talent</span></div><div className="-mt-1 text-[9px] font-semibold uppercase tracking-[.17em] text-slate-400">Tu siguiente oportunidad</div></div>}</button>;
}
/** Controla la navegación del sistema y muestra la pantalla correspondiente al rol. */
export default function Aplicacion() {
  const { sesion, entrar, salir, perfil, postulaciones, avisoTemporal, notificacionesReclutador, preferenciasReclutador, idCandidatoActual } = usarAplicacion();
  // El rol sale del token de Identidad: no se puede cambiar desde la pantalla.
  const rol: RolUsuario = sesion?.rol ?? 'candidato';
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [pagina, establecerPagina] = useState<Pagina>(INICIO_POR_ROL[rol]);
  /** Guarda la sesión y abre la primera pantalla del rol. */
  const iniciarSesion = (nueva: Sesion) => { entrar(nueva); establecerPagina(INICIO_POR_ROL[nueva.rol]); };
  /** Cierra la sesión y borra el token de esta pestaña. */
  const cerrarSesion = () => { salir(); establecerMenuMovil(false); };
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [oscuro, establecerOscuro] = useState(leerModoOscuro);
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [menuMovil, establecerMenuMovil] = useState(false);
  // Efecto: sincroniza un cambio de datos con la interfaz o el navegador.
  useEffect(() => { document.documentElement.classList.toggle('dark', oscuro); try { localStorage.setItem('nextalent-dark', String(oscuro)); } catch { /* ignore */ } }, [oscuro]);
  /** Ejecuta la operación navegar dentro del frontend. */
  const navegar = (siguiente: Pagina) => { establecerPagina(siguiente); establecerMenuMovil(false); window.scrollTo({ top: 0, behavior: 'smooth' }); };
  /** Al pulsar el logo, abre la primera sección del candidato o del reclutador. */
  const irAlInicio = () => navegar(INICIO_POR_ROL[rol]);
  const sinLeer = rol === 'candidato' ? postulaciones.filter(elemento => elemento.idCandidato === idCandidatoActual && elemento.estado === 'En revisión').length : notificacionesReclutador.filter(elemento => !elemento.leida).length;
  const navegacionActual = MENUS_POR_ROL[rol];
  if (!sesion) return <PantallaAutenticacion alAcceder={iniciarSesion} oscuro={oscuro} alCambiarModoOscuro={() => establecerOscuro(valor => !valor)} />;
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="min-h-screen min-w-0 lg:pl-[246px]">
    <aside className="fixed inset-y-0 left-0 z-40 hidden w-[246px] flex-col border-r border-slate-200 bg-white px-4 py-6 dark:border-slate-800 dark:bg-slate-900 lg:flex">
      <div className="px-2"><Marca alPulsar={irAlInicio} /></div>
      <div className="mt-10 px-2 text-[10px] font-extrabold uppercase tracking-[.18em] text-slate-400">Espacio de trabajo</div>
      <nav className="mt-3 space-y-1" aria-label="Navegación principal">{navegacionActual.map(item => <button type="button" key={item.pagina} onClick={() => navegar(item.pagina)} aria-current={pagina === item.pagina ? 'page' : undefined} className={`flex min-h-12 w-full items-center gap-3 rounded-xl px-3 text-left text-sm font-semibold transition ${pagina === item.pagina ? 'bg-[#F6F0B3]/65 text-[#745500] dark:bg-[#514324] dark:text-[#F6F0B3]' : 'text-slate-500 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'}`}><item.icon size={19} />{item.etiqueta}{item.pagina === 'postulaciones' && <span className="ml-auto rounded-full bg-white px-2 py-0.5 text-xs text-[#745500] shadow-sm dark:bg-[#514324] dark:text-[#F6F0B3]">{postulaciones.filter(elemento => elemento.idCandidato === idCandidatoActual).length}</span>}</button>)}</nav>
      <div className="mt-auto space-y-3"><div className="rounded-xl bg-slate-50 p-3 dark:bg-slate-800"><div className="flex items-center gap-2"><ShieldCheck size={17} className="text-emerald-600" /><span className="text-xs font-semibold">Transparencia primero</span></div><p className="mt-2 text-[11px] leading-5 text-slate-500 dark:text-slate-400">Salarios visibles, empresas verificadas y estados de postulación claros.</p></div><button type="button" onClick={cerrarSesion} className="boton-discreto w-full justify-start !text-xs"><LogOut size={15} /> Cerrar sesión</button></div>
    </aside>
    <header className="sticky top-0 z-30 flex h-[70px] items-center justify-between gap-3 border-b border-slate-200 bg-white/95 px-4 backdrop-blur-xl dark:border-slate-800 dark:bg-slate-900/95 sm:px-6 lg:px-8">
      <div className="flex min-w-0 items-center gap-2 sm:gap-3"><div className="lg:hidden"><Marca compacta alPulsar={irAlInicio} /></div><div className="hidden lg:block"><div className="mt-0.5 text-sm font-bold">{rol === 'candidato' ? 'Tu siguiente paso profesional' : 'Gestión de talento'}</div></div></div>
      <div className="flex shrink-0 items-center gap-1 sm:gap-2"><button type="button" className="boton-discreto !h-11 !w-11 !p-0" onClick={() => establecerOscuro(valor => !valor)} aria-label={oscuro ? 'Activar modo claro' : 'Activar modo oscuro'}>{oscuro ? <Sun size={19} /> : <Moon size={19} />}</button><button type="button" onClick={() => navegar(rol === 'candidato' ? 'postulaciones' : 'notificaciones')} className="boton-discreto relative !h-11 !w-11 !p-0" aria-label="Ver actividad"><Bell size={19} />{sinLeer > 0 && <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-[#CD9901]" />}</button><div className="ml-1 hidden h-9 w-px bg-slate-200 dark:bg-slate-700 sm:block" /><button type="button" className="boton-discreto hidden !text-xs sm:inline-flex" onClick={cerrarSesion}><LogOut size={15} /> Salir</button><button type="button" onClick={() => navegar('perfil')} aria-label="Abrir mi perfil" title="Mi cuenta" className="ml-1 flex items-center gap-2 rounded-xl p-1 transition hover:bg-[#F6F0B3]/60 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#CD9901]"><div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200">{(rol === 'candidato' ? perfil.datosFotografia : preferenciasReclutador.datosFotografia) ? <img src={rol === 'candidato' ? perfil.datosFotografia : preferenciasReclutador.datosFotografia} alt="Avatar" className="h-full w-full rounded-full object-cover" /> : <CircleUserRound size={20} />} </div><div className="hidden max-w-36 sm:block"><p className="truncate text-xs font-bold">{rol === 'candidato' ? perfil.nombre : preferenciasReclutador.empresa}</p><p className="text-[10px] text-slate-400">{rol === 'candidato' ? 'Candidato' : 'Empresa verificada'}</p></div></button><button type="button" className="boton-discreto !h-11 !w-11 !p-0 lg:hidden" onClick={() => establecerMenuMovil(valor => !valor)} aria-label="Abrir menú" aria-expanded={menuMovil}>{menuMovil ? <X size={21} /> : <Menu size={21} />}</button></div>
    </header>
    {menuMovil && <div className="fixed inset-x-3 top-[76px] z-40 rounded-2xl border border-slate-200 bg-white p-4 shadow-xl dark:border-slate-700 dark:bg-slate-900 lg:hidden"><button type="button" className="boton-discreto w-full text-xs" onClick={cerrarSesion}><LogOut size={14} /> Cerrar sesión</button></div>}
    <main className="mx-auto max-w-[1640px] min-w-0 px-4 pb-28 pt-6 sm:px-6 lg:px-7 lg:pb-12 lg:pt-8 xl:px-9" id="main">
      {pagina === 'buscar' && <BusquedaCandidato alVerPostulaciones={() => navegar('postulaciones')} />}
      {pagina === 'postulaciones' && <PostulacionesCandidato alBuscar={() => navegar('buscar')} />}
      {pagina === 'publicar' && <PublicacionVacante alVerTablero={() => navegar('kanban')} />}
      {pagina === 'kanban' && <TableroReclutador alPublicar={() => navegar('publicar')} />}
      {pagina === 'perfil' && <ConfiguracionPerfil key={rol} rol={rol} />}
      {pagina === 'notificaciones' && <NotificacionesReclutador alConfigurar={() => navegar('perfil')} />}
      <footer className="mt-10 flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-5 text-xs text-slate-400 dark:border-slate-800"><span>© NexTalent · Bolsa de trabajo</span><span>{sesion.correo}</span></footer>
    </main>
    <nav aria-label="Navegación móvil" className="fixed inset-x-0 bottom-0 z-30 flex min-h-[74px] items-center justify-around gap-1 border-t border-slate-200 bg-white/95 px-2 py-1 pb-[calc(6px+env(safe-area-inset-bottom))] backdrop-blur-lg dark:border-slate-800 dark:bg-slate-900/95 lg:hidden">{navegacionActual.map(item => <button key={item.pagina} type="button" onClick={() => navegar(item.pagina)} aria-current={pagina === item.pagina ? 'page' : undefined} className={`flex min-h-[60px] min-w-0 flex-1 flex-col items-center justify-center gap-1 rounded-xl text-[10px] font-semibold ${pagina === item.pagina ? 'text-[#9A7100] dark:text-[#D4AF36]' : 'text-slate-400'}`}><item.icon size={21} /><span className="max-w-full whitespace-normal break-words px-0.5 text-center leading-3">{item.etiqueta}</span></button>)}</nav>
    {avisoTemporal && <div role="status" aria-live="polite" className="fixed bottom-24 right-4 z-[60] flex max-w-[min(94vw,410px)] items-start gap-3 rounded-xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white shadow-xl lg:bottom-6"><span className="flex h-6 w-6 items-center justify-center rounded-full bg-emerald-600"><Check size={15} /></span><span>{avisoTemporal}</span></div>}
  </div>;
}
