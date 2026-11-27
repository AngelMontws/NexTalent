using System.Net.Http.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexTalent.Contracts;
using NexTalent.Notificaciones.Application;
using NexTalent.Notificaciones.Domain;

namespace NexTalent.Notificaciones.Infrastructure.Data;

public sealed class ContactosRepositorio(
    NotificacionesDbContext db, TimeProvider reloj, IPublishEndpoint? bus = null) : IContactos
{
    public async Task<Contacto?> ObtenerAsync(Guid usuarioId, CancellationToken ct) =>
        Mapear(await db.Contactos.AsNoTracking().FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct));

    public async Task<Contacto?> BuscarPorTelefonoAsync(string telefono, CancellationToken ct) =>
        Mapear(await db.Contactos.AsNoTracking().FirstOrDefaultAsync(x => x.TelefonoWhatsapp == telefono, ct));

    public async Task<Contacto?> BuscarPorUsuarioWhatsappAsync(string usuarioWhatsapp, CancellationToken ct) =>
        Mapear(await db.Contactos.AsNoTracking().FirstOrDefaultAsync(x => x.WhatsappUserId == usuarioWhatsapp, ct));

    public async Task ActivarWhatsappAsync(
        Guid usuarioId, string telefono, DateTimeOffset fechaConsentimiento, CancellationToken ct)
    {
        var fila = await db.Contactos.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct)
            ?? throw new InvalidOperationException("El contacto del usuario no existe en la replica.");

        fila.TelefonoWhatsapp = telefono;
        fila.WhatsappOptIn = true;
        fila.WhatsappOptInFecha = fechaConsentimiento;
        fila.WhatsappBajaFecha = null;
        fila.VersionEvento++;

        if (bus is not null)
        {
            await bus.Publish(new ConsentimientoWhatsappCambiado(
                Guid.CreateVersion7(),
                reloj.GetUtcNow(),
                fila.UsuarioId,
                fila.Rol,
                fila.EmpresaId,
                fila.TelefonoWhatsapp,
                true,
                fila.WhatsappOptInFecha,
                fila.VersionEvento), ct);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DesactivarWhatsappAsync(Guid usuarioId, DateTimeOffset fecha, CancellationToken ct)
    {
        var fila = await db.Contactos.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct);
        if (fila is null)
        {
            return;
        }

        fila.WhatsappOptIn = false;
        fila.WhatsappBajaFecha = fecha;
        fila.VersionEvento++;

        if (bus is not null)
        {
            await bus.Publish(new ConsentimientoWhatsappCambiado(
                Guid.CreateVersion7(),
                reloj.GetUtcNow(),
                fila.UsuarioId,
                fila.Rol,
                fila.EmpresaId,
                fila.TelefonoWhatsapp,
                false,
                fila.WhatsappOptInFecha,
                fila.VersionEvento), ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static Contacto? Mapear(ContactoReplica? c) =>
        c is null ? null : new Contacto(c.UsuarioId, c.Email, c.TelefonoWhatsapp, c.WhatsappOptIn);
}

public sealed class VinculacionesRepositorio(NotificacionesDbContext db, TimeProvider reloj) : IVinculaciones
{
    public async Task<VinculacionWhatsapp> ObtenerAsync(Guid usuarioId, CancellationToken ct)
    {
        var fila = await db.CodigosWhatsapp.AsNoTracking().FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct);
        return fila is null
            ? new VinculacionWhatsapp(usuarioId)
            : VinculacionWhatsapp.Rehidratar(
                fila.UsuarioId,
                fila.Telefono,
                fila.CodigoHash,
                fila.Intentos,
                fila.EnviadoEn,
                fila.BloqueadoHasta);
    }

    public async Task GuardarAsync(VinculacionWhatsapp vinculacion, CancellationToken ct)
    {
        var fila = await db.CodigosWhatsapp.FirstOrDefaultAsync(x => x.UsuarioId == vinculacion.UsuarioId, ct);
        if (fila is null)
        {
            fila = new CodigoWhatsappFila
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = vinculacion.UsuarioId
            };
            db.CodigosWhatsapp.Add(fila);
        }

        var hash = vinculacion.ObtenerHash();
        var teniaHash = fila.CodigoHash is not null;

        fila.Telefono = vinculacion.Telefono;
        fila.CodigoHash = hash;
        fila.EnviadoEn = vinculacion.EnviadoEn;
        fila.ExpiraEn = vinculacion.EnviadoEn is { } env ? env + VinculacionWhatsapp.Vigencia : null;
        fila.Intentos = vinculacion.Intentos;
        fila.BloqueadoHasta = vinculacion.BloqueadoHasta;

        if (teniaHash && hash is null && vinculacion.BloqueadoHasta is null && vinculacion.Intentos == 0)
        {
            fila.UsadoEn = reloj.GetUtcNow();
        }
        else if (hash is not null)
        {
            fila.UsadoEn = null;
        }

        await db.SaveChangesAsync(ct);
    }
}

