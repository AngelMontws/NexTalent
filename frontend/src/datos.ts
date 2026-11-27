/**
 * NexTalent — datos
 * Define datos de demostración y herramientas para fechas, salarios y coincidencias.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import type { Postulacion, Vacante, PerfilUsuario, Habilidad } from './tipos';

/** Convierte un número en importe mexicano con separadores. */
export const formatearSalario = (valor: number) => new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 0 }).format(valor);
/** Obtiene una fecha anterior a hoy. */
export const haceDias = (dias: number) => new Date(Date.now() - dias * 86_400_000).toISOString();
/** Obtiene una fecha futura. */
export const dentroDeDias = (dias: number) => new Date(Date.now() + dias * 86_400_000).toISOString();
/** Muestra una fecha en formato breve español. */
export const formatearFecha = (fecha: string) => new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(fecha));
/** Devuelve únicamente direcciones HTTP o HTTPS seguras para enlaces externos. */
export const validarEnlaceEmpresa = (texto?: string): string | null => {
  if (!texto?.trim()) return null;
  try {
    const url = new URL(texto.trim());
    return (url.protocol === 'https:' || url.protocol === 'http:') && url.hostname.includes('.') ? url.href : null;
  } catch { return null; }
};
/** Calcula la antigüedad del estado actual. */
export const diasEnEtapa = (fecha: string) => Math.max(0, Math.floor((Date.now() - new Date(fecha).getTime()) / 86_400_000));
/** Verifica que la vacante siga activa y no haya caducado. */
export const estaDisponible = (vacante: Vacante) => vacante.activa && new Date(vacante.fechaVencimiento).getTime() >= Date.now();
/** Habilidades permitidas en búsquedas y publicaciones. */
export const CATALOGO_HABILIDADES = ['React', 'TypeScript', 'JavaScript', 'CSS', 'HTML', 'Git', 'Figma', 'SQL', 'Excel', 'Atención al cliente', 'Logística', 'Inventarios', 'Python', 'C#', 'Comunicación', 'Organización', 'Ventas', 'Power BI', 'Inglés'];
/** Secuencia principal del seguimiento de una postulación. */
export const ETAPAS_POSTULACION = ['Enviada', 'Vista', 'En revisión', 'Entrevista', 'Oferta / Contratado'] as const;
/** Incluye también resultados finales de contratación o descarte. */
export const TODOS_LOS_ESTADOS = [...ETAPAS_POSTULACION, 'No seleccionado'] as const;

/** Perfil ficticio usado en el modo demostración. */
export const perfilInicial: PerfilUsuario = {
  identificador: 'me', nombre: 'Alex Rivera', correo: 'alex.rivera@ejemplo.com', celular: '55 0000 0000',
  ubicacion: 'Ciudad de México', titulo: 'Desarrollador Frontend Jr.', aceptaWhatsapp: false,
  habilidades: [
    { nombre: 'React', nivel: 'Intermedio' }, { nombre: 'TypeScript', nivel: 'Intermedio' },
    { nombre: 'JavaScript', nivel: 'Avanzado' }, { nombre: 'HTML', nivel: 'Avanzado' },
    { nombre: 'CSS', nivel: 'Intermedio' }, { nombre: 'Git', nivel: 'Intermedio' },
    { nombre: 'Figma', nivel: 'Básico' }, { nombre: 'SQL', nivel: 'Básico' }
  ]
};

/** Convierte nombres de habilidades en objetos con nivel asignado. */
const convertirHabilidades = (elementos: string[]): Habilidad[] => elementos.map((nombre, indice) => ({ nombre, nivel: indice < 2 ? 'Intermedio' : 'Básico' }));
/** Crea una vacante ficticia con salario, ubicación y fecha de vigencia. */
const vacante = (identificador: string, titulo: string, empresa: string, ubicacion: string, modalidad: Vacante['modalidad'], jornada: Vacante['jornada'], minimo: number, maximo: number, habilidades: Habilidad[], edad: number, inicialesEmpresa: string, indice: number, descripcion: string): Vacante => ({
  identificador: identificador, titulo: titulo, empresa: empresa, ubicacion: ubicacion, modalidad: modalidad, jornada: jornada, salarioMinimo: minimo, salarioMaximo: maximo, tipoSalario: 'Bruto', habilidades: habilidades, verificada: true,
  descripcion: descripcion, responsabilidades: ['Colaborar con el equipo en actividades del puesto.', 'Cumplir objetivos con buenas prácticas y comunicación clara.', 'Documentar avances y participar en mejoras continuas.'],
  prestaciones: ['Prestaciones de ley', 'Capacitación continua', 'Ambiente de trabajo colaborativo'], indiceRespuesta: indice,
  activa: true, fechaCreacion: haceDias(edad), fechaVencimiento: dentroDeDias(30 - edad), inicialesEmpresa: inicialesEmpresa
});

