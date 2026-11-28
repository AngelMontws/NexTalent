using System.Net.Mail;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexTalent.Contracts;
using NexTalent.Identidad.Infrastructure.Persistence;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Registro;

public sealed record SolicitudRegistro(string? Email, string? Password, string? Rol, string? Nombre, Guid? EmpresaId);

public sealed record UsuarioRegistrado(
    Guid Id,
    string Email,
    string Rol,
    string Nombre,
    Guid? EmpresaId,
    bool EmailVerificado,
    bool AprobadoEmpresa,
    DateTime CreadoEn);

public enum EstadoRegistro
{
    Creado,
    EmailDuplicado,
    Invalido,
    EmpresaNoEncontrada
}

public sealed record ResultadoRegistro(
    EstadoRegistro Estado,
    UsuarioRegistrado? Usuario = null,
    Dictionary<string, string[]>? Errores = null);

/// <summary>
/// EP-01 / HU-01.01 y HU-01.02. Usuario, perfil y eventos se confirman en una sola transaccion (Outbox).
/// El reclutador queda pendiente de aprobacion de su empresa.
/// </summary>
public sealed class ServicioRegistro(IdentidadDbContext db, TimeProvider reloj, IPublishEndpoint? bus = null)
{
    public const string VersionAviso = "1.0";
    private const int LlaveDuplicada = 2601;
    private const int RestriccionUnica = 2627;

    public async Task<ResultadoRegistro> RegistrarAsync(SolicitudRegistro s, CancellationToken ct)
    {
        var errores = Validar(s, out var email, out var rol, out var nombre);
        if (errores.Count > 0)
        {
            return new(EstadoRegistro.Invalido, Errores: errores);
        }

        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
        {
            return new(EstadoRegistro.EmailDuplicado);
        }

        if (rol == "Reclutador"
            && !await db.Empresas.AsNoTracking().AnyAsync(e => e.Id == s.EmpresaId!.Value, ct))
        {
            return new(EstadoRegistro.EmpresaNoEncontrada);
        }

        var ahora = reloj.GetUtcNow();
        var usuario = new Usuario
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            PasswordHash = PasswordHasher.Hashear(s.Password!),
            Rol = rol,
            WhatsappOptIn = false,
            AvisoPrivacidadVersion = VersionAviso,
            AvisoPrivacidadAceptadoEn = ahora.UtcDateTime,
            EmailVerificado = false,
            CreadoEn = ahora.UtcDateTime
        };
        db.Usuarios.Add(usuario);

        Guid? empresaId = null;
        if (rol == "Candidato")
        {
            var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            db.Candidatos.Add(new Candidato
            {
                UsuarioId = usuario.Id,
                Nombres = partes.Length > 1 ? string.Join(' ', partes[..^1]) : partes[0],
                ApellidoPaterno = partes.Length > 1 ? partes[^1] : "",
                CompletitudPct = 25
            });
        }
        else
        {
            empresaId = s.EmpresaId;
            db.Reclutadores.Add(new Reclutador
            {
                UsuarioId = usuario.Id,
                EmpresaId = empresaId!.Value,
                NombreContacto = nombre,
                AprobadoEmpresa = false
            });
        }

        if (bus is not null)
        {
            await bus.Publish(new ContactoActualizado(
                Guid.CreateVersion7(), ahora, usuario.Id, rol, empresaId, email, null, false, null, 1), ct);

            if (rol == "Candidato")
            {
                await bus.Publish(new PerfilHabilidadesActualizado(
                    Guid.CreateVersion7(), ahora, usuario.Id, [], true, true, 1), ct);
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: LlaveDuplicada or RestriccionUnica })
        {
            return new(EstadoRegistro.EmailDuplicado);
        }

        return new(EstadoRegistro.Creado, new UsuarioRegistrado(
            usuario.Id, usuario.Email, usuario.Rol, nombre, empresaId,
            usuario.EmailVerificado, false, usuario.CreadoEn));
    }

    private static Dictionary<string, string[]> Validar(
        SolicitudRegistro s, out string email, out string rol, out string nombre)
    {
        var errores = new Dictionary<string, string[]>();
        email = (s.Email ?? "").Trim().ToLowerInvariant();
        nombre = (s.Nombre ?? "").Trim();
        rol = "";

        if (email.Length is 0 or > 256 || !MailAddress.TryCreate(email, out var dir) || dir.Address != email)
        {
            errores["email"] = ["Correo no valido."];
        }

        var p = s.Password ?? "";
        if (p.Length is < 8 or > 128 || !p.Any(char.IsUpper) || !p.Any(char.IsDigit))
        {
            errores["password"] = ["Minimo 8 caracteres, una mayuscula y un numero."];
        }

        if (nombre.Length is 0 or > 100)
        {
            errores["nombre"] = ["El nombre es obligatorio (maximo 100 caracteres)."];
        }

        if (string.Equals(s.Rol?.Trim(), "Candidato", StringComparison.OrdinalIgnoreCase))
        {
            rol = "Candidato";
        }
        else if (string.Equals(s.Rol?.Trim(), "Reclutador", StringComparison.OrdinalIgnoreCase))
        {
            rol = "Reclutador";
            if (s.EmpresaId is null || s.EmpresaId == Guid.Empty)
            {
                errores["empresaId"] = ["El reclutador debe indicar la empresa."];
            }
        }
        else
        {
            errores["rol"] = ["Rol permitido: Candidato o Reclutador."];
        }

        return errores;
    }
}