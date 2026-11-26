namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class ExperienciaLaboral
{
    public Guid Id { get; set; }
    public Guid CandidatoId { get; set; }
    public string Puesto { get; set; } = null!;
    public string EmpresaNombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool EsActual { get; set; }
    public Candidato Candidato { get; set; } = null!;
}
