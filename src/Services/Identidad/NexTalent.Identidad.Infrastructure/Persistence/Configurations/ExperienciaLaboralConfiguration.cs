using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class ExperienciaLaboralConfiguration : IEntityTypeConfiguration<ExperienciaLaboral>
{
    public void Configure(EntityTypeBuilder<ExperienciaLaboral> b)
    {
        b.ToTable("experiencia_laboral", t =>
            t.HasCheckConstraint("CK_ExperienciaLaboral_Fechas",
                "[es_actual] = 1 OR ([fecha_fin] IS NOT NULL AND [fecha_fin] >= [fecha_inicio])"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.CandidatoId).HasColumnName("candidato_id");
        b.Property(x => x.Puesto).HasColumnName("puesto").HasColumnType("nvarchar(120)").IsRequired();
        b.Property(x => x.EmpresaNombre).HasColumnName("empresa_nombre").HasColumnType("nvarchar(160)").IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion").HasColumnType("nvarchar(1000)");
        b.Property(x => x.FechaInicio).HasColumnName("fecha_inicio").HasColumnType("date");
        b.Property(x => x.FechaFin).HasColumnName("fecha_fin").HasColumnType("date");
        b.Property(x => x.EsActual).HasColumnName("es_actual").HasDefaultValue(false);
        b.HasIndex(x => x.CandidatoId).HasDatabaseName("IX_ExperienciaLaboral_Candidato");
        b.HasOne(x => x.Candidato).WithMany(c => c.Experiencias).HasForeignKey(x => x.CandidatoId).OnDelete(DeleteBehavior.Cascade);
    }
}
