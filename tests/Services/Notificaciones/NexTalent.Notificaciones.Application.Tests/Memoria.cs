using NexTalent.Contracts;
using NexTalent.Notificaciones.Domain;

namespace NexTalent.Notificaciones.Application.Tests;

/// <summary>Sustituye base de datos, Meta, correo y reloj en las pruebas.</summary>
internal sealed class Memoria : TimeProvider, IContactos, IVinculaciones, INotificaciones, IWhatsapp, ICorreo
{
    private readonly Dictionary<Guid, VinculacionWhatsapp> _vinculaciones = [];

    public DateTimeOffset Ahora { get; set; } = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);
    public Dictionary<Guid, Contacto> Contactos { get; } = [];
    public List<(Notificacion Notificacion, EstatusCambiado Evento)> Avisos { get; } = [];
    public List<string> Codigos { get; } = [];
    public List<(string Destino, string Texto)> Whatsapps { get; } = [];
    public List<(string Destino, string Texto)> Correos { get; } = [];
    public Func<ResultadoEnvio> RespuestaWhatsapp { get; set; } = () => ResultadoEnvio.Ok("wamid");
    public Func<ResultadoEnvio> RespuestaCorreo { get; set; } = () => ResultadoEnvio.Ok();
    public DateTimeOffset? FechaConsentimiento { get; private set; }

    public override DateTimeOffset GetUtcNow() => Ahora;

    Task<Contacto?> IContactos.ObtenerAsync(Guid usuarioId, CancellationToken ct) =>
        Task.FromResult(Contactos.GetValueOrDefault(usuarioId));

    public Task<Contacto?> BuscarPorTelefonoAsync(string telefono, CancellationToken ct) =>
        Task.FromResult(Contactos.Values.FirstOrDefault(c => c.TelefonoWhatsapp == telefono));

    public Dictionary<string, Guid> UsuariosWhatsapp { get; } = [];

    public Task<Contacto?> BuscarPorUsuarioWhatsappAsync(string usuarioWhatsapp, CancellationToken ct) =>
        Task.FromResult(UsuariosWhatsapp.TryGetValue(usuarioWhatsapp, out var id) ? Contactos[id] : null);

    public Task ActivarWhatsappAsync(Guid usuarioId, string telefono, DateTimeOffset fecha, CancellationToken ct)
    {
        Contactos[usuarioId] = Contactos[usuarioId] with { TelefonoWhatsapp = telefono, WhatsappOptIn = true };
        FechaConsentimiento = fecha;
        return Task.CompletedTask;
    }

    public Task DesactivarWhatsappAsync(Guid usuarioId, DateTimeOffset fecha, CancellationToken ct)
    {
        Contactos[usuarioId] = Contactos[usuarioId] with { WhatsappOptIn = false };
        return Task.CompletedTask;
    }

    Task<VinculacionWhatsapp> IVinculaciones.ObtenerAsync(Guid usuarioId, CancellationToken ct) =>
        Task.FromResult(_vinculaciones.GetValueOrDefault(usuarioId) ?? new VinculacionWhatsapp(usuarioId));

    public Task GuardarAsync(VinculacionWhatsapp vinculacion, CancellationToken ct)
    {
        _vinculaciones[vinculacion.UsuarioId] = vinculacion;
        return Task.CompletedTask;
    }

    public Task<bool> ExisteEventoAsync(Guid eventoId, CancellationToken ct) =>
        Task.FromResult(Avisos.Exists(a => a.Evento.EventoId == eventoId));

    public Task<int> UltimaSecuenciaAsync(Guid postulacionId, CancellationToken ct) => Task.FromResult(
        Avisos.Where(a => a.Evento.PostulacionId == postulacionId).Select(a => a.Evento.Secuencia).DefaultIfEmpty().Max());

    public Task AgregarAsync(Notificacion notificacion, EstatusCambiado evento, CancellationToken ct)
    {
        Avisos.Add((notificacion, evento));
        return Task.CompletedTask;
    }

    public Task GuardarAsync(Notificacion notificacion, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<(Notificacion Notificacion, EstatusCambiado Evento)>> ListarPorEnviarAsync(
        DateTimeOffset ahora, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<(Notificacion, EstatusCambiado)>>(
            Avisos.FindAll(a => a.Notificacion.LeToca(ahora)));

    public Task<ResultadoEnvio> EnviarCodigoAsync(string telefono, string codigo, CancellationToken ct)
    {
        Codigos.Add(codigo);
        return Task.FromResult(RespuestaWhatsapp());
    }

    public Task<ResultadoEnvio> EnviarPlantillaAsync(string telefono, string plantilla, string texto, CancellationToken ct) =>
        EnviarTextoAsync(telefono, texto, ct);

    public Task<ResultadoEnvio> EnviarTextoAsync(string telefono, string texto, CancellationToken ct)
    {
        Whatsapps.Add((telefono, texto));
        return Task.FromResult(RespuestaWhatsapp());
    }

    public Task<ResultadoEnvio> EnviarAsync(string email, string asunto, string texto, CancellationToken ct)
    {
        Correos.Add((email, texto));
        return Task.FromResult(RespuestaCorreo());
    }
}
