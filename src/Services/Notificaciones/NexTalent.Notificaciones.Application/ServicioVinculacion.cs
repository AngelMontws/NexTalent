using System.Security.Cryptography;
using System.Text;
using NexTalent.Notificaciones.Domain;

namespace NexTalent.Notificaciones.Application;

public sealed record Respuesta(bool Exito, string Mensaje);

/// <summary>
/// HU-07.01: numero, consentimiento, codigo y BAJA. La clave protege el codigo guardado y vive en .env.
/// </summary>
public sealed class ServicioVinculacion
{
    private const int BytesMinimosClave = 32;

    private readonly IVinculaciones _vinculaciones;
    private readonly IContactos _contactos;
    private readonly IWhatsapp _whatsapp;
    private readonly TimeProvider _reloj;
    private readonly byte[] _clave;

    public ServicioVinculacion(
        IVinculaciones vinculaciones, IContactos contactos, IWhatsapp whatsapp, TimeProvider reloj, byte[] clave)
    {
        ArgumentNullException.ThrowIfNull(clave);
        if (clave.Length < BytesMinimosClave)
        {
            throw new ArgumentException($"La clave debe tener al menos {BytesMinimosClave} bytes.", nameof(clave));
        }

        (_vinculaciones, _contactos, _whatsapp, _reloj, _clave) = (vinculaciones, contactos, whatsapp, reloj, clave);
    }

    /// <summary>Pedir el codigo exige haber aceptado antes: sin consentimiento no se envia nada.</summary>
    public async Task<Respuesta> SolicitarCodigoAsync(
        Guid usuarioId, string numero, bool aceptaAvisos, CancellationToken ct)
    {
        if (!aceptaAvisos)
        {
            return new(false, "Se requiere aceptar los avisos por WhatsApp.");
        }

        if (NormalizarTelefono(numero) is not { } telefono
            || (await _contactos.BuscarPorTelefonoAsync(telefono, ct) is { } dueno && dueno.UsuarioId != usuarioId))
        {
            return new(false, Textos.NumeroNoDisponible);
        }

        var ahora = _reloj.GetUtcNow();
        var vinculacion = await _vinculaciones.ObtenerAsync(usuarioId, ct);
        if (vinculacion.EstaBloqueada(ahora))
        {
            return new(false, Textos.LimiteIntentos);
        }

        if (!vinculacion.PuedeSolicitar(ahora))
        {
            return new(false, "Espera 60 segundos antes de reenviar el código.");
        }

        var esReenvio = vinculacion.EnviadoEn is not null;
        var codigo = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
        vinculacion.Solicitar(telefono, Proteger(usuarioId, codigo), ahora);
        await _vinculaciones.GuardarAsync(vinculacion, ct);

        var envio = await _whatsapp.EnviarCodigoAsync(telefono, codigo, ct);
        if (!envio.Exito)
        {
            return new(false, Textos.NumeroNoDisponible);
        }

        return new(true, esReenvio
            ? Textos.CodigoReenviado(telefono)
            : "Introduce el código de 6 dígitos que recibiste por WhatsApp");
    }

    public async Task<Respuesta> VerificarAsync(Guid usuarioId, string codigo, CancellationToken ct)
    {
        var ahora = _reloj.GetUtcNow();
        var vinculacion = await _vinculaciones.ObtenerAsync(usuarioId, ct);
        var resultado = vinculacion.Verificar(Proteger(usuarioId, codigo.Trim()), ahora);
        await _vinculaciones.GuardarAsync(vinculacion, ct);

        switch (resultado)
        {
            case ResultadoCodigo.Correcto:
                await _contactos.ActivarWhatsappAsync(usuarioId, vinculacion.Telefono!, ahora, ct);
                return new(true, Textos.NumeroVinculado);
            case ResultadoCodigo.Vencido:
                return new(false, Textos.CodigoVencido);
            case ResultadoCodigo.Bloqueado:
                return new(false, Textos.LimiteIntentos);
            default:
                return new(false, Textos.CodigoIncorrecto);
        }
    }

    /// <summary>Devuelve true si el texto era BAJA. Guarda la baja antes de responder.</summary>
    public async Task<bool> ProcesarTextoAsync(string telefono, string texto, CancellationToken ct)
    {
        if (!string.Equals(texto.Trim(), "BAJA", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (await _contactos.BuscarPorTelefonoAsync(telefono, ct) is { WhatsappOptIn: true } contacto)
        {
            await _contactos.DesactivarWhatsappAsync(contacto.UsuarioId, _reloj.GetUtcNow(), ct);
            await _whatsapp.EnviarTextoAsync(telefono, Textos.Baja, ct);
        }

        return true;
    }

    /// <summary>Mexico: 10 digitos, con o sin +52. Devuelve null si no es valido.</summary>
    public static string? NormalizarTelefono(string numero)
    {
        var limpio = new string([.. numero.Where(c => c is not (' ' or '-' or '(' or ')'))]);
        if (limpio.StartsWith("+52", StringComparison.Ordinal))
        {
            limpio = limpio[3..];
        }

        return limpio.Length == 10 && limpio.All(char.IsAsciiDigit) ? $"+52{limpio}" : null;
    }

    private byte[] Proteger(Guid usuarioId, string codigo) =>
        HMACSHA256.HashData(_clave, Encoding.UTF8.GetBytes($"{usuarioId:N}:{codigo}"));
}