public sealed class NotificacionesRepositorio(NotificacionesDbContext db, TimeProvider reloj) : INotificaciones
{
    public Task<bool> ExisteEventoAsync(Guid eventoId, CancellationToken ct) =>
        db.Notificaciones.AnyAsync(x => x.EventoId == eventoId, ct);

    public async Task<int> UltimaSecuenciaAsync(Guid postulacionId, CancellationToken ct) =>
        await db.Notificaciones
            .Where(x => x.PostulacionId == postulacionId && x.Secuencia != null)
            .Select(x => x.Secuencia!.Value)
            .DefaultIfEmpty()
            .MaxAsync(ct);

    public async Task AgregarAsync(Notificacion notificacion, EstatusCambiado evento, CancellationToken ct)
    {
        db.Notificaciones.Add(new NotificacionFila
        {
            Id = notificacion.Id,
            EventoId = notificacion.EventoId,
            UsuarioId = notificacion.UsuarioId,
            Canal = notificacion.Canal,
            Plantilla = notificacion.Plantilla,
            EstadoEnvio = notificacion.Estado,
            Intentos = notificacion.Intentos,
            CreadaEn = notificacion.CreadaEn,
            ProximoIntentoEn = notificacion.ProximoIntentoEn,
            IdExterno = notificacion.IdExterno,
            UltimoError = notificacion.UltimoError,
            PostulacionId = evento.PostulacionId,
            VacanteId = evento.VacanteId,
            EmpresaId = evento.EmpresaId,
            ReferenciaFolio = evento.Folio,
            TituloVacante = evento.TituloVacante,
            NombreEmpresa = evento.NombreEmpresa,
            EstadoAnterior = evento.EstadoAnterior,
            EstadoNuevo = evento.EstadoNuevo,
            CausaCambio = evento.Causa,
            CambiadoEn = evento.CambiadoEn,
            Secuencia = evento.Secuencia
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task GuardarAsync(Notificacion notificacion, CancellationToken ct)
    {
        var fila = await db.Notificaciones.FirstOrDefaultAsync(x => x.Id == notificacion.Id, ct)
            ?? throw new InvalidOperationException("La notificacion no existe.");

        fila.EstadoEnvio = notificacion.Estado;
        fila.Intentos = notificacion.Intentos;
        fila.ProximoIntentoEn = notificacion.ProximoIntentoEn;
        fila.IdExterno = notificacion.IdExterno;
        fila.UltimoError = notificacion.UltimoError;

        if (notificacion.Estado == EstadoEnvio.Enviada && fila.EnviadaEn is null)
        {
            fila.EnviadaEn = reloj.GetUtcNow();
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<(Notificacion Notificacion, EstatusCambiado Evento)>> ListarPorEnviarAsync(
        DateTimeOffset ahora, CancellationToken ct)
    {
        var filas = await db.Notificaciones.AsNoTracking()
            .Where(x => x.EstadoEnvio == EstadoEnvio.Pendiente
                && x.ProximoIntentoEn != null
                && x.ProximoIntentoEn <= ahora
                && x.EventoId != null
                && x.PostulacionId != null
                && x.EstadoNuevo != null)
            .OrderBy(x => x.ProximoIntentoEn)
            .ToListAsync(ct);

        return [.. filas.Select(f => (
            Notificacion.Rehidratar(
                f.Id,
                f.EventoId!.Value,
                f.UsuarioId,
                f.Canal,
                f.Plantilla,
                f.CreadaEn,
                f.EstadoEnvio,
                f.Intentos,
                f.ProximoIntentoEn,
                f.IdExterno,
                f.UltimoError),
            new EstatusCambiado(
                f.EventoId!.Value,
                f.CreadaEn,
                f.PostulacionId!.Value,
                f.ReferenciaFolio ?? "",
                f.UsuarioId,
                f.VacanteId ?? Guid.Empty,
                f.EmpresaId ?? Guid.Empty,
                f.TituloVacante ?? "",
                f.NombreEmpresa ?? "",
                f.EstadoAnterior ?? Estados.Enviada,
                f.EstadoNuevo!,
                f.CausaCambio ?? CausasCambio.Avance,
                f.CambiadoEn ?? f.CreadaEn,
                f.Secuencia ?? 1)))];
    }
}

public sealed class MensajesProcesadosRepositorio(NotificacionesDbContext db, TimeProvider reloj) : IMensajesProcesados
{
    public Task<bool> ExisteAsync(string mensajeId, CancellationToken ct) =>
        db.MensajesProcesados.AnyAsync(x => x.MensajeId == mensajeId, ct);

    public async Task MarcarAsync(string mensajeId, CancellationToken ct)
    {
        if (await ExisteAsync(mensajeId, ct))
        {
            return;
        }

        db.MensajesProcesados.Add(new MensajeWhatsappProcesado
        {
            MensajeId = mensajeId,
            RecibidoEn = reloj.GetUtcNow(),
            Resultado = "Procesado"
        });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AlertasRepositorio(NotificacionesDbContext db) : IAlertas
{
    public async Task<Guid?> BuscarVacanteAsync(Guid usuarioId, string idAlerta, string boton, CancellationToken ct) =>
        await db.AlertasEnviadas.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.IdAlerta == idAlerta && x.Boton == boton)
            .Select(x => (Guid?)x.VacanteId)
            .FirstOrDefaultAsync(ct);
}

/// <summary>Cliente HTTP interno hacia NexTalent.Postulaciones protegido con X-Internal-Api-Key.</summary>
public sealed class ClientePostulacionesInterno(HttpClient http) : IPostulaciones
{
    private sealed record RespuestaInterna(string Tipo, string? Folio);

    public async Task<ResultadoPostulacion> PostularAsync(Guid candidatoId, Guid vacanteId, CancellationToken ct)
    {
        using var respuesta = await http.PostAsJsonAsync(
            "/interno/postulaciones",
            new { CandidatoId = candidatoId, VacanteId = vacanteId },
            ct);
        respuesta.EnsureSuccessStatusCode();

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaInterna>(cancellationToken: ct)
            ?? throw new HttpRequestException("Respuesta vacia de Postulaciones.");

        var tipo = Enum.TryParse<TipoResultado>(cuerpo.Tipo, ignoreCase: true, out var valor)
            ? valor
            : throw new HttpRequestException($"Resultado desconocido: {cuerpo.Tipo}.");

        return new ResultadoPostulacion(tipo, cuerpo.Folio);
    }

    public async Task MarcarRecordatorioEnviadoAsync(Guid postulacionId, CancellationToken ct)
    {
        using var respuesta = await http.PostAsync(
            $"/interno/postulaciones/{postulacionId}/recordatorio-enviado",
            content: null,
            ct);
        respuesta.EnsureSuccessStatusCode();
    }
}

/// <summary>
/// Despacho de correo en desarrollo/prototipo sin registrar datos sensibles ni cuerpo personal en logs.
/// </summary>
public sealed class SimuladorCorreo(ILogger<SimuladorCorreo> log) : ICorreo
{
    public Task<ResultadoEnvio> EnviarAsync(string email, string asunto, string texto, CancellationToken ct)
    {
        var id = $"mail.{Guid.NewGuid():N}";
        log.LogInformation("Correo simulado despachado con id {IdExterno} y asunto '{Asunto}'.", id, asunto);
        return Task.FromResult(ResultadoEnvio.Ok(id));
    }
}
