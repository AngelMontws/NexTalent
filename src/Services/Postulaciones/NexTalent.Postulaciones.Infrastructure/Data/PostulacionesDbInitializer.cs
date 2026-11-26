using Microsoft.EntityFrameworkCore;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Infrastructure.Data;

/// <summary>
/// Solo Development (DatabaseSeed:RunInitializers): aplica migraciones y carga replicas de prueba con los GUID
/// acordados con Base de Datos. Datos ficticios. Se puede repetir sin duplicar.
/// </summary>
public static class PostulacionesDbInitializer
{
    public static readonly Guid Empresa1 = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid Empresa2 = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid[] Candidatos =
    [
        Guid.Parse("33333333-3333-3333-3333-333333333301"),
        Guid.Parse("33333333-3333-3333-3333-333333333302"),
        Guid.Parse("33333333-3333-3333-3333-333333333303"),
        Guid.Parse("33333333-3333-3333-3333-333333333304")
    ];
    public static readonly Guid[] Habilidades =
    [
        Guid.Parse("44444444-4444-4444-4444-444444444401"),
        Guid.Parse("44444444-4444-4444-4444-444444444402"),
        Guid.Parse("44444444-4444-4444-4444-444444444403"),
        Guid.Parse("44444444-4444-4444-4444-444444444404")
    ];
    public static readonly Guid Vacante1 = Guid.Parse("55555555-5555-5555-5555-555555555501");
    public static readonly Guid Vacante2 = Guid.Parse("55555555-5555-5555-5555-555555555502");
    public static readonly Guid Vacante3 = Guid.Parse("55555555-5555-5555-5555-555555555503");

    public static async Task EjecutarAsync(PostulacionesDbContext db, TimeProvider reloj, CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);
        if (await db.Vacantes.AnyAsync(v => v.VacanteId == Vacante1, ct))
        {
            return;
        }

        var ahora = reloj.GetUtcNow();
        db.Vacantes.AddRange(
            Vacante(Vacante1, Empresa1, "Desarrollador Backend .NET Jr.", "Empresa de prueba 1", true, ahora),
            Vacante(Vacante2, Empresa2, "Desarrollador Frontend Jr.", "Empresa de prueba 2", true, ahora),
            Vacante(Vacante3, Empresa1, "Administrador de Base de Datos Jr.", "Empresa de prueba 1", false, ahora));

        // Vacante 1: C# intermedio y SQL basico requeridas; Docker deseable.
        db.VacanteHabilidades.AddRange(
            HabilidadVacante(Vacante1, 0, NivelHabilidad.Intermedio, true),
            HabilidadVacante(Vacante1, 1, NivelHabilidad.Basico, true),
            HabilidadVacante(Vacante1, 3, NivelHabilidad.Basico, false),
            HabilidadVacante(Vacante2, 2, NivelHabilidad.Intermedio, true),
            HabilidadVacante(Vacante2, 3, NivelHabilidad.Basico, true),
            HabilidadVacante(Vacante3, 1, NivelHabilidad.Avanzado, true));

        // Candidatos 1 a 3 completos con distinta coincidencia; el 4 sin contacto y con 1 habilidad.
        Candidato(db, 0, true, [NivelHabilidad.Avanzado, NivelHabilidad.Intermedio, NivelHabilidad.Basico, NivelHabilidad.Basico]);
        Candidato(db, 1, true, [NivelHabilidad.Basico, NivelHabilidad.Avanzado, NivelHabilidad.Avanzado, null]);
        Candidato(db, 2, true, [null, NivelHabilidad.Basico, NivelHabilidad.Intermedio, NivelHabilidad.Intermedio]);
        Candidato(db, 3, false, [NivelHabilidad.Basico, null, null, null]);

        await db.SaveChangesAsync(ct);
    }

    private static VacanteReplica Vacante(
        Guid id, Guid empresa, string titulo, string nombreEmpresa, bool abierta, DateTimeOffset ahora) => new()
    {
        VacanteId = id,
        EmpresaId = empresa,
        Titulo = titulo,
        NombreEmpresa = nombreEmpresa,
        Abierta = abierta,
        ActualizadaEn = ahora,
        VersionEvento = 1
    };

    private static VacanteHabilidadReplica HabilidadVacante(Guid vacante, int habilidad, NivelHabilidad nivel, bool requerida) =>
        new() { VacanteId = vacante, HabilidadId = Habilidades[habilidad], Nivel = nivel, Requerida = requerida };

    private static void Candidato(PostulacionesDbContext db, int indice, bool tieneContacto, NivelHabilidad?[] niveles)
    {
        var id = Candidatos[indice];
        db.Candidatos.Add(new CandidatoReplica
        {
            CandidatoId = id, TieneNombre = true, TieneContacto = tieneContacto, VersionEvento = 1
        });
        for (var i = 0; i < niveles.Length; i++)
        {
            if (niveles[i] is { } nivel)
            {
                db.CandidatoHabilidades.Add(new CandidatoHabilidadReplica
                {
                    CandidatoId = id, HabilidadId = Habilidades[i], Nivel = nivel
                });
            }
        }
    }
}
