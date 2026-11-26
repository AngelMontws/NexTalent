using Microsoft.EntityFrameworkCore;

namespace NexTalent.Notificaciones.Infrastructure.Data;

/// <summary>
/// Solo Development (DatabaseSeed:RunInitializers): aplica migraciones y carga replicas de contacto con los
/// mismos GUID deterministicos usados en Identidad, Vacantes y Postulaciones. Se puede repetir sin duplicar.
/// </summary>
public static class NotificacionesDbInitializer
{
    public static readonly Guid Empresa1 = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid Empresa2 = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid Reclutador1 = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid Reclutador2 = Guid.Parse("22222222-2222-2222-2222-222222222202");

    public static readonly Guid[] Candidatos =
    [
        Guid.Parse("33333333-3333-3333-3333-333333333301"),
        Guid.Parse("33333333-3333-3333-3333-333333333302"),
        Guid.Parse("33333333-3333-3333-3333-333333333303"),
        Guid.Parse("33333333-3333-3333-3333-333333333304")
    ];

    public static async Task EjecutarAsync(NotificacionesDbContext db, TimeProvider reloj, CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);
        if (await db.Contactos.AnyAsync(c => c.UsuarioId == Reclutador1, ct))
        {
            return;
        }

        var ahora = reloj.GetUtcNow();
        db.Contactos.AddRange(
            Contacto(Reclutador1, "Reclutador", Empresa1, "reclutador1@empresa1.mx", "+525520000001", true, "Ninguna", ahora),
            Contacto(Reclutador2, "Reclutador", Empresa2, "reclutador2@empresa2.mx", "+525520000002", true, "Ninguna", ahora),
            Contacto(Candidatos[0], "Candidato", null, "candidato1@nextalent.mx", "+525510000001", true, "Diaria", ahora),
            Contacto(Candidatos[1], "Candidato", null, "candidato2@nextalent.mx", "+525510000002", true, "Semanal", ahora),
            Contacto(Candidatos[2], "Candidato", null, "candidato3@nextalent.mx", "+525510000003", false, "Ninguna", ahora),
            Contacto(Candidatos[3], "Candidato", null, "candidato4@nextalent.mx", null, false, "Ninguna", ahora));

        await db.SaveChangesAsync(ct);
    }

    private static ContactoReplica Contacto(
        Guid id, string rol, Guid? empresaId, string email, string? telefono, bool optIn, string frecuencia, DateTimeOffset ahora) => new()
    {
        UsuarioId = id,
        Rol = rol,
        EmpresaId = empresaId,
        Email = email,
        TelefonoWhatsapp = telefono,
        WhatsappOptIn = optIn,
        WhatsappOptInFecha = optIn ? ahora : null,
        FrecuenciaAlertas = frecuencia,
        VersionEvento = 1
    };
}
