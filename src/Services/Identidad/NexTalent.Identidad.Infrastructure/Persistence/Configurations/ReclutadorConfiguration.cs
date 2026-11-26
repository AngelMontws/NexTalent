using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class ReclutadorConfiguration : IEntityTypeConfiguration<Reclutador>
{
    public void Configure(EntityTypeBuilder<Reclutador> b)
    {
        b.ToTable("reclutador");
        b.HasKey(x => x.UsuarioId);
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id").ValueGeneratedNever();
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id");
        b.Property(x => x.NombreContacto).HasColumnName("nombre_contacto").HasColumnType("nvarchar(120)").IsRequired();
        b.Property(x => x.AprobadoEmpresa).HasColumnName("aprobado_empresa").HasDefaultValue(false);
        b.HasIndex(x => x.EmpresaId).HasDatabaseName("IX_Reclutador_Empresa");
        b.HasOne(x => x.Usuario).WithOne(u => u.Reclutador).HasForeignKey<Reclutador>(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Empresa).WithMany(e => e.Reclutadores).HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
