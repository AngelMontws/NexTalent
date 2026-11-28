using Microsoft.EntityFrameworkCore;
using NexTalent.Identidad.Application.Auth;
using NexTalent.Identidad.Infrastructure.Persistence;

namespace NexTalent.Identidad.Infrastructure.Auth;

public sealed class UsuariosAutenticacionRepositorio(IdentidadDbContext db) : IUsuariosAutenticacion
{
    public Task<UsuarioAutenticable?> BuscarPorEmailAsync(string email, CancellationToken ct) =>
        db.Usuarios.AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => new UsuarioAutenticable(
                u.Id,
                u.Email,
                u.PasswordHash,
                u.Rol,
                u.Reclutador != null ? u.Reclutador.EmpresaId : (Guid?)null))
            .FirstOrDefaultAsync(ct);
}