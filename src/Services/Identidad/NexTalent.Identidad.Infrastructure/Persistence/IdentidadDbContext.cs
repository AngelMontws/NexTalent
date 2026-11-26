using Microsoft.EntityFrameworkCore;
using NexTalent.Identidad.Infrastructure.Persistence.Configurations;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence;

public sealed class IdentidadDbContext(DbContextOptions<IdentidadDbContext> options) : DbContext(options)
{
    public const string Esquema = "identidad";

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Candidato> Candidatos => Set<Candidato>();
    public DbSet<CandidatoHabilidad> CandidatoHabilidades => Set<CandidatoHabilidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new EmpresaConfiguration());
        modelBuilder.ApplyConfiguration(new CandidatoConfiguration());
        modelBuilder.ApplyConfiguration(new CandidatoHabilidadConfiguration());
    }
}