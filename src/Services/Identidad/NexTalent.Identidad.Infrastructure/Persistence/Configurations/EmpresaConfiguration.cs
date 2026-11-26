using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.ToTable("empresa", t =>
        {
            t.HasCheckConstraint("CK_Empresa_Rfc_Longitud", "LEN([rfc]) IN (12,13)");
            t.HasCheckConstraint("CK_Empresa_EstadoVerificacion",
                "[estado_verificacion] IN ('Pendiente','Aprobada','Rechazada')");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("nvarchar(160)").IsRequired();
        b.Property(x => x.Rfc).HasColumnName("rfc").HasColumnType("varchar(13)").IsRequired();
        b.Property(x => x.Verificada).HasColumnName("verificada").HasDefaultValue(false);
        b.Property(x => x.EstadoVerificacion).HasColumnName("estado_verificacion").HasColumnType("varchar(20)")
            .HasDefaultValue("Pendiente").IsRequired();
        b.Property(x => x.CreadaEn).HasColumnName("creada_en").HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => x.Rfc).IsUnique().HasDatabaseName("UQ_Empresa_Rfc");
        b.HasIndex(x => new { x.EstadoVerificacion, x.CreadaEn }).HasDatabaseName("IX_Empresa_ColaFifo");
    }
}