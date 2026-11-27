namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class BitacoraModeracionEmpresa
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AdministradorId { get; set; }
    public string Decision { get; set; } = null!;
    public string Motivo { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Usuario Administrador { get; set; } = null!;
}
