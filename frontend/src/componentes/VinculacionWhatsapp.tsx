/**
 * NexTalent — VinculacionWhatsapp
 * HU-07.01: vincula el celular con un código de 6 dígitos que llega por WhatsApp.
 * Sin el consentimiento marcado no se envía nada. Para darse de baja se responde BAJA en WhatsApp.
 */
import { useState } from 'react';
import { CheckCircle2, MessageCircle, Send, ShieldCheck } from 'lucide-react';
import { mensajeDeError } from '../api/cliente';
import { mensajesSimulador, solicitarCodigo, verificarCodigo } from '../api/whatsapp';
import type { RolUsuario } from '../tipos';

type Paso = 'numero' | 'codigo' | 'vinculado';

export default function VinculacionWhatsapp({ rol, celularInicial, alVincular }: {
  rol: RolUsuario; celularInicial: string; alVincular: (celular: string) => void;
}) {
  const [paso, establecerPaso] = useState<Paso>('numero');
  const [celular, establecerCelular] = useState(celularInicial);
  const [acepta, establecerAcepta] = useState(false);
  const [codigo, establecerCodigo] = useState('');
  const [mensaje, establecerMensaje] = useState('');
  const [error, establecerError] = useState('');
  const [enviando, establecerEnviando] = useState(false);
  const [codigoSimulado, establecerCodigoSimulado] = useState('');

  const limpio = celular.replace(/\D/g, '');

  /** Pide el código. El servidor valida el número y el límite de reenvíos. */
  const pedirCodigo = async () => {
    establecerError(''); establecerMensaje(''); establecerCodigoSimulado('');
    if (!/^\d{10}$/.test(limpio)) { establecerError('Escribe un celular de 10 dígitos.'); return; }
    if (!acepta) { establecerError('Marca la autorización para recibir avisos por WhatsApp.'); return; }
    establecerEnviando(true);
    try {
      establecerMensaje((await solicitarCodigo(limpio, acepta)).mensaje);
      establecerPaso('codigo');
    } catch (falla) { establecerError(mensajeDeError(falla)); }
    finally { establecerEnviando(false); }
  };

  /** Comprueba el código; si es correcto, el número queda vinculado y con consentimiento. */
  const comprobar = async () => {
    establecerError('');
    if (!/^\d{6}$/.test(codigo)) { establecerError('El código tiene 6 dígitos.'); return; }
    establecerEnviando(true);
    try {
      establecerMensaje((await verificarCodigo(codigo)).mensaje);
      establecerPaso('vinculado');
      alVincular(limpio);
    } catch (falla) { establecerError(mensajeDeError(falla)); }
    finally { establecerEnviando(false); }
  };

  /** SOLO DESARROLLO: el WhatsApp está simulado, así que el código se lee del simulador. */
  const verCodigoSimulado = async () => {
    try {
      const ultimo = (await mensajesSimulador()).find(m => m.plantilla === 'codigo');
      establecerCodigoSimulado(ultimo ? ultimo.texto : 'Todavía no hay código para tu número.');
    } catch (falla) { establecerCodigoSimulado(mensajeDeError(falla)); }
  };

  return <section className="panel min-w-0 p-5 sm:p-7">
    <div className="flex items-start gap-3"><MessageCircle className="mt-1 shrink-0 text-[#9A7100] dark:text-[#D4AF36]" size={22} /><div><h2 className="text-lg font-bold">Avisos de WhatsApp</h2><p className="mt-1 text-xs leading-5 text-slate-500 dark:text-slate-300">{rol === 'candidato' ? 'Recibe los cambios de tus postulaciones en tu celular.' : 'Recibe un aviso cuando una persona se postule a tus vacantes.'}</p></div></div>

    {paso === 'vinculado' ? <div role="status" className="mt-5 flex items-start gap-3 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-200"><CheckCircle2 size={20} className="shrink-0" /><div><p className="font-semibold">{mensaje}</p><p className="mt-1 text-xs">Para dejar de recibir avisos, responde <strong>BAJA</strong> en el chat de WhatsApp de NexTalent.</p></div></div> : <div className="mt-5 space-y-4">
      <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-end">
        <label className="block"><span className="etiqueta-campo">Celular (10 dígitos)</span><input className="campo" type="tel" inputMode="numeric" value={celular} onChange={evento => establecerCelular(evento.target.value)} placeholder="55 1234 5678" disabled={paso === 'codigo'} /></label>
        {paso === 'codigo' && <button type="button" className="boton-discreto !text-xs" onClick={() => { establecerPaso('numero'); establecerCodigo(''); establecerMensaje(''); }}>Cambiar número</button>}
      </div>
      {paso === 'numero' && <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-[#E6D9AE] bg-[#F6F0B3]/40 p-4 dark:border-[#594623] dark:bg-[#514324]/30"><input className="mt-1 h-4 w-4 shrink-0 accent-[#CD9901]" type="checkbox" checked={acepta} onChange={evento => establecerAcepta(evento.target.checked)} /><span className="text-sm leading-6">Acepto recibir notificaciones de NexTalent por WhatsApp.<span className="block text-xs text-slate-500 dark:text-slate-300">Guardamos la fecha de tu autorización. Puedes darte de baja cuando quieras respondiendo BAJA.</span></span></label>}
      {paso === 'codigo' && <label className="block"><span className="etiqueta-campo">Código de 6 dígitos</span><input className="campo tracking-[.4em]" inputMode="numeric" autoComplete="one-time-code" maxLength={6} value={codigo} onChange={evento => establecerCodigo(evento.target.value.replace(/\D/g, ''))} placeholder="000000" /></label>}
      {mensaje && <p role="status" className="rounded-lg bg-[#F6F0B3]/60 p-3 text-xs text-[#4B380D] dark:bg-[#514324]/40 dark:text-[#F6F0B3]">{mensaje}</p>}
      {error && <p role="alert" className="rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700">{error}</p>}
      <div className="flex flex-wrap gap-2">
        {paso === 'numero'
          ? <button type="button" className="boton-principal" disabled={enviando} onClick={pedirCodigo}><Send size={16} /> {enviando ? 'Enviando…' : 'Enviar código'}</button>
          : <><button type="button" className="boton-principal" disabled={enviando} onClick={comprobar}><ShieldCheck size={16} /> {enviando ? 'Verificando…' : 'Verificar'}</button><button type="button" className="boton-secundario" disabled={enviando} onClick={pedirCodigo}>Reenviar código</button></>}
      </div>
      {import.meta.env.DEV && paso === 'codigo' && <div className="rounded-lg border border-dashed border-slate-300 p-3 text-xs text-slate-500 dark:border-slate-700"><button type="button" className="font-semibold text-[#956B00] underline dark:text-[#F6F0B3]" onClick={verCodigoSimulado}>Ver código en el simulador (solo desarrollo)</button>{codigoSimulado && <p className="mt-2 font-mono text-base tracking-widest text-slate-700 dark:text-slate-200">{codigoSimulado}</p>}</div>}
    </div>}
  </section>;
}
