/**
 * NexTalent — ContextoAplicacion
 * Guarda la sesión y el estado compartido de vacantes, postulaciones, perfiles y avisos.
 * Las postulaciones vienen de Postulaciones (Backend 2) y se actualizan solas por SignalR.
 * Vacantes y perfiles siguen locales hasta que Identidad y Vacantes (Backend 1) expongan sus endpoints.
 */
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { dentroDeDias, perfilInicial } from '../datos';
import { VACANTES_TEMPORALES } from '../api/catalogoTemporal';
import { mensajeDeError, registrarAlVencerSesion } from '../api/cliente';
import * as apiNotificaciones from '../api/notificaciones';
import * as apiPostulaciones from '../api/postulaciones';
import { conectarTiempoReal } from '../api/tiempoReal';
import { borrarSesion, guardarSesion, leerSesionGuardada, type Sesion } from '../api/sesion';
import type { Postulacion, EstadoPostulacion, InformacionDocumento, Vacante, NuevaVacante, PerfilUsuario, NotificacionReclutador, PreferenciasReclutador } from '../tipos';

/** Establece la estructura de datos de EstadoAplicacion. */
interface EstadoAplicacion {
  sesion: Sesion | null;
  entrar: (sesion: Sesion) => void;
  salir: () => void;
  vacantes: Vacante[];
  postulaciones: Postulacion[];
  cargandoPostulaciones: boolean;
  perfil: PerfilUsuario;
  /** Identificador de la cuenta actual (sub del JWT). */
  idCandidatoActual: string;
  /** Empresa del reclutador (empresa_id del JWT). */
  idEmpresaActual: string | null;
  postular: (idVacante: string, documentos?: InformacionDocumento[]) => Promise<string | null>;
  publicar: (datos: NuevaVacante) => void;
  moverPostulaciones: (idsPostulaciones: string[], estado: EstadoPostulacion, motivo?: string, mensajeCandidato?: string) => Promise<void>;
  abrirPerfilCandidato: (idPostulacion: string) => Promise<void>;
  actualizarPerfil: (siguiente: Partial<PerfilUsuario>) => void;
  guardarPerfil: (siguiente: Partial<PerfilUsuario>) => void;
  preferenciasReclutador: PreferenciasReclutador;
  actualizarPreferenciasReclutador: (siguiente: Partial<PreferenciasReclutador>) => void;
  notificacionesReclutador: NotificacionReclutador[];
  marcarNotificacionesLeidas: () => void;
  marcarNotificacionLeida: (identificador: string) => void;
  avisoTemporal: string | null;
  notificar: (mensaje: string) => void;
}
const ContextoAplicacion = createContext<EstadoAplicacion | null>(null);

/** Clave local de las vacantes publicadas en este navegador (hasta que exista la API de Vacantes). */
const CLAVE_VACANTES = 'nextalent-vacantes-locales';
/** Clave local de los datos de perfil no sensibles, por cuenta (hasta que exista la API de perfiles). */
const CLAVE_PERFILES = 'nextalent-perfiles-locales';

const leerJson = <T,>(clave: string, valor: T): T => {
  try { const guardado = localStorage.getItem(clave); return guardado ? JSON.parse(guardado) as T : valor; } catch { return valor; }
};
const guardarJson = (clave: string, valor: unknown) => {
  try { localStorage.setItem(clave, JSON.stringify(valor)); } catch { /* El navegador puede bloquear el almacenamiento local. */ }
};
/** Nombre provisional a partir del correo, mientras Identidad no devuelva el perfil. */
const nombreDesdeCorreo = (correo: string) => correo.split('@')[0].replace(/[._-]+/g, ' ').replace(/\b\w/g, letra => letra.toUpperCase()) || 'Usuario';

