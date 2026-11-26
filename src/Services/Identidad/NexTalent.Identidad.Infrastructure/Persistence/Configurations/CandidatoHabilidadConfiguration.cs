using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class CandidatoHabilidadConfiguration : IEntityTypeConfiguration<CandidatoHabilidad>
{
    public void Configure(EntityTypeBuilder<CandidatoHabilidad> b)
    {
        b.ToTable("candidato_habilidad", t =>
            t.HasCheckConstraint("CK_CandidatoHabilidad_Nivel", "[nivel] IN (1,2,3)"));

        b.HasKey(x => new { x.CandidatoId, x.HabilidadId });
        b.Property(x => x.CandidatoId).HasColumnName("candidato_id");
        b.Property(x => x.HabilidadId).HasColumnName("habilidad_id");
        b.Property(x => x.Nivel).HasColumnName("nivel").HasColumnType("tinyint");

        b.HasIndex(x => new { x.HabilidadId, x.Nivel })
            .IncludeProperties(x => x.CandidatoId)
            .HasDatabaseName("IX_CandidatoHabilidad_Habilidad");

        b.HasOne(x => x.Candidato)
            .WithMany(c => c.Habilidades)
            .HasForeignKey(x => x.CandidatoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}