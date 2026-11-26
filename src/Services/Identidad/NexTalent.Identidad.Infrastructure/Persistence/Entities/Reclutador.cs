// Reclutador.cs
namespace NexTalent.Identidad.Infrastructure.Persistence.Entities;

public sealed class Reclutador
{
    public Guid UsuarioId { get; set; }
    public Guid EmpresaId { get; set; }
    public string NombreContacto { get; set; } = null!;
    public bool AprobadoEmpresa { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Empresa Empresa { get; set; } = null!;
}
