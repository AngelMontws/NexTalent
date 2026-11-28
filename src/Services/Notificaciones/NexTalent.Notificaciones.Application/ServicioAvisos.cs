using NexTalent.Contracts;
using NexTalent.Notificaciones.Domain;

namespace NexTalent.Notificaciones.Application;

/// <summary>
/// HU-07.04: avisa cada cambio de estatus. WhatsApp si hay opt-in; correo si no lo hay o si WhatsApp falla.
/// </summary>
public sealed class ServicioAvisos(
    INotificaciones notificaciones,
    IContactos contactos,
    IWhatsapp whatsapp,
    ICorreo correo,
    TimeProvider reloj,
    Func<Guid, string?> urlSimilares)
{
    /// <summary>Se puede repetir sin efecto. Ignora eventos mas viejos que el ultimo avisado.</summary>
    public async Task ProcesarAsync(EstatusCambiado evento, CancellationToken ct)
    {
        if (await notificaciones.ExisteEventoAsync(evento.EventoId, ct)
            || evento.Secuencia <= await notificaciones.UltimaSecuenciaAsync(evento.PostulacionId, ct))
        {
            return;
        }

        // Si el contacto aun no llega, el consumidor reintenta: no se pierde el aviso.
        var contacto = await contactos.ObtenerAsync(evento.CandidatoId, ct)
            ?? throw new ContactoNoDisponibleException();

        await CrearYEnviarAsync(evento, contacto, PuedeWhatsapp(contacto) ? Canal.Whatsapp : Canal.Email, ct);
    }

    /// <summary>Lo llamara una tarea periodica.</summary>
    public async Task ReintentarAsync(CancellationToken ct)
    {
        foreach (var (notificacion, evento) in await notificaciones.ListarPorEnviarAsync(reloj.GetUtcNow(), ct))
        {
            if (await contactos.ObtenerAsync(notificacion.UsuarioId, ct) is { } contacto)
            {
                await EnviarAsync(notificacion, evento, contacto, ct);
            }
        }
    }

    private async Task CrearYEnviarAsync(EstatusCambiado evento, Contacto contacto, Canal canal, CancellationToken ct)
    {
        var notificacion = new Notificacion(
            Guid.CreateVersion7(), evento.EventoId, contacto.UsuarioId, canal,
            $"P-02:{evento.EstadoNuevo}", reloj.GetUtcNow());

        // Se guarda antes de enviar: si el servicio cae, queda pendiente.
        await notificaciones.AgregarAsync(notificacion, evento, ct);
        await EnviarAsync(notificacion, evento, contacto, ct);
    }

    private async Task EnviarAsync(Notificacion n, EstatusCambiado evento, Contacto contacto, CancellationToken ct)
    {
        // El opt-in se revisa en cada intento: pudo llegar una BAJA.
        if (n.Canal == Canal.Whatsapp && !PuedeWhatsapp(contacto))
        {
            n.Cancelar("sin_opt_in");
        }
        else
        {
            var resultado = await IntentarAsync(n, evento, contacto, ct);
            if (resultado.Exito)
            {
                n.RegistrarExito(resultado.IdExterno);
            }
            else
            {
                n.RegistrarFallo(resultado.CodigoError ?? "desconocido", resultado.Permanente, reloj.GetUtcNow());
            }
        }

        if (n is { Canal: Canal.Whatsapp, Estado: EstadoEnvio.Fallida })
        {
            // El correo se crea antes de cerrar el de WhatsApp, para no perder el respaldo si el servicio cae.
            await CrearYEnviarAsync(evento, contacto, Canal.Email, ct);
        }

        await notificaciones.GuardarAsync(n, ct);
    }

    private async Task<ResultadoEnvio> IntentarAsync(
        Notificacion n, EstatusCambiado evento, Contacto contacto, CancellationToken ct)
    {
        var texto = Textos.Estatus(evento, urlSimilares(evento.VacanteId));
        try
        {
            // HU-06.03 y decision 5: el mensaje del reclutador va solo por correo; WhatsApp usa la plantilla fija.
            return n.Canal == Canal.Whatsapp
                ? await whatsapp.EnviarPlantillaAsync(contacto.TelefonoWhatsapp!, n.Plantilla, texto, ct)
                : await correo.EnviarAsync(contacto.Email, "NexTalent | Actualización de postulación",
                    Textos.ConMensajeDelReclutador(texto, evento.MensajeCandidato), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TimeoutException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            return ResultadoEnvio.Fallo("sin_conexion");
        }
    }

    private static bool PuedeWhatsapp(Contacto c) => c is { WhatsappOptIn: true, TelefonoWhatsapp: not null };
}

public sealed class ContactoNoDisponibleException()
    : Exception("El contacto aun no esta disponible. Se debe reintentar.");
