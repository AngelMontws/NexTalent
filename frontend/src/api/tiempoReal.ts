/**
 * NexTalent — tiempoReal
 * Conexión al hub /hubs/estatus de Postulaciones (SignalR). El candidato recibe sus cambios de etapa y
 * el reclutador las nuevas postulaciones de su empresa, sin recargar (HU-05.02, RNF-02).
 */
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export interface AvisoEstatus {
  postulacionId: string; folio: string; vacanteId: string; tituloVacante: string; nombreEmpresa: string;
  estadoAnterior: string; estadoNuevo: string; causa: string; cambiadoEn: string; secuencia: number;
}
export interface AvisoNuevaPostulacion {
  postulacionId: string; folio: string; vacanteId: string; tituloVacante: string; creadaEn: string;
}

/** Abre la conexión y devuelve una función para cerrarla. */
export function conectarTiempoReal(token: string, manejadores: {
  alCambiarEstatus: (aviso: AvisoEstatus) => void;
  alCrearPostulacion: (aviso: AvisoNuevaPostulacion) => void;
  alReconectar: () => void;
}): () => void {
  const conexion = new HubConnectionBuilder()
    .withUrl('/hubs/estatus', { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();

  conexion.on('estatusCambiado', manejadores.alCambiarEstatus);
  conexion.on('postulacionCreada', manejadores.alCrearPostulacion);
  // Al volver la conexión se recarga todo, por si se perdió un aviso mientras estaba caída.
  conexion.onreconnected(() => manejadores.alReconectar());

  let cerrada = false;
  let arranque: Promise<void> = Promise.resolve();
  const iniciar = (intento = 0) => {
    arranque = conexion.start().catch(() => {
      if (!cerrada && intento < 5) window.setTimeout(() => { if (!cerrada) iniciar(intento + 1); }, 2000 * (intento + 1));
    });
  };
  iniciar();
  // No se detiene a media negociación ("The connection was stopped during negotiation"): primero se espera
  // a que termine de arrancar. Pasa en desarrollo porque React.StrictMode monta y desmonta dos veces.
  return () => { cerrada = true; void arranque.finally(() => conexion.stop()); };
}
