/**
 * NexTalent — postulaciones
 * Endpoints de Postulaciones (Backend 2) por el gateway en /api/postulaciones.
 * Convierte las respuestas del servidor al tipo Postulacion que usan las pantallas.
 */
import { pedir } from './cliente';
import { HABILIDADES_POR_ID } from './catalogoTemporal';
import type { EstadoPostulacion, Postulacion } from '../tipos';

/** Estados tal como los manda el servidor. */
export type EstadoServidor = 'Enviada' | 'Vista' | 'EnRevision' | 'Entrevista' | 'OfertaContratado' | 'NoSeleccionado';

const A_PANTALLA: Record<EstadoServidor, EstadoPostulacion> = {
  Enviada: 'Enviada', Vista: 'Vista', EnRevision: 'En revisión', Entrevista: 'Entrevista',
  OfertaContratado: 'Oferta / Contratado', NoSeleccionado: 'No seleccionado'
};
const A_SERVIDOR = Object.fromEntries(
  Object.entries(A_PANTALLA).map(([servidor, pantalla]) => [pantalla, servidor])
) as Record<EstadoPostulacion, EstadoServidor>;

/** Convierte el estado de un aviso de SignalR al texto de pantalla. */
export const estadoEnPantalla = (estado: string): EstadoPostulacion => A_PANTALLA[estado as EstadoServidor] ?? 'Enviada';

interface Resumen {
  id: string; folio: string; vacanteId: string; tituloVacante: string; nombreEmpresa: string;
  estado: EstadoServidor; coincidenciaPct: number; diasEnEtapa: number; activa: boolean; creadaEn: string;
}
interface Detalle { resumen: Resumen; historial: { anterior: EstadoServidor | null; nuevo: EstadoServidor; fecha: string }[]; }
interface CandidatoDeVacante {
  postulacionId: string; folio: string; candidatoId: string; estado: EstadoServidor; coincidenciaPct: number;
  habilidadesCumplidas: string[]; habilidadesFaltantes: string[]; creadaEn: string; etapaActualizadaEn: string;
}

const nombreHabilidad = (id: string) => HABILIDADES_POR_ID[id] ?? 'Habilidad';

/** Postulación propia con su historial. */
function desdeDetalle({ resumen, historial }: Detalle, idCandidato: string, nombre: string): Postulacion {
  const pasos = historial.map(paso => ({ estado: A_PANTALLA[paso.nuevo], fecha: paso.fecha }));
  return {
    identificador: resumen.id, folio: resumen.folio, idVacante: resumen.vacanteId, idCandidato,
    nombreCandidato: nombre, ubicacionCandidato: '', habilidadesCandidato: [],
    estado: A_PANTALLA[resumen.estado], fechaCreacion: resumen.creadaEn,
    fechaActualizacion: pasos.length ? pasos[pasos.length - 1].fecha : resumen.creadaEn,
    historial: pasos.length ? pasos : [{ estado: 'Enviada', fecha: resumen.creadaEn }],
    tituloVacante: resumen.tituloVacante, nombreEmpresa: resumen.nombreEmpresa,
    coincidencia: Math.round(resumen.coincidenciaPct)
  };
}

/** Candidato de una vacante, visto por el reclutador. */
function desdeCandidato(c: CandidatoDeVacante, idVacante: string): Postulacion {
  return {
    identificador: c.postulacionId, folio: c.folio, idVacante, idCandidato: c.candidatoId,
    // TEMPORAL: el nombre y la ubicación vendrán del perfil público de Identidad.
    nombreCandidato: `Candidato ${c.folio}`, ubicacionCandidato: '',
    habilidadesCandidato: c.habilidadesCumplidas.map(nombreHabilidad),
    habilidadesFaltantes: c.habilidadesFaltantes.map(nombreHabilidad),
    estado: A_PANTALLA[c.estado], fechaCreacion: c.creadaEn, fechaActualizacion: c.etapaActualizadaEn,
    historial: [{ estado: A_PANTALLA[c.estado], fecha: c.etapaActualizadaEn }],
    coincidencia: Math.round(c.coincidenciaPct)
  };
}

/** HU-05.01: devuelve el folio. */
export async function postular(idVacante: string): Promise<string> {
  const respuesta = await pedir<Resumen>('/api/postulaciones', { metodo: 'POST', cuerpo: { vacanteId: idVacante } });
  return respuesta.folio;
}

/** HU-05.02 y HU-05.04: todas las postulaciones de la persona candidata, con historial. */
export async function listarMias(idCandidato: string, nombre: string): Promise<Postulacion[]> {
  const resumenes = await pedir<Resumen[]>('/api/postulaciones/mias');
  const detalles = await Promise.all(resumenes.map(r => pedir<Detalle>(`/api/postulaciones/${r.id}`)));
  return detalles.map(d => desdeDetalle(d, idCandidato, nombre));
}

/** HU-06.01: candidatos de una vacante, ya ordenados por coincidencia. */
export async function listarCandidatos(idVacante: string): Promise<Postulacion[]> {
  const lista = await pedir<CandidatoDeVacante[]>(`/api/postulaciones/vacantes/${idVacante}/candidatos`);
  return lista.map(c => desdeCandidato(c, idVacante));
}

/** HU-06.01: abrir el perfil pasa Enviada a Vista. */
export const abrirPerfil = (idPostulacion: string) =>
  pedir<unknown>(`/api/postulaciones/${idPostulacion}/abrir-perfil`, { metodo: 'POST' });

/** HU-06.02: mover de etapa. Reactivar desde "No seleccionado" exige motivo. */
export const moverEtapa = (idPostulacion: string, estado: EstadoPostulacion, motivo?: string) =>
  pedir<unknown>(`/api/postulaciones/${idPostulacion}/estado`, {
    metodo: 'PATCH', cuerpo: { estado: A_SERVIDOR[estado], motivo: motivo || null }
  });
