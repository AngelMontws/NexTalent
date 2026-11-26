namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class FormacionAcademica
{
    public Guid Id { get; set; }
    public Guid CandidatoId { get; set; }
    public string Institucion { get; set; } = null!;
    public string TituloGrado { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool EsActual { get; set; }
    public Candidato Candidato { get; set; } = null!;
}
