using System.Security.Cryptography;

namespace NexTalent.Notificaciones.Domain;

public enum ResultadoCodigo
{
    Correcto,
    Incorrecto,
    Vencido,
    Bloqueado
}

/// <summary>
/// Codigo de 6 digitos de un usuario (HU-07.01). Guarda solo el hash. Los fallos se acumulan
/// aunque se reenvie el codigo, para que reenviar no regale intentos.
/// </summary>
public sealed class VinculacionWhatsapp(Guid usuarioId)
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan EsperaReenvio = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan Pausa = TimeSpan.FromMinutes(15);
    public const int MaximoIntentos = 5;

    private byte[]? _hash;

    public Guid UsuarioId { get; } = usuarioId;
    public string? Telefono { get; private set; }
    public int Intentos { get; private set; }
    public DateTimeOffset? EnviadoEn { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    public bool EstaBloqueada(DateTimeOffset ahora) => ahora < BloqueadoHasta;

    public bool PuedeSolicitar(DateTimeOffset ahora) =>
        !EstaBloqueada(ahora) && (EnviadoEn is null || ahora - EnviadoEn >= EsperaReenvio);

    /// <summary>Un codigo nuevo invalida el anterior.</summary>
    public void Solicitar(string telefono, byte[] hash, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(telefono);
        ArgumentNullException.ThrowIfNull(hash);
        if (!PuedeSolicitar(ahora))
        {
            throw new InvalidOperationException("Todavia no se puede solicitar otro codigo.");
        }

        LevantarPausa(ahora);
        Telefono = telefono;
        EnviadoEn = ahora;
        _hash = hash;
    }

    public ResultadoCodigo Verificar(byte[] hash, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (EstaBloqueada(ahora))
        {
            return ResultadoCodigo.Bloqueado;
        }

        LevantarPausa(ahora);
        if (_hash is null || ahora - EnviadoEn >= Vigencia)
        {
            return ResultadoCodigo.Vencido;
        }

        if (CryptographicOperations.FixedTimeEquals(_hash, hash))
        {
            _hash = null;
            Intentos = 0;
            return ResultadoCodigo.Correcto;
        }

        Intentos++;
        if (Intentos < MaximoIntentos)
        {
            return ResultadoCodigo.Incorrecto;
        }

        _hash = null;
        BloqueadoHasta = ahora + Pausa;
        return ResultadoCodigo.Bloqueado;
    }

    private void LevantarPausa(DateTimeOffset ahora)
    {
        if (BloqueadoHasta is not null && !EstaBloqueada(ahora))
        {
            BloqueadoHasta = null;
            Intentos = 0;
        }
    }

    internal byte[]? ObtenerHash() => _hash;

    internal static VinculacionWhatsapp Rehidratar(
        Guid usuarioId,
        string? telefono,
        byte[]? hash,
        int intentos,
        DateTimeOffset? enviadoEn,
        DateTimeOffset? bloqueadoHasta)
    {
        var v = new VinculacionWhatsapp(usuarioId)
        {
            Telefono = telefono,
            Intentos = intentos,
            EnviadoEn = enviadoEn,
            BloqueadoHasta = bloqueadoHasta
        };
        v._hash = hash;
        return v;
    }
}
