namespace NexTalent.Notificaciones.Application;

public enum TipoResultado
{
    Creada,
    YaPostulado,
    PerfilIncompleto,
    VacanteCerrada
}

public sealed record ResultadoPostulacion(TipoResultado Tipo, string? Folio);

/// <summary>Llamara a POST /interno/postulaciones. Si no hay respuesta, lanza una excepcion.</summary>
public interface IPostulaciones
{
    Task<ResultadoPostulacion> PostularAsync(Guid candidatoId, Guid vacanteId, CancellationToken ct);
}

/// <summary>Que vacante tiene cada boton de una alerta enviada.</summary>
public interface IAlertas
{
    Task<Guid?> BuscarVacanteAsync(Guid usuarioId, string idAlerta, string boton, CancellationToken ct);
}

public interface IMensajesProcesados
{
    Task<bool> ExisteAsync(string mensajeId, CancellationToken ct);
    Task MarcarAsync(string mensajeId, CancellationToken ct);
}

/// <summary>
/// HU-07.03 y BAJA. Si algo falla lanza una excepcion y el mensaje no se marca: Meta lo reintenta,
/// y repetir la postulacion no la duplica.
/// </summary>
public sealed class ServicioWebhook(
    ServicioVinculacion vinculacion,
    IContactos contactos,
    IAlertas alertas,
    IPostulaciones postulaciones,
    IMensajesProcesados procesados,
    IWhatsapp whatsapp,
    string? urlMiCuenta)
{
    public async Task ProcesarAsync(IReadOnlyList<MensajeEntrante> mensajes, CancellationToken ct)
    {
        foreach (var mensaje in mensajes)
        {
            if (await procesados.ExisteAsync(mensaje.Id, ct))
            {
                continue;
            }

            await AtenderAsync(mensaje, ct);
            await procesados.MarcarAsync(mensaje.Id, ct);
        }
    }

    private async Task AtenderAsync(MensajeEntrante m, CancellationToken ct)
    {
        // Sin opt-in no se envia nada por WhatsApp (HU-07.01).
        if (await BuscarContactoAsync(m, ct) is not { WhatsappOptIn: true, TelefonoWhatsapp: { } telefono } contacto)
        {
            return;
        }

        if (m.Texto is not null)
        {
            await vinculacion.ProcesarTextoAsync(telefono, m.Texto, ct);
            return;
        }

        if (m.Boton is null || m.IdAlerta is null
            || await alertas.BuscarVacanteAsync(contacto.UsuarioId, m.IdAlerta, m.Boton, ct) is not { } vacanteId)
        {
            return;
        }

        var resultado = await postulaciones.PostularAsync(contacto.UsuarioId, vacanteId, ct);
        var envio = await whatsapp.EnviarTextoAsync(telefono, Textos.Postulacion(resultado, urlMiCuenta), ct);
        if (!envio.Exito && !envio.Permanente)
        {
            throw new EnvioFallidoException();
        }
    }

    private async Task<Contacto?> BuscarContactoAsync(MensajeEntrante m, CancellationToken ct)
    {
        if (m.UsuarioWhatsapp is not null
            && await contactos.BuscarPorUsuarioWhatsappAsync(m.UsuarioWhatsapp, ct) is { } porUsuario)
        {
            return porUsuario;
        }

        return m.Telefono is not null && NormalizarDeMeta(m.Telefono) is { } telefono
            ? await contactos.BuscarPorTelefonoAsync(telefono, ct)
            : null;
    }

    /// <summary>Meta envia sin "+", y en Mexico puede agregar un 1: 521 + 10 digitos.</summary>
    internal static string? NormalizarDeMeta(string telefono)
    {
        var digitos = telefono.TrimStart('+');
        if (digitos.Length == 13 && digitos.StartsWith("521", StringComparison.Ordinal))
        {
            digitos = "52" + digitos[3..];
        }

        return ServicioVinculacion.NormalizarTelefono("+" + digitos);
    }
}

public sealed class EnvioFallidoException() : Exception("No se pudo enviar la confirmacion por WhatsApp.");
