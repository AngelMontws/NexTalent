namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class Usuario
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? TelefonoWhatsapp { get; set; }
    public string Rol { get; set; } = null!;
    public bool WhatsappOptIn { get; set; }
    public DateTime? WhatsappOptInFecha { get; set; }
    public string AvisoPrivacidadVersion { get; set; } = null!;
    public DateTime AvisoPrivacidadAceptadoEn { get; set; }
    public bool EmailVerificado { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime CreadoEn { get; set; }

    public Candidato? Candidato { get; set; }
}