/** Comparte los datos y las acciones con todas las pantallas. */
export function ProveedorAplicacion({ children }: { children: ReactNode; }) {
  const [sesion, establecerSesion] = useState<Sesion | null>(leerSesionGuardada);
  const [vacantesLocales, establecerVacantesLocales] = useState<Vacante[]>(() => leerJson<Vacante[]>(CLAVE_VACANTES, []));
  const [postulaciones, establecerPostulaciones] = useState<Postulacion[]>([]);
  const [cargandoPostulaciones, establecerCargando] = useState(false);
  const [perfilesLocales, establecerPerfilesLocales] = useState<Record<string, Partial<PerfilUsuario>>>(() => leerJson(CLAVE_PERFILES, {}));
  const [notificacionesReclutador, establecerNotificacionesReclutador] = useState<NotificacionReclutador[]>([]);
  const [avisoTemporal, establecerAvisoTemporal] = useState<string | null>(null);

  const vacantes = useMemo(() => [...vacantesLocales, ...VACANTES_TEMPORALES], [vacantesLocales]);
  const idCandidatoActual = sesion?.idUsuario ?? '';
  const idEmpresaActual = sesion?.idEmpresa ?? null;
  const empresaActual = VACANTES_TEMPORALES.find(v => v.idEmpresa === idEmpresaActual)?.empresa ?? 'Mi empresa';

  const notificar = useCallback((mensaje: string) => establecerAvisoTemporal(mensaje), []);
  useEffect(() => { if (!avisoTemporal) return; const temporizador = window.setTimeout(() => establecerAvisoTemporal(null), 4400); return () => window.clearTimeout(temporizador); }, [avisoTemporal]);
  useEffect(() => guardarJson(CLAVE_VACANTES, vacantesLocales), [vacantesLocales]);
  useEffect(() => guardarJson(CLAVE_PERFILES, perfilesLocales), [perfilesLocales]);

  /** Datos públicos del perfil: base + lo editado en este navegador. Nunca se guardan CURP, domicilio ni foto. */
  const perfil: PerfilUsuario = useMemo(() => ({
    ...perfilInicial, identificador: idCandidatoActual, correo: sesion?.correo ?? '', celular: '',
    nombre: sesion ? nombreDesdeCorreo(sesion.correo) : perfilInicial.nombre, titulo: '',
    ...(perfilesLocales[idCandidatoActual] ?? {})
  }), [sesion, idCandidatoActual, perfilesLocales]);

  const entrar = useCallback((nueva: Sesion) => { guardarSesion(nueva); establecerSesion(nueva); }, []);
  const salir = useCallback(() => {
    borrarSesion(); establecerSesion(null); establecerPostulaciones([]); establecerNotificacionesReclutador([]);
  }, []);
  // Si el backend responde 401 (token vencido), se regresa al inicio de sesión.
  useEffect(() => registrarAlVencerSesion(() => { salir(); notificar('Tu sesión terminó. Vuelve a iniciar sesión.'); }), [salir, notificar]);

  /** Trae del servidor las postulaciones de la persona o de las vacantes de su empresa. */
  const nombrePerfil = perfil.nombre;
  const recargar = useCallback(async () => {
    if (!sesion) return;
    establecerCargando(true);
    try {
      if (sesion.rol === 'candidato') {
        establecerPostulaciones(await apiPostulaciones.listarMias(sesion.idUsuario, nombrePerfil));
      } else {
        const propias = VACANTES_TEMPORALES.filter(v => v.idEmpresa === sesion.idEmpresa);
        const listas = await Promise.all(propias.map(v => apiPostulaciones.listarCandidatos(v.identificador).catch(() => [])));
        establecerPostulaciones(listas.flat());
      }
    } catch (error) {
      notificar(mensajeDeError(error));
    } finally {
      establecerCargando(false);
    }
  }, [sesion, nombrePerfil, notificar]);
  useEffect(() => { void recargar(); }, [recargar]);

  /** NT-041: la bandeja del reclutador vive en Notificaciones; aquí solo se muestra. */
  const recargarBandeja = useCallback(async () => {
    if (sesion?.rol !== 'reclutador') return;
    try {
      const delServidor = await apiNotificaciones.listarAvisos();
      // Conserva los avisos que llegaron por SignalR y que Notificaciones aún no termina de guardar.
      establecerNotificacionesReclutador(anteriores => [
        ...anteriores.filter(n => n.identificador.startsWith('nuevo-') && !delServidor.some(s => s.idPostulacion === n.idPostulacion)),
        ...delServidor
      ]);
    } catch (error) {
      notificar(mensajeDeError(error));
    }
  }, [sesion, notificar]);
  useEffect(() => { void recargarBandeja(); }, [recargarBandeja]);

  // Tiempo real: los avisos llegan por SignalR y se vuelve a pedir la lista para mostrar el estado exacto.
  const recargarRef = useRef(recargar);
  recargarRef.current = recargar;
  const recargarBandejaRef = useRef(recargarBandeja);
  recargarBandejaRef.current = recargarBandeja;
  useEffect(() => {
    if (!sesion) return;
    return conectarTiempoReal(sesion.token, {
      alCambiarEstatus: aviso => {
        void recargarRef.current();
        if (sesion.rol === 'candidato') notificar(`${aviso.tituloVacante}: ahora estás en «${apiPostulaciones.estadoEnPantalla(aviso.estadoNuevo)}»`);
      },
      alCrearPostulacion: aviso => {
        void recargarRef.current();
        // Se muestra al instante y luego se reemplaza por el aviso guardado en Notificaciones.
        establecerNotificacionesReclutador(anteriores => anteriores.some(n => n.idPostulacion === aviso.postulacionId) ? anteriores : [{
          identificador: `nuevo-${aviso.postulacionId}`, idPostulacion: aviso.postulacionId, candidato: 'Nueva postulación',
          tituloVacante: aviso.tituloVacante, empresa: empresaActual, folio: aviso.folio, fechaCreacion: aviso.creadaEn, leida: false
        }, ...anteriores]);
        notificar(`Nueva postulación · ${aviso.folio} · ${aviso.tituloVacante}`);
        window.setTimeout(() => void recargarBandejaRef.current(), 1500);
      },
      alReconectar: () => { void recargarRef.current(); void recargarBandejaRef.current(); }
    });
  }, [sesion, notificar, empresaActual]);

  /** HU-05.01: postula en el servidor y devuelve el folio. Los documentos todavía no se suben. */
  const postular = useCallback(async (idVacante: string): Promise<string | null> => {
    if (!sesion) { notificar('Inicia sesión para postularte'); return null; }
    try {
      const folio = await apiPostulaciones.postular(idVacante);
      notificar(`Postulación enviada · ${folio}`);
      void recargar();
      return folio;
    } catch (error) {
      notificar(mensajeDeError(error));
      return null;
    }
  }, [sesion, notificar, recargar]);

  /** HU-06.02 y HU-06.03: mueve una o varias postulaciones; cada una se valida en el servidor. */
  const moverPostulaciones = useCallback(async (identificadores: string[], estado: EstadoPostulacion, motivo?: string, mensajeCandidato?: string) => {
    const resultados = await Promise.allSettled(identificadores.map(id => apiPostulaciones.moverEtapa(id, estado, motivo, mensajeCandidato)));
    const fallidas = resultados.filter((r): r is PromiseRejectedResult => r.status === 'rejected');
    const correctas = resultados.length - fallidas.length;
    if (fallidas.length === 0) notificar(`${correctas} postulación${correctas !== 1 ? 'es' : ''} movida${correctas !== 1 ? 's' : ''} a «${estado}»`);
    else notificar(`${correctas} de ${resultados.length} movidas. ${mensajeDeError(fallidas[0].reason)}`);
    await recargar();
  }, [notificar, recargar]);

  /** HU-06.01: al abrir el perfil, una postulación "Enviada" pasa a "Vista". */
  const abrirPerfilCandidato = useCallback(async (idPostulacion: string) => {
    if (postulaciones.find(p => p.identificador === idPostulacion)?.estado !== 'Enviada') return;
    try { await apiPostulaciones.abrirPerfil(idPostulacion); await recargar(); } catch (error) { notificar(mensajeDeError(error)); }
  }, [postulaciones, recargar, notificar]);

  /** TEMPORAL: la vacante se guarda en este navegador hasta que exista POST /api/vacantes (Backend 1). */
  const publicar = useCallback((datos: NuevaVacante) => {
    if (datos.salarioMinimo <= 0 || datos.salarioMaximo < datos.salarioMinimo || !datos.titulo.trim()) { notificar('Revisa el título y el rango salarial'); return; }
    const vacante: Vacante = {
      ...datos, identificador: `local-${Date.now()}`, empresa: empresaActual, idEmpresa: idEmpresaActual ?? undefined,
      verificada: true, inicialesEmpresa: empresaActual.slice(0, 2).toUpperCase(), indiceRespuesta: 0, activa: true,
      fechaCreacion: new Date().toISOString(), fechaVencimiento: dentroDeDias(30)
    };
    establecerVacantesLocales(anteriores => [vacante, ...anteriores]);
    notificar('Vacante guardada en este navegador · se publicará al conectar el servicio de Vacantes');
  }, [notificar, empresaActual, idEmpresaActual]);

  /** TEMPORAL: datos no sensibles del perfil en este navegador, hasta que exista la API de perfiles. */
  const actualizarPerfil = useCallback((siguiente: Partial<PerfilUsuario>) => {
    if (!idCandidatoActual) return;
    const { curp: _curp, calle: _calle, numeroExterior: _numero, colonia: _colonia, codigoPostal: _cp, datosFotografia: _foto, celular: _celular, ...seguro } = siguiente;
    establecerPerfilesLocales(anteriores => ({ ...anteriores, [idCandidatoActual]: { ...anteriores[idCandidatoActual], ...seguro } }));
  }, [idCandidatoActual]);
  const guardarPerfil = useCallback((siguiente: Partial<PerfilUsuario>) => { actualizarPerfil(siguiente); notificar('Cambios de perfil guardados en este navegador'); }, [actualizarPerfil, notificar]);

  const [preferenciasEditadas, establecerPreferenciasEditadas] = useState<Partial<PreferenciasReclutador>>({});
  const preferenciasReclutador: PreferenciasReclutador = useMemo(() => ({
    nombreContacto: sesion ? nombreDesdeCorreo(sesion.correo) : '', empresa: empresaActual,
    correo: sesion?.correo ?? '', celular: '', aceptaWhatsapp: false, sitioWeb: '', ...preferenciasEditadas
  }), [sesion, empresaActual, preferenciasEditadas]);
  const actualizarPreferenciasReclutador = useCallback((siguiente: Partial<PreferenciasReclutador>) => {
    establecerPreferenciasEditadas(anteriores => ({ ...anteriores, ...siguiente }));
    notificar('Preferencias guardadas en este navegador');
  }, [notificar]);

  /** Marca todos los avisos; el cambio se ve de inmediato y se confirma en el servidor. */
  const marcarNotificacionesLeidas = useCallback(() => {
    establecerNotificacionesReclutador(lista => lista.map(n => ({ ...n, leida: true })));
    apiNotificaciones.marcarTodasLeidas().catch(error => notificar(mensajeDeError(error)));
  }, [notificar]);
  /** Marca un solo aviso como leído (NT-041). */
  const marcarNotificacionLeida = useCallback((identificador: string) => {
    establecerNotificacionesReclutador(lista => lista.map(n => n.identificador === identificador ? { ...n, leida: true } : n));
    if (!identificador.startsWith('nuevo-')) apiNotificaciones.marcarLeida(identificador).catch(error => notificar(mensajeDeError(error)));
  }, [notificar]);

  const valor = useMemo<EstadoAplicacion>(() => ({
    sesion, entrar, salir, vacantes, postulaciones, cargandoPostulaciones, perfil, idCandidatoActual, idEmpresaActual,
    postular, publicar, moverPostulaciones, abrirPerfilCandidato, actualizarPerfil, guardarPerfil,
    preferenciasReclutador, actualizarPreferenciasReclutador, notificacionesReclutador, marcarNotificacionesLeidas, marcarNotificacionLeida, avisoTemporal, notificar
  }), [sesion, entrar, salir, vacantes, postulaciones, cargandoPostulaciones, perfil, idCandidatoActual, idEmpresaActual,
    postular, publicar, moverPostulaciones, abrirPerfilCandidato, actualizarPerfil, guardarPerfil,
    preferenciasReclutador, actualizarPreferenciasReclutador, notificacionesReclutador, marcarNotificacionesLeidas, marcarNotificacionLeida, avisoTemporal, notificar]);
  return <ContextoAplicacion.Provider value={valor}>{children}</ContextoAplicacion.Provider>;
}
/** Permite que un componente consulte los datos globales. */
export const usarAplicacion = () => { const ctx = useContext(ContextoAplicacion); if (!ctx) throw Error('Falta ProveedorAplicacion'); return ctx; };
