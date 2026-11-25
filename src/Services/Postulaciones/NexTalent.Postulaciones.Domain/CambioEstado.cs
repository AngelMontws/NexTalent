namespace NexTalent.Postulaciones.Domain;

/// <summary>
/// Registro inmutable del historial (HU-05.04). EstadoAnterior es null solo al crear;
/// CambiadoPor es null cuando el cambio lo hizo el sistema.
/// </summary>
public sealed record CambioEstado
{
    internal CambioEstado(
        int secuencia,
        EstadoPostulacion? estadoAnterior,
        EstadoPostulacion estadoNuevo,
        CausaCambio causa,
        Guid? cambiadoPor,
        string? motivo,
        DateTimeOffset fecha)
    {
        Secuencia = secuencia;
        EstadoAnterior = estadoAnterior;
        EstadoNuevo = estadoNuevo;
        Causa = causa;
        CambiadoPor = cambiadoPor;
        Motivo = motivo;
        Fecha = fecha;
    }

    public int Secuencia { get; }
    public EstadoPostulacion? EstadoAnterior { get; }
    public EstadoPostulacion EstadoNuevo { get; }
    public CausaCambio Causa { get; }
    public Guid? CambiadoPor { get; }
    public string? Motivo { get; }
    public DateTimeOffset Fecha { get; }
}
