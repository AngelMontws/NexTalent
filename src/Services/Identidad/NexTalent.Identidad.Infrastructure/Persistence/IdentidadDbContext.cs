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
    public DbSet<BitacoraModeracionEmpresa> BitacoraModeracionEmpresas => Set<BitacoraModeracionEmpresa>();
    public DbSet<ExperienciaLaboral> ExperienciaLaborales => Set<ExperienciaLaboral>();
    public DbSet<FormacionAcademica> FormacionAcademica => Set<FormacionAcademica>();
    public DbSet<Reclutador> Reclutadores => Set<Reclutador>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new EmpresaConfiguration());
        modelBuilder.ApplyConfiguration(new CandidatoConfiguration());
        modelBuilder.ApplyConfiguration(new CandidatoHabilidadConfiguration());
        modelBuilder.ApplyConfiguration(new BitacoraModeracionEmpresaConfiguration());
        modelBuilder.ApplyConfiguration(new ExperienciaLaboralConfiguration());
        modelBuilder.ApplyConfiguration(new FormacionAcademicaConfiguration());
        modelBuilder.ApplyConfiguration(new ReclutadorConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
    }
}
