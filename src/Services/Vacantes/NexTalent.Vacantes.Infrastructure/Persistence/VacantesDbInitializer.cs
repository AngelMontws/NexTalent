using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexTalent.Vacantes.Infrastructure.Persistence.Entities;

namespace NexTalent.Vacantes.Infrastructure.Persistence;

/// <summary>
/// Solo Development (DatabaseSeed:RunInitializers): aplica migraciones y carga el catalogo de habilidades,
/// empresas verificadas y vacantes de prueba con los GUID deterministicos acordados con Base de Datos.
/// Con DatabaseSeed:SeedBenchmark50k = true inserta por lotes hasta 50,000 vacantes para RNF-01.
/// </summary>
public static class VacantesDbInitializer
{
    public static readonly Guid Empresa1 = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid Empresa2 = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid Reclutador1 = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid Reclutador2 = Guid.Parse("22222222-2222-2222-2222-222222222202");

    public static readonly Guid[] Habilidades =
    [
        Guid.Parse("44444444-4444-4444-4444-444444444401"),
        Guid.Parse("44444444-4444-4444-4444-444444444402"),
        Guid.Parse("44444444-4444-4444-4444-444444444403"),
        Guid.Parse("44444444-4444-4444-4444-444444444404"),
        Guid.Parse("44444444-4444-4444-4444-444444444405"),
        Guid.Parse("44444444-4444-4444-4444-444444444406"),
        Guid.Parse("44444444-4444-4444-4444-444444444407"),
        Guid.Parse("44444444-4444-4444-4444-444444444408")
    ];

    public static readonly Guid Vacante1 = Guid.Parse("55555555-5555-5555-5555-555555555501");
    public static readonly Guid Vacante2 = Guid.Parse("55555555-5555-5555-5555-555555555502");
    public static readonly Guid Vacante3 = Guid.Parse("55555555-5555-5555-5555-555555555503");

