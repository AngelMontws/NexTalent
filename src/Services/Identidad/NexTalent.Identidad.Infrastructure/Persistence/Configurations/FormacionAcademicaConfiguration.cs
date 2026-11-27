using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class FormacionAcademicaConfiguration : IEntityTypeConfiguration<FormacionAcademica>
{
    public void Configure(EntityTypeBuilder<FormacionAcademica> b)
    {
        b.ToTable("formacion_academica", t =>
            t.HasCheckConstraint("CK_FormacionAcademica_Fechas",
                "[es_actual] = 1 OR ([fecha_fin] IS NOT NULL AND [fecha_fin] >= [fecha_inicio])"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.CandidatoId).HasColumnName("candidato_id");
        b.Property(x => x.Institucion).HasColumnName("institucion").HasColumnType("nvarchar(160)").IsRequired();
        b.Property(x => x.TituloGrado).HasColumnName("titulo_grado").HasColumnType("nvarchar(160)").IsRequired();
        b.Property(x => x.FechaInicio).HasColumnName("fecha_inicio").HasColumnType("date");
        b.Property(x => x.FechaFin).HasColumnName("fecha_fin").HasColumnType("date");
        b.Property(x => x.EsActual).HasColumnName("es_actual").HasDefaultValue(false);
        b.HasIndex(x => x.CandidatoId).HasDatabaseName("IX_FormacionAcademica_Candidato");
        b.HasOne(x => x.Candidato).WithMany(c => c.Formaciones).HasForeignKey(x => x.CandidatoId).OnDelete(DeleteBehavior.Cascade);
    }
}
