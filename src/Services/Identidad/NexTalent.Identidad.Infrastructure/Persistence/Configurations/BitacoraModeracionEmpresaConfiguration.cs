using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class BitacoraModeracionEmpresaConfiguration : IEntityTypeConfiguration<BitacoraModeracionEmpresa>
{
    public void Configure(EntityTypeBuilder<BitacoraModeracionEmpresa> b)
    {
        b.ToTable("bitacora_moderacion_empresa", t =>
        {
            t.HasCheckConstraint("CK_BitacoraEmpresa_Decision", "[decision] IN ('Aprobada','Rechazada')");
            t.HasCheckConstraint("CK_BitacoraEmpresa_Motivo", "LEN(LTRIM(RTRIM([motivo]))) >= 5");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id");
        b.Property(x => x.AdministradorId).HasColumnName("administrador_id");
        b.Property(x => x.Decision).HasColumnName("decision").HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.Motivo).HasColumnName("motivo").HasColumnType("nvarchar(500)").IsRequired();
        b.Property(x => x.Fecha).HasColumnName("fecha").HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        b.HasIndex(x => new { x.EmpresaId, x.Fecha }).HasDatabaseName("IX_BitacoraEmpresa_Empresa_Fecha");
        b.HasOne(x => x.Empresa).WithMany(e => e.Bitacora).HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Administrador).WithMany().HasForeignKey(x => x.AdministradorId).OnDelete(DeleteBehavior.Restrict);
    }
}