    public static async Task EjecutarAsync(
        VacantesDbContext db, bool seedBenchmark50k = false, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Empresas.AnyAsync(e => e.EmpresaId == Empresa1, ct))
        {
            var ahora = DateTime.UtcNow;

            db.Empresas.AddRange(
                new EmpresaResumen
                {
                    EmpresaId = Empresa1,
                    Nombre = "Empresa de prueba 1",
                    Verificada = true,
                    IndiceRespuestaPct = 100m,
                    ActualizadoEn = ahora
                },
                new EmpresaResumen
                {
                    EmpresaId = Empresa2,
                    Nombre = "Empresa de prueba 2",
                    Verificada = true,
                    IndiceRespuestaPct = 100m,
                    ActualizadoEn = ahora
                });

            db.Habilidades.AddRange(
                new Habilidad { Id = Habilidades[0], Nombre = "C# / .NET", Categoria = "Backend", EstadoAprobacion = "Aprobada" },
                new Habilidad { Id = Habilidades[1], Nombre = "SQL Server", Categoria = "Base de Datos", EstadoAprobacion = "Aprobada" },
                new Habilidad { Id = Habilidades[2], Nombre = "React / TypeScript", Categoria = "Frontend", EstadoAprobacion = "Aprobada" },
                new Habilidad { Id = Habilidades[3], Nombre = "Docker", Categoria = "DevOps", EstadoAprobacion = "Aprobada" },
                new Habilidad { Id = Habilidades[4], Nombre = "Git", Categoria = "Herramientas", EstadoAprobacion = "Aprobada" },
                new Habilidad { Id = Habilidades[5], Nombre = "REST API", Categoria = "Backend", EstadoAprobacion = "Aprobada" },
                new Habilidad { Id = Habilidades[6], Nombre = "Excel Intermedio", Categoria = "Administración", EstadoAprobacion = "Aprobada" },
                new Habilidad { Id = Habilidades[7], Nombre = "Atención a Clientes", Categoria = "Operaciones", EstadoAprobacion = "Aprobada" });

            db.Vacantes.AddRange(
                new Vacante
                {
                    Id = Vacante1,
                    EmpresaId = Empresa1,
                    ReclutadorId = Reclutador1,
                    Titulo = "Desarrollador Backend .NET Jr.",
                    Descripcion = "Desarrollo y mantenimiento de microservicios en ASP.NET Core y SQL Server.",
                    Actividades = "Implementar endpoints REST, consultas optimizadas y pruebas unitarias.",
                    Beneficios = "Prestaciones superiores a la ley, trabajo híbrido y capacitación continua.",
                    SalarioMin = 18000m,
                    SalarioMax = 24000m,
                    TipoSalario = "Bruto",
                    Modalidad = "Hibrido",
                    Jornada = "Tiempo completo",
                    Ubicacion = "Ciudad de Mexico",
                    Estado = "Publicada",
                    PublicadaEn = ahora,
                    ExpiraEn = ahora.AddDays(30),
                    VersionEvento = 1,
                    CreadaEn = ahora
                },
                new Vacante
                {
                    Id = Vacante2,
                    EmpresaId = Empresa2,
                    ReclutadorId = Reclutador2,
                    Titulo = "Desarrollador Frontend Jr.",
                    Descripcion = "Construcción de interfaces web responsivas con React, TypeScript y Tailwind CSS.",
                    Actividades = "Maquetación móvil primero, consumo de APIs y accesibilidad WCAG 2.1 AA.",
                    Beneficios = "Trabajo remoto, seguro de gastos médicos menores y bono anual.",
                    SalarioMin = 17000m,
                    SalarioMax = 23000m,
                    TipoSalario = "Bruto",
                    Modalidad = "Remoto",
                    Jornada = "Tiempo completo",
                    Ubicacion = "Guadalajara, Jalisco",
                    Estado = "Publicada",
                    PublicadaEn = ahora,
                    ExpiraEn = ahora.AddDays(30),
                    VersionEvento = 1,
                    CreadaEn = ahora
                },
                new Vacante
                {
                    Id = Vacante3,
                    EmpresaId = Empresa1,
                    ReclutadorId = Reclutador1,
                    Titulo = "Administrador de Base de Datos Jr.",
                    Descripcion = "Administración, respaldos, índices y monitoreo de instancias SQL Server.",
                    Actividades = "Optimización de consultas, planes de ejecución y seguridad por esquemas.",
                    Beneficios = "Prestaciones de ley y vales de despensa.",
                    SalarioMin = 20000m,
                    SalarioMax = 26000m,
                    TipoSalario = "Bruto",
                    Modalidad = "Presencial",
                    Jornada = "Tiempo completo",
                    Ubicacion = "Ciudad de Mexico",
                    Estado = "Cerrada",
                    PublicadaEn = ahora.AddDays(-35),
                    ExpiraEn = ahora.AddDays(-5),
                    VersionEvento = 2,
                    CreadaEn = ahora.AddDays(-36)
                });

            // Paridad exacta con PostulacionesDbInitializer:
            // Vacante 1: C# intermedio (2) y SQL basico (1) requeridas; Docker basico (1) deseable.
            // Vacante 2: React intermedio (2) y Docker basico (1) requeridas.
            // Vacante 3: SQL avanzado (3) requerida.
            db.VacanteHabilidades.AddRange(
                new VacanteHabilidad { VacanteId = Vacante1, HabilidadId = Habilidades[0], Nivel = 2, EsRequerida = true },
                new VacanteHabilidad { VacanteId = Vacante1, HabilidadId = Habilidades[1], Nivel = 1, EsRequerida = true },
                new VacanteHabilidad { VacanteId = Vacante1, HabilidadId = Habilidades[3], Nivel = 1, EsRequerida = false },
                new VacanteHabilidad { VacanteId = Vacante2, HabilidadId = Habilidades[2], Nivel = 2, EsRequerida = true },
                new VacanteHabilidad { VacanteId = Vacante2, HabilidadId = Habilidades[3], Nivel = 1, EsRequerida = true },
                new VacanteHabilidad { VacanteId = Vacante3, HabilidadId = Habilidades[1], Nivel = 3, EsRequerida = true });

            await db.SaveChangesAsync(ct);
        }

