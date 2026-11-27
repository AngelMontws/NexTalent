/**
 * NexTalent — catalogoTemporal
 * TEMPORAL: copia de los datos de prueba que siembran Vacantes y Postulaciones (mismos GUID).
 * Se usa mientras Vacantes no exponga GET /api/vacantes y GET /api/habilidades.
 * Cuando existan, este archivo se borra y estas listas se piden al servidor.
 */
import { dentroDeDias, haceDias } from '../datos';
import type { Vacante } from '../tipos';

/** Nombres de las habilidades sembradas (44444444-…-01 a 08). */
export const HABILIDADES_POR_ID: Record<string, string> = {
  '44444444-4444-4444-4444-444444444401': 'C# / .NET',
  '44444444-4444-4444-4444-444444444402': 'SQL Server',
  '44444444-4444-4444-4444-444444444403': 'React / TypeScript',
  '44444444-4444-4444-4444-444444444404': 'Docker',
  '44444444-4444-4444-4444-444444444405': 'Git',
  '44444444-4444-4444-4444-444444444406': 'REST API',
  '44444444-4444-4444-4444-444444444407': 'Excel Intermedio',
  '44444444-4444-4444-4444-444444444408': 'Atención a Clientes'
};

const base = {
  verificada: true, tipoSalario: 'Bruto' as const, jornada: 'Tiempo completo' as const, indiceRespuesta: 90,
  fechaCreacion: haceDias(1), fechaVencimiento: dentroDeDias(29)
};

/** Las tres vacantes sembradas. La 3 está cerrada. */
export const VACANTES_TEMPORALES: Vacante[] = [
  {
    ...base, identificador: '55555555-5555-5555-5555-555555555501', titulo: 'Desarrollador Backend .NET Jr.',
    empresa: 'Empresa de prueba 1', idEmpresa: '11111111-1111-1111-1111-111111111101', inicialesEmpresa: 'E1',
    ubicacion: 'Ciudad de México', modalidad: 'Híbrido', salarioMinimo: 18000, salarioMaximo: 24000, activa: true,
    habilidades: [{ nombre: 'C# / .NET', nivel: 'Intermedio' }, { nombre: 'SQL Server', nivel: 'Básico' }, { nombre: 'Docker', nivel: 'Básico' }],
    descripcion: 'Desarrollo y mantenimiento de microservicios en ASP.NET Core y SQL Server.',
    responsabilidades: ['Implementar endpoints REST, consultas optimizadas y pruebas unitarias.'],
    prestaciones: ['Prestaciones superiores a la ley', 'Trabajo híbrido', 'Capacitación continua']
  },
  {
    ...base, identificador: '55555555-5555-5555-5555-555555555502', titulo: 'Desarrollador Frontend Jr.',
    empresa: 'Empresa de prueba 2', idEmpresa: '11111111-1111-1111-1111-111111111102', inicialesEmpresa: 'E2',
    ubicacion: 'Guadalajara, Jalisco', modalidad: 'Remoto', salarioMinimo: 17000, salarioMaximo: 23000, activa: true,
    habilidades: [{ nombre: 'React / TypeScript', nivel: 'Intermedio' }, { nombre: 'Docker', nivel: 'Básico' }],
    descripcion: 'Construcción de interfaces web responsivas con React, TypeScript y Tailwind CSS.',
    responsabilidades: ['Maquetación móvil primero, consumo de APIs y accesibilidad WCAG 2.1 AA.'],
    prestaciones: ['Trabajo remoto', 'Seguro de gastos médicos menores', 'Bono anual']
  },
  {
    ...base, identificador: '55555555-5555-5555-5555-555555555503', titulo: 'Administrador de Base de Datos Jr.',
    empresa: 'Empresa de prueba 1', idEmpresa: '11111111-1111-1111-1111-111111111101', inicialesEmpresa: 'E1',
    ubicacion: 'Ciudad de México', modalidad: 'Presencial', salarioMinimo: 20000, salarioMaximo: 26000, activa: false,
    fechaCreacion: haceDias(36), fechaVencimiento: haceDias(5),
    habilidades: [{ nombre: 'SQL Server', nivel: 'Avanzado' }],
    descripcion: 'Administración, respaldos, índices y monitoreo de instancias SQL Server.',
    responsabilidades: ['Optimización de consultas, planes de ejecución y seguridad por esquemas.'],
    prestaciones: ['Prestaciones de ley', 'Vales de despensa']
  }
];
