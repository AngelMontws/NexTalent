namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class CandidatoHabilidad
{
    public Guid CandidatoId { get; set; }

    /// <summary>Referencia logica a vacantes.habilidad.id: sin llave foranea entre esquemas.</summary>
    public Guid HabilidadId { get; set; }

    /// <summary>1 = Basico, 2 = Intermedio, 3 = Avanzado.</summary>
    public byte Nivel { get; set; }

    public Candidato Candidato { get; set; } = null!;
}