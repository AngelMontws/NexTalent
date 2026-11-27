/**
 * NexTalent — tipos
 * Declara los tipos de datos que comparten los componentes del frontend.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
/** Establece la estructura de datos de ModalidadVacante. */
export type ModalidadVacante = 'Remoto' | 'Híbrido' | 'Presencial';
/** Establece la estructura de datos de JornadaVacante. */
export type JornadaVacante = 'Tiempo completo' | 'Medio tiempo' | 'Por turnos';
/** Establece la estructura de datos de NivelHabilidad. */
export type NivelHabilidad = 'Básico' | 'Intermedio' | 'Avanzado';
/** Establece la estructura de datos de EstadoPostulacion. */
export type EstadoPostulacion = 'Enviada' | 'Vista' | 'En revisión' | 'Entrevista' | 'Oferta / Contratado' | 'No seleccionado';
/** Establece la estructura de datos de RolUsuario. */
export type RolUsuario = 'candidato' | 'reclutador';

/** Establece la estructura de datos de Habilidad. */
export interface Habilidad { nombre: string; nivel: NivelHabilidad; }
/** Establece la estructura de datos de Vacante. */
export interface Vacante {
  identificador: string;
  titulo: string;
  empresa: string;
  /** Identificador estable de la empresa; no cambia al renombrarla. */
  idEmpresa?: string;
  verificada: boolean;
  ubicacion: string;
  modalidad: ModalidadVacante;
  jornada: JornadaVacante;
  salarioMinimo: number;
  salarioMaximo: number;
  tipoSalario: 'Bruto' | 'Neto';
  habilidades: Habilidad[];
  descripcion: string;
  responsabilidades: string[];
  prestaciones: string[];
  indiceRespuesta: number;
  fechaCreacion: string;
  fechaVencimiento: string;
  activa: boolean;
  inicialesEmpresa: string;
  /** Sitio web público de la empresa, cuando el reclutador lo registra. */
  sitioWeb?: string;
}
/** Establece la estructura de datos de RegistroHistorial. */
export interface RegistroHistorial { estado: EstadoPostulacion; fecha: string; }
/** Establece la estructura de datos de InformacionDocumento. */
export interface InformacionDocumento { tipo: string; nombre: string; tamanio: number; }
/** Establece la estructura de datos de Postulacion. */
export interface Postulacion {
  documentos?: InformacionDocumento[];
  identificador: string;
  folio: string;
  idVacante: string;
  idCandidato: string;
  nombreCandidato: string;
  ubicacionCandidato: string;
  habilidadesCandidato: string[];
  estado: EstadoPostulacion;
  fechaCreacion: string;
  fechaActualizacion: string;
  historial: RegistroHistorial[];
  /** Datos que manda el servidor; las pantallas los prefieren a los calculados en el navegador. */
  tituloVacante?: string;
  nombreEmpresa?: string;
  /** Porcentaje de coincidencia guardado al postularse (RN-03). */
  coincidencia?: number;
  habilidadesFaltantes?: string[];
}
/** Establece la estructura de datos de PerfilUsuario. */
export interface PerfilUsuario {
  identificador: string;
  nombre: string;
  correo: string;
  celular: string;
  ubicacion: string;
  titulo: string;
  habilidades: Habilidad[];
  aceptaWhatsapp: boolean;
  nombrePila?: string;
  apellidoPaterno?: string;
  apellidoMaterno?: string;
  edad?: string;
  curp?: string;
  calle?: string;
  numeroExterior?: string;
  colonia?: string;
  municipio?: string;
  entidadFederativa?: string;
  codigoPostal?: string;
  datosFotografia?: string;
}
/** Establece la estructura de datos de NotificacionReclutador. */
export interface NotificacionReclutador {
  identificador: string;
  candidato: string;
  tituloVacante: string;
  empresa: string;
  folio: string;
  fechaCreacion: string;
  leida: boolean;
}
/** Establece la estructura de datos de PreferenciasReclutador. */
export interface PreferenciasReclutador {
  nombreContacto: string;
  empresa: string;
  correo: string;
  celular: string;
  aceptaWhatsapp: boolean;
  datosFotografia?: string;
  /** Dirección de la página principal de la empresa. */
  sitioWeb?: string;
}
/** Establece la estructura de datos de NuevaVacante. */
export type NuevaVacante = Omit<Vacante, 'identificador' | 'fechaCreacion' | 'fechaVencimiento' | 'indiceRespuesta' | 'activa' | 'verificada' | 'empresa' | 'idEmpresa' | 'inicialesEmpresa'>;
