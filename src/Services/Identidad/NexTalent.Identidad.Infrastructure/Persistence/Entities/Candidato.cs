namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class Candidato
{
    public Guid UsuarioId { get; set; }
    public string Nombres { get; set; } = null!;
    public string ApellidoPaterno { get; set; } = null!;
    public string? ApellidoMaterno { get; set; }
    public byte? Edad { get; set; }
    public string? Ocupacion { get; set; }
    public string? Curp { get; set; }
    public string? Calle { get; set; }
    public string? NumeroExterior { get; set; }
    public string? Colonia { get; set; }
    public string? MunicipioAlcaldia { get; set; }
    public string? EstadoMx { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Ubicacion { get; set; }
    public decimal? SalarioEsperado { get; set; }
    public string? ModalidadPreferida { get; set; }
    public string? FotoUrl { get; set; }
    public string? CvPerfilUrl { get; set; }
    public byte CompletitudPct { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public List<CandidatoHabilidad> Habilidades { get; set; } = [];
    public List<ExperienciaLaboral> Experiencias { get; set; } = [];
    public List<FormacionAcademica> Formaciones { get; set; } = [];
}