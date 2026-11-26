namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class Empresa
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Rfc { get; set; } = null!;
    public bool Verificada { get; set; }
    public string EstadoVerificacion { get; set; } = "Pendiente";
    public DateTime CreadaEn { get; set; }
}