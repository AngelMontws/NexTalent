namespace NexTalent.Notificaciones.Domain;

public enum Canal
{
    Web,
    Whatsapp,
    Email
}

public enum EstadoEnvio
{
    Pendiente,
    Enviada,
    Fallida
}

/// <summary>Un aviso por un canal. No guarda el texto, para no duplicar datos personales.</summary>
public sealed class Notificacion(
    Guid id, Guid eventoId, Guid usuarioId, Canal canal, string plantilla, DateTimeOffset creadaEn)
{
    /// <summary>Espera antes de cada reintento: 3 reintentos despues del primer envio (HU-07.04).</summary>
    private static readonly TimeSpan[] Esperas =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)];

    public Guid Id { get; } = id;
    public Guid EventoId { get; } = eventoId;
    public Guid UsuarioId { get; } = usuarioId;
    public Canal Canal { get; } = canal;
    public string Plantilla { get; } = plantilla;
    public DateTimeOffset CreadaEn { get; } = creadaEn;
    public EstadoEnvio Estado { get; private set; } = EstadoEnvio.Pendiente;
    public int Intentos { get; private set; }
    public DateTimeOffset? ProximoIntentoEn { get; private set; } = creadaEn;
    public string? IdExterno { get; private set; }
    public string? UltimoError { get; private set; }

    public bool LeToca(DateTimeOffset ahora) => Estado == EstadoEnvio.Pendiente && ProximoIntentoEn <= ahora;

    public void RegistrarExito(string? idExterno)
    {
        ExigirPendiente();
        Intentos++;
        Estado = EstadoEnvio.Enviada;
        IdExterno = idExterno;
        ProximoIntentoEn = null;
    }

    /// <summary>Un error permanente (por ejemplo, numero inexistente) no se reintenta.</summary>
    public void RegistrarFallo(string codigoError, bool permanente, DateTimeOffset ahora)
    {
        ExigirPendiente();
        Intentos++;
        UltimoError = codigoError;
        if (permanente || Intentos > Esperas.Length)
        {
            Estado = EstadoEnvio.Fallida;
            ProximoIntentoEn = null;
            return;
        }

        ProximoIntentoEn = ahora + Esperas[Intentos - 1];
    }

    /// <summary>Por ejemplo, la persona se dio de baja antes del reintento.</summary>
    public void Cancelar(string motivo)
    {
        ExigirPendiente();
        Estado = EstadoEnvio.Fallida;
        UltimoError = motivo;
        ProximoIntentoEn = null;
    }

    private void ExigirPendiente()
    {
        if (Estado != EstadoEnvio.Pendiente)
        {
            throw new InvalidOperationException("La notificacion ya termino.");
        }
    }
}
