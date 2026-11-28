/**
 * NexTalent — whatsapp
 * Vinculación del número con código de 6 dígitos (HU-07.01) en Notificaciones, por el gateway en /whatsapp.
 */
import { pedir } from './cliente';

interface Respuesta { exito: boolean; mensaje: string; }
export interface MensajeSimulado { id: string; telefono: string; plantilla: string; texto: string; }

/** Envía el código al número. Sin consentimiento, el servidor no manda nada. */
export const solicitarCodigo = (numero: string, aceptaAvisos: boolean) =>
  pedir<Respuesta>('/whatsapp/vincular', { metodo: 'POST', cuerpo: { numero, aceptaAvisos } });

export const verificarCodigo = (codigo: string) =>
  pedir<Respuesta>('/whatsapp/verificar', { metodo: 'POST', cuerpo: { codigo } });

/** SOLO DESARROLLO: lo que el simulador "envió" a tus números (para leer el código en la demo). */
export const mensajesSimulador = () => pedir<MensajeSimulado[]>('/whatsapp/simulador');