/** Vacantes ficticias que se muestran al abrir el sistema. */
export const vacantesIniciales: Vacante[] = [
  vacante('job-1', 'Desarrollador Frontend Jr.', 'Impulso Digital', 'Ciudad de México', 'Híbrido', 'Tiempo completo', 18000, 24000, convertirHabilidades(['React', 'TypeScript', 'JavaScript', 'CSS', 'Git', 'Figma']), 2, 'ID', 94, 'Construye experiencias web accesibles y rápidas en un equipo cercano. Participarás en el desarrollo de componentes reutilizables y la mejora de interfaces.'),
  vacante('job-2', 'Diseñador UX/UI Jr.', 'Nube Creativa', 'Ciudad de México', 'Remoto', 'Tiempo completo', 16000, 22000, convertirHabilidades(['Figma', 'HTML', 'CSS', 'Comunicación']), 1, 'NC', 88, 'Diseña flujos digitales claros, prototipos y sistemas de diseño, en colaboración con producto y desarrollo.'),
  vacante('job-3', 'Analista de Datos Jr.', 'Datos Norte', 'Monterrey', 'Híbrido', 'Tiempo completo', 20000, 28000, convertirHabilidades(['SQL', 'Excel', 'Power BI', 'Python']), 4, 'DN', 96, 'Transforma datos en indicadores y reportes para apoyar la toma de decisiones.'),
  vacante('job-4', 'Desarrollador Web', 'Impulso Digital', 'Ciudad de México', 'Remoto', 'Tiempo completo', 25000, 35000, convertirHabilidades(['React', 'TypeScript', 'C#', 'SQL', 'Git']), 3, 'ID', 94, 'Desarrolla interfaces e integra servicios API en proyectos tecnológicos orientados a personas.'),
  vacante('job-5', 'Auxiliar de Logística', 'Ruta Segura', 'Estado de México', 'Presencial', 'Por turnos', 10500, 14000, convertirHabilidades(['Logística', 'Inventarios', 'Excel', 'Organización']), 6, 'RS', 82, 'Apoya la recepción, el registro y el movimiento de materiales en almacén.'),
  vacante('job-6', 'Asesor de Atención al Cliente', 'Conecta MX', 'Guadalajara', 'Híbrido', 'Medio tiempo', 9000, 13500, convertirHabilidades(['Atención al cliente', 'Comunicación', 'Excel']), 1, 'CM', 91, 'Brinda orientación y seguimiento a clientes mediante canales digitales.'),
  vacante('job-7', 'Practicante de Desarrollo', 'Impulso Digital', 'Ciudad de México', 'Híbrido', 'Medio tiempo', 8000, 12000, convertirHabilidades(['HTML', 'CSS', 'JavaScript', 'Git']), 9, 'ID', 94, 'Inicia tu experiencia profesional apoyando el desarrollo de interfaces web.'),
  vacante('job-8', 'Coordinador de Ventas', 'Avanza Retail', 'Puebla', 'Presencial', 'Tiempo completo', 16000, 22000, convertirHabilidades(['Ventas', 'Excel', 'Comunicación', 'Organización']), 5, 'AR', 87, 'Coordina indicadores comerciales y equipos de atención para mejorar resultados.')
];

/** Compara las habilidades y calcula el porcentaje de coincidencia. */
export const calcularCoincidencia = (vacante: Vacante, habilidades: Habilidad[]) => {
  const niveles = { 'Básico': 1, 'Intermedio': 2, 'Avanzado': 3 };
  const presentes = vacante.habilidades.filter(requisito =>
    habilidades.some(habilidad => habilidad.nombre.toLowerCase() === requisito.nombre.toLowerCase()
      && niveles[habilidad.nivel] >= niveles[requisito.nivel]));
  return { porcentaje: Math.round((presentes.length / Math.max(1, vacante.habilidades.length)) * 100),
    coincidentes: presentes.length, total: vacante.habilidades.length };
};

/** Crea una postulación ficticia con historial inicial. */
function postulacionEjemplo(identificador: string, folio: string, idVacante: string, idCandidato: string, nombreCandidato: string, estado: Postulacion['estado'], dias: number, habilidades: string[]): Postulacion {
  return { identificador: identificador, folio, idVacante: idVacante, idCandidato: idCandidato, nombreCandidato: nombreCandidato, ubicacionCandidato: 'Ciudad de México', habilidadesCandidato: habilidades, estado: estado, fechaCreacion: haceDias(dias + 2), fechaActualizacion: haceDias(dias), historial: [{ estado: 'Enviada', fecha: haceDias(dias + 2) }, ...(estado !== 'Enviada' ? [{ estado: estado, fecha: haceDias(dias) }] : [])] };
}
/** Postulaciones de ejemplo para el historial y el Kanban. */
export const postulacionesIniciales: Postulacion[] = [
  postulacionEjemplo('a1', 'NT-000241', 'job-1', 'candidato-ejemplo', 'Candidato Ejemplo', 'En revisión', 2, ['React', 'TypeScript', 'JavaScript', 'HTML', 'CSS', 'Git', 'Figma']),
  postulacionEjemplo('a2', 'NT-000242', 'job-3', 'candidato-ejemplo', 'Candidato Ejemplo', 'Vista', 1, ['SQL', 'Excel']),
  postulacionEjemplo('a3', 'NT-000219', 'job-2', 'candidato-ejemplo', 'Candidato Ejemplo', 'No seleccionado', 7, ['Figma', 'HTML', 'CSS']),
  postulacionEjemplo('a4', 'NT-000253', 'job-1', 'c2', 'María García', 'Entrevista', 1, ['React', 'TypeScript', 'JavaScript', 'CSS', 'Git', 'Figma']),
  postulacionEjemplo('a5', 'NT-000254', 'job-1', 'c3', 'Carlos López', 'Enviada', 1, ['React', 'JavaScript', 'Git']),
  postulacionEjemplo('a6', 'NT-000255', 'job-1', 'c4', 'Sofía Torres', 'Vista', 3, ['React', 'TypeScript', 'Git', 'HTML']),
  postulacionEjemplo('a7', 'NT-000256', 'job-1', 'c5', 'Javier Morales', 'Oferta / Contratado', 0, ['React', 'TypeScript', 'JavaScript', 'CSS', 'Git']),
  postulacionEjemplo('a8', 'NT-000257', 'job-4', 'c6', 'Lucía Hernández', 'En revisión', 11, ['C#', 'SQL', 'React', 'Git']),
  postulacionEjemplo('a9', 'NT-000258', 'job-7', 'c7', 'Diego Castillo', 'Enviada', 0, ['HTML', 'CSS', 'JavaScript'])
];
