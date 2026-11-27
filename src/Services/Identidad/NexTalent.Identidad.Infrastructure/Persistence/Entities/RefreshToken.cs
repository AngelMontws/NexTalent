namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiraEn { get; set; }
    public DateTime? RevocadoEn { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
