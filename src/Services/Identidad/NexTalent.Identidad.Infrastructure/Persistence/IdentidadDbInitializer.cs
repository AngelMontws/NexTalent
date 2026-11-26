
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence;

public static class IdentidadDbInitializer
{
    public static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
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
    public static readonly Guid[] Habilidades =
    [
        Guid.Parse("44444444-4444-4444-4444-444444444401"),
        Guid.Parse("44444444-4444-4444-4444-444444444402"),
        Guid.Parse("44444444-4444-4444-4444-444444444403"),
        Guid.Parse("44444444-4444-4444-4444-444444444404")
    ];

    public static async Task EjecutarAsync(IdentidadDbContext db, CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);
        if (await db.Empresas.AnyAsync(e => e.Id == Empresa1, ct)) return;

        var ahora = DateTime.UtcNow;
        var salt = Encoding.UTF8.GetBytes("NexTalent-Dev-Salt-2026");
        var pwd = Environment.GetEnvironmentVariable("SEED_DEFAULT_PASSWORD") ?? "NexTalent2026!";
        var hashDev = $"PBKDF2$100000${Convert.ToBase64String(salt)}${Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(pwd, salt, 100_000, HashAlgorithmName.SHA256, 32))}";

        db.Empresas.AddRange(
            new Empresa { Id = Empresa1, Nombre = "Empresa de prueba 1", Rfc = "EPR260101AA1", Verificada = true, EstadoVerificacion = "Aprobada", CreadaEn = ahora },
            new Empresa { Id = Empresa2, Nombre = "Empresa de prueba 2", Rfc = "EPR260102BB2", Verificada = true, EstadoVerificacion = "Aprobada", CreadaEn = ahora });

        db.Usuarios.AddRange(
            NuevoUsuario(AdminId, "admin@nextalent.mx", "Administrador", null, false, hashDev, ahora),
            NuevoUsuario(Reclutador1, "reclutador1@empresa1.mx", "Reclutador", "+525520000001", true, hashDev, ahora),
            NuevoUsuario(Reclutador2, "reclutador2@empresa2.mx", "Reclutador", "+525520000002", true, hashDev, ahora),
            NuevoUsuario(Candidatos[0], "candidato1@nextalent.mx", "Candidato", "+525510000001", true, hashDev, ahora),
            NuevoUsuario(Candidatos[1], "candidato2@nextalent.mx", "Candidato", "+525510000002", true, hashDev, ahora),
            NuevoUsuario(Candidatos[2], "candidato3@nextalent.mx", "Candidato", "+525510000003", false, hashDev, ahora),
            NuevoUsuario(Candidatos[3], "candidato4@nextalent.mx", "Candidato", null, false, hashDev, ahora));

        db.Reclutadores.AddRange(
            new Reclutador { UsuarioId = Reclutador1, EmpresaId = Empresa1, NombreContacto = "Laura Mendoza", AprobadoEmpresa = true },
            new Reclutador { UsuarioId = Reclutador2, EmpresaId = Empresa2, NombreContacto = "Carlos Ramirez", AprobadoEmpresa = true });

        db.Candidatos.AddRange(
            new Candidato { UsuarioId = Candidatos[0], Nombres = "Ana Sofia", ApellidoPaterno = "Lopez", ApellidoMaterno = "Hernandez", Edad = 24, Ocupacion = "Ingeniera en Informatica", Curp = "LOHA020514MDFRNS01", Calle = "Av. Universidad", NumeroExterior = "120", Colonia = "Narvarte", MunicipioAlcaldia = "Benito Juarez", EstadoMx = "Ciudad de Mexico", CodigoPostal = "03020", Ubicacion = "Ciudad de Mexico", SalarioEsperado = 22000m, ModalidadPreferida = "Hibrido", CvPerfilUrl = "/privado/cv/candidato1.pdf", CompletitudPct = 100 },
            new Candidato { UsuarioId = Candidatos[1], Nombres = "Diego Alejandro", ApellidoPaterno = "Martinez", ApellidoMaterno = "Cruz", Edad = 26, Ocupacion = "Desarrollador Full-Stack", Curp = "MACD000822HDFRNS02", Calle = "Calle Hidalgo", NumeroExterior = "45", Colonia = "Americana", MunicipioAlcaldia = "Guadalajara", EstadoMx = "Jalisco", CodigoPostal = "44160", Ubicacion = "Guadalajara, Jalisco", SalarioEsperado = 25000m, ModalidadPreferida = "Remoto", CvPerfilUrl = "/privado/cv/candidato2.pdf", CompletitudPct = 100 },
            new Candidato { UsuarioId = Candidatos[2], Nombres = "Valeria", ApellidoPaterno = "Sanchez", ApellidoMaterno = "Torres", Edad = 23, Ocupacion = "Desarrolladora Web", Curp = "SATV031109MDFRNS03", Calle = "Av. Revolucion", NumeroExterior = "88", Colonia = "Centro", MunicipioAlcaldia = "Monterrey", EstadoMx = "Nuevo Leon", CodigoPostal = "64000", Ubicacion = "Monterrey, Nuevo Leon", SalarioEsperado = 20000m, ModalidadPreferida = "Presencial", CvPerfilUrl = "/privado/cv/candidato3.pdf", CompletitudPct = 100 },
            new Candidato { UsuarioId = Candidatos[3], Nombres = "Luis", ApellidoPaterno = "Gomez", CompletitudPct = 25 });

        db.CandidatoHabilidades.AddRange(
            new CandidatoHabilidad { CandidatoId = Candidatos[0], HabilidadId = Habilidades[0], Nivel = 3 },
            new CandidatoHabilidad { CandidatoId = Candidatos[0], HabilidadId = Habilidades[1], Nivel = 2 },
            new CandidatoHabilidad { CandidatoId = Candidatos[0], HabilidadId = Habilidades[2], Nivel = 1 },
            new CandidatoHabilidad { CandidatoId = Candidatos[0], HabilidadId = Habilidades[3], Nivel = 1 },
            new CandidatoHabilidad { CandidatoId = Candidatos[1], HabilidadId = Habilidades[0], Nivel = 1 },
            new CandidatoHabilidad { CandidatoId = Candidatos[1], HabilidadId = Habilidades[1], Nivel = 3 },
            new CandidatoHabilidad { CandidatoId = Candidatos[1], HabilidadId = Habilidades[2], Nivel = 3 },
            new CandidatoHabilidad { CandidatoId = Candidatos[2], HabilidadId = Habilidades[1], Nivel = 1 },
            new CandidatoHabilidad { CandidatoId = Candidatos[2], HabilidadId = Habilidades[2], Nivel = 2 },
            new CandidatoHabilidad { CandidatoId = Candidatos[2], HabilidadId = Habilidades[3], Nivel = 2 },
            new CandidatoHabilidad { CandidatoId = Candidatos[3], HabilidadId = Habilidades[0], Nivel = 1 });

        await db.SaveChangesAsync(ct);
    }

    private static Usuario NuevoUsuario(Guid id, string email, string rol, string? tel, bool optIn, string hash, DateTime ahora) => new()
    {
        Id = id,
        Email = email,
        PasswordHash = hash,
        TelefonoWhatsapp = tel,
        Rol = rol,
        WhatsappOptIn = optIn,
        WhatsappOptInFecha = optIn ? ahora : null,
        AvisoPrivacidadVersion = "1.0",
        AvisoPrivacidadAceptadoEn = ahora,
        EmailVerificado = true,
        CreadoEn = ahora
    };
}
