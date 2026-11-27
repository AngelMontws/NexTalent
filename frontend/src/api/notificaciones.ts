/**
 * NexTalent — notificaciones
 * Bandeja web del reclutador (NT-041) en Notificaciones (Backend 2), por el gateway en /api/notificaciones.
 * Los avisos se guardan en el servidor: no se pierden al recargar ni si el reclutador no estaba conectado.
 */
import { pedir } from './cliente';
import type { NotificacionReclutador } from '../tipos';

interface AvisoWeb {
  id: string; tipo: string; postulacionId: string | null; vacanteId: string | null; folio: string | null;
  tituloVacante: string | null; nombreEmpresa: string | null; creadaEn: string; leidaEn: string | null;
}

const aPantalla = (a: AvisoWeb): NotificacionReclutador => ({
  identificador: a.id, idPostulacion: a.postulacionId ?? undefined,
  // El Manual de Identidad pide no mostrar datos personales en el aviso: "Una persona se postuló".
  candidato: 'Nueva postulación', tituloVacante: a.tituloVacante ?? 'Vacante', empresa: a.nombreEmpresa ?? '',
  folio: a.folio ?? '', fechaCreacion: a.creadaEn, leida: a.leidaEn !== null
});

export async function listarAvisos(): Promise<NotificacionReclutador[]> {
  return (await pedir<AvisoWeb[]>('/api/notificaciones')).map(aPantalla);
}

export const marcarLeida = (id: string) => pedir<void>(`/api/notificaciones/${id}/leida`, { metodo: 'PATCH' });

export const marcarTodasLeidas = () => pedir<void>('/api/notificaciones/leidas', { metodo: 'POST' });
