/**
 * NexTalent — CargaDocumentos
 * Selecciona, comprueba y enumera los documentos de una postulación.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import { FileText, Paperclip, UploadCloud, X } from 'lucide-react';
import { useState } from 'react';
import type { InformacionDocumento } from '../tipos';

/** Documentos aceptados, formato y obligatoriedad de cada campo. */
const DOCUMENTOS_PERMITIDOS: { clave: string; titulo: string; obligatorios: boolean; ayuda: string; formatosAdmitidos: string ;}[] = [
  { clave: 'cv', titulo: 'Currículum vitae (CV)', obligatorios: true, ayuda: 'PDF, DOC o DOCX', formatosAdmitidos: '.pdf,.doc,.docx' },
  { clave: 'carta1', titulo: 'Carta de recomendación 1', obligatorios: false, ayuda: 'PDF o imagen', formatosAdmitidos: '.pdf,.png,.jpg,.jpeg' },
  { clave: 'carta2', titulo: 'Carta de recomendación 2', obligatorios: false, ayuda: 'PDF o imagen', formatosAdmitidos: '.pdf,.png,.jpg,.jpeg' },
  { clave: 'carta3', titulo: 'Carta de recomendación 3', obligatorios: false, ayuda: 'PDF o imagen', formatosAdmitidos: '.pdf,.png,.jpg,.jpeg' },
  { clave: 'titulo', titulo: 'Título universitario', obligatorios: false, ayuda: 'Copia en PDF o imagen', formatosAdmitidos: '.pdf,.png,.jpg,.jpeg' },
  { clave: 'cedula', titulo: 'Cédula profesional', obligatorios: false, ayuda: 'Copia en PDF o imagen', formatosAdmitidos: '.pdf,.png,.jpg,.jpeg' },
];
/** Renderiza los campos para adjuntar CV, cartas, título y cédula. */
export default function CargaDocumentos({ alCambiar: alCambiar }: { alCambiar: (archivos: InformacionDocumento[]) => void ;}) {
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [archivos, establecerArchivos] = useState<Record<string, File>>({});
  // Estado local: conserva este dato y actualiza la pantalla cuando cambia.
  const [error, establecerError] = useState('');
  /** Actualiza el dato elegido del formulario en memoria. */
  const actualizar = (clave: string, archivo?: File) => {
    if (archivo && archivo.size > 5 * 1024 * 1024) { establecerError('Cada documento debe pesar máximo 5 MB.'); return ;}
    const definicion = DOCUMENTOS_PERMITIDOS.find(elemento => elemento.clave === clave);
    if (archivo && (!definicion || !definicion.formatosAdmitidos.split(',').some(extension => archivo.name.toLowerCase().endsWith(extension)))) { establecerError('Tipo de archivo no permitido para este campo.'); return ;}
    const siguiente = { ...archivos }; if (archivo) siguiente[clave] = archivo; else delete siguiente[clave]; establecerError(''); establecerArchivos(siguiente); alCambiar(Object.entries(siguiente).map(([tipo, elemento]) => ({ tipo, nombre: elemento.name, tamanio: elemento.size })));
  };
  // Vista: organiza los elementos que la persona puede consultar o utilizar.
  return <div className="space-y-3"><div className="flex items-center gap-2"><UploadCloud size={18} className="text-[#A17900]" /><h3 className="font-bold">Adjunta tus documentos</h3></div>
    <p className="text-xs leading-5 text-slate-500 dark:text-slate-400">CV obligatorio. Puedes añadir hasta tres cartas, título y cédula cuando correspondan a tu perfil. Máximo 5 MB por archivo. Los archivos solo se seleccionan en este navegador; no se envían ni guardan.</p>
    <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">{DOCUMENTOS_PERMITIDOS.map(doc => <div key={doc.clave} className="rounded-xl border border-dashed border-[#D4AF36]/60 p-3 dark:border-[#66522B]"><label className="block cursor-pointer"><span className="flex items-center gap-1 text-xs font-bold"><Paperclip size={13} />{doc.titulo} {doc.obligatorios ? '*' : ''}</span><span className="my-1 block text-[11px] text-slate-500">{doc.ayuda}</span><span className="inline-flex rounded-md bg-[#F6F0B3] px-2 py-1 text-[11px] font-semibold text-[#453513]">{archivos[doc.clave] ? 'Cambiar archivo' : 'Elegir archivo'}</span><input className="sr-only" aria-label={doc.titulo} type="file" accept={doc.formatosAdmitidos} onChange={evento => actualizar(doc.clave, evento.target.files?.[0])} /></label>{archivos[doc.clave] && <div className="mt-2 flex items-center gap-1 rounded-md bg-slate-50 p-2 text-xs dark:bg-[#30291F]"><FileText size={13} /><span className="min-w-0 flex-1 truncate">{archivos[doc.clave].name}</span><button className="shrink-0" aria-label={`Quitar ${doc.titulo}`} type="button" onClick={() => actualizar(doc.clave)}><X size={14} /></button></div>}</div>)}</div>
    {error && <p role="alert" className="text-sm font-semibold text-red-600">{error}</p>}
  </div>;
}
