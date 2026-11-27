/**
 * NexTalent — VentanaModal
 * Muestra ventanas emergentes accesibles y permite cerrarlas.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { useEffect, type ReactNode } from 'react';
import { X } from 'lucide-react';
/** Abre una ventana emergente y la cierra con Escape o al pulsar fuera. */
export default function VentanaModal({ titulo: titulo, onClose, children, narrow = false }: { titulo: string; onClose: () => void; children: ReactNode; narrow?: boolean; }) {
  // Efecto: sincroniza un cambio de datos con la interfaz o el navegador.
  useEffect(() => { const esc = (evento: KeyboardEvent) => { if (evento.key === 'Escape') onClose(); }; window.addEventListener('keydown', esc); const anterior = document.body.style.overflow; document.body.style.overflow = 'hidden'; return () => { window.removeEventListener('keydown', esc); document.body.style.overflow = anterior; }; }, [onClose]);
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/45 backdrop-blur-[2px] sm:items-center sm:p-4" role="presentation" onMouseDown={(evento) => { if (evento.target === evento.currentTarget) onClose(); }}>
    <section role="dialog" aria-modal="true" aria-label={titulo} className={`max-h-[93dvh] w-full overflow-y-auto rounded-t-3xl border border-slate-200 bg-white p-5 shadow-2xl dark:border-slate-700 dark:bg-slate-900 sm:rounded-2xl sm:p-6 ${narrow ? 'sm:max-w-lg' : 'sm:max-w-2xl'}`}>
      <div className="mb-5 flex items-center justify-between gap-3"><h2 className="text-lg font-bold">{titulo}</h2><button type="button" onClick={onClose} className="boton-discreto !min-h-10 !w-10 !p-2" aria-label="Cerrar"><X size={19} /></button></div>{children}
    </section>
  </div>;
}
