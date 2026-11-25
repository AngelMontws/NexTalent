using NexTalent.Contracts;
using NexTalent.Notificaciones.Domain;

namespace NexTalent.Notificaciones.Application;

// Interfaces que implementa Infrastructure. Application no conoce SQL Server, Meta ni el correo.

public sealed record Contacto(Guid UsuarioId, string Email, string? TelefonoWhatsapp, bool WhatsappOptIn);

public interface IContactos
{
    Task<Contacto?> ObtenerAsync(Guid usuarioId, CancellationToken ct);
    Task<Contacto?> BuscarPorTelefonoAsync(string telefono, CancellationToken ct);
    Task ActivarWhatsappAsync(Guid usuarioId, string telefono, DateTimeOffset fechaConsentimiento, CancellationToken ct);
    Task DesactivarWhatsappAsync(Guid usuarioId, DateTimeOffset fecha, CancellationToken ct);
}

public interface IVinculaciones
{
    /// <summary>Devuelve una nueva si el usuario no tiene.</summary>
    Task<VinculacionWhatsapp> ObtenerAsync(Guid usuarioId, CancellationToken ct);
    Task GuardarAsync(VinculacionWhatsapp vinculacion, CancellationToken ct);
}

public interface INotificaciones
{
    Task<bool> ExisteEventoAsync(Guid eventoId, CancellationToken ct);
    Task<int> UltimaSecuenciaAsync(Guid postulacionId, CancellationToken ct);

    /// <summary>Guarda el aviso y su evento, para poder reconstruir el texto al reintentar.</summary>
    Task AgregarAsync(Notificacion notificacion, EstatusCambiado evento, CancellationToken ct);
    Task GuardarAsync(Notificacion notificacion, CancellationToken ct);
    Task<IReadOnlyList<(Notificacion Notificacion, EstatusCambiado Evento)>> ListarPorEnviarAsync(
        DateTimeOffset ahora, CancellationToken ct);
}

public sealed record ResultadoEnvio(bool Exito, string? IdExterno, string? CodigoError, bool Permanente)
{
    public static ResultadoEnvio Ok(string? idExterno = null) => new(true, idExterno, null, false);
    public static ResultadoEnvio Fallo(string codigo, bool permanente = false) => new(false, null, codigo, permanente);
}

/// <summary>Dos implementaciones previstas: Meta y simulador.</summary>
public interface IWhatsapp
{
    Task<ResultadoEnvio> EnviarCodigoAsync(string telefono, string codigo, CancellationToken ct);
    Task<ResultadoEnvio> EnviarPlantillaAsync(string telefono, string plantilla, string texto, CancellationToken ct);

    /// <summary>Solo dentro de la ventana de 24 h que abre la persona al escribir.</summary>
    Task<ResultadoEnvio> EnviarTextoAsync(string telefono, string texto, CancellationToken ct);
}

public interface ICorreo
{
    Task<ResultadoEnvio> EnviarAsync(string email, string asunto, string texto, CancellationToken ct);
}