        if (seedBenchmark50k)
        {
            await CargarBenchmark50kAsync(db, ct);
        }
    }

    private static async Task CargarBenchmark50kAsync(VacantesDbContext db, CancellationToken ct)
    {
        const int meta = 50_000;
        var actuales = await db.Vacantes.CountAsync(ct);
        if (actuales >= meta)
        {
            return;
        }

        var faltantes = meta - actuales;
        var ahora = DateTime.UtcNow;
        string[] puestos =
        [
            "Desarrollador Backend .NET", "Analista de Datos SQL", "Desarrollador Frontend React",
            "Ingeniero DevOps Docker", "Soporte Técnico TI", "Ejecutivo de Atención a Clientes",
            "Coordinador de Logística", "Auxiliar Administrativo"
        ];
        string[] modalidades = ["Presencial", "Hibrido", "Remoto"];
        string[] jornadas = ["Tiempo completo", "Medio tiempo", "Por turnos"];
        string[] ubicaciones =
        [
            "Ciudad de Mexico", "Guadalajara, Jalisco", "Monterrey, Nuevo Leon",
            "Puebla, Puebla", "Queretaro, Queretaro", "Tijuana, Baja California"
        ];
        string[] estados = ["Publicada", "Publicada", "Publicada", "Publicada", "Cerrada", "Caducada"];

        var tablaVacantes = new DataTable();
        tablaVacantes.Columns.Add("id", typeof(Guid));
        tablaVacantes.Columns.Add("empresa_id", typeof(Guid));
        tablaVacantes.Columns.Add("reclutador_id", typeof(Guid));
        tablaVacantes.Columns.Add("titulo", typeof(string));
        tablaVacantes.Columns.Add("descripcion", typeof(string));
        tablaVacantes.Columns.Add("actividades", typeof(string));
        tablaVacantes.Columns.Add("beneficios", typeof(string));
        tablaVacantes.Columns.Add("salario_min", typeof(decimal));
        tablaVacantes.Columns.Add("salario_max", typeof(decimal));
        tablaVacantes.Columns.Add("tipo_salario", typeof(string));
        tablaVacantes.Columns.Add("modalidad", typeof(string));
        tablaVacantes.Columns.Add("jornada", typeof(string));
        tablaVacantes.Columns.Add("ubicacion", typeof(string));
        tablaVacantes.Columns.Add("estado", typeof(string));
        tablaVacantes.Columns.Add("publicada_en", typeof(DateTime));
        tablaVacantes.Columns.Add("expira_en", typeof(DateTime));
        tablaVacantes.Columns.Add("recordatorio_27d_enviado", typeof(bool));
        tablaVacantes.Columns.Add("version_evento", typeof(long));
        tablaVacantes.Columns.Add("creada_en", typeof(DateTime));

        var tablaPuente = new DataTable();
        tablaPuente.Columns.Add("vacante_id", typeof(Guid));
        tablaPuente.Columns.Add("habilidad_id", typeof(Guid));
        tablaPuente.Columns.Add("nivel", typeof(byte));
        tablaPuente.Columns.Add("es_requerida", typeof(bool));

        for (var i = 0; i < faltantes; i++)
        {
            var id = Guid.CreateVersion7();
            var par = i % 2 == 0;
            var salarioMin = 10000m + ((i % 35) * 1000m);
            var salarioMax = salarioMin + 4000m + ((i % 5) * 1500m);
            var publicada = ahora.AddDays(-(i % 25));

            tablaVacantes.Rows.Add(
                id,
                par ? Empresa1 : Empresa2,
                par ? Reclutador1 : Reclutador2,
                $"{puestos[i % puestos.Length]} #{actuales + i + 1}",
                $"Vacante de prueba para evaluacion de rendimiento RNF-01 con puesto {puestos[i % puestos.Length]} y tecnologias empresariales.",
                "Colaboracion en equipo agil y cumplimiento de objetivos operativos.",
                "Prestaciones de ley y capacitacion.",
                salarioMin,
                salarioMax,
                i % 3 == 0 ? "Neto" : "Bruto",
                modalidades[i % modalidades.Length],
                jornadas[i % jornadas.Length],
                ubicaciones[i % ubicaciones.Length],
                estados[i % estados.Length],
                publicada,
                publicada.AddDays(30),
                false,
                1L,
                publicada);

            tablaPuente.Rows.Add(id, Habilidades[i % Habilidades.Length], (byte)((i % 3) + 1), true);
            tablaPuente.Rows.Add(id, Habilidades[(i + 1) % Habilidades.Length], (byte)(((i + 1) % 3) + 1), false);
        }

        var conexion = (SqlConnection)db.Database.GetDbConnection();
        var cerrada = conexion.State == ConnectionState.Closed;
        if (cerrada)
        {
            await conexion.OpenAsync(ct);
        }

        try
        {
            using (var bulkVacantes = new SqlBulkCopy(conexion)
            {
                DestinationTableName = "[vacantes].[vacante]",
                BatchSize = 5000
            })
            {
                foreach (DataColumn col in tablaVacantes.Columns)
                {
                    bulkVacantes.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                }

                await bulkVacantes.WriteToServerAsync(tablaVacantes, ct);
            }

            using (var bulkPuente = new SqlBulkCopy(conexion)
            {
                DestinationTableName = "[vacantes].[vacante_habilidad]",
                BatchSize = 5000
            })
            {
                foreach (DataColumn col in tablaPuente.Columns)
                {
                    bulkPuente.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                }

                await bulkPuente.WriteToServerAsync(tablaPuente, ct);
            }
        }
        finally
        {
            if (cerrada)
            {
                await conexion.CloseAsync();
            }
        }
    }
}
