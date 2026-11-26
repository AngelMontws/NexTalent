using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class CandidatoConfiguration : IEntityTypeConfiguration<Candidato>
{
    public void Configure(EntityTypeBuilder<Candidato> b)
    {
        b.ToTable("candidato", t =>
        {
            t.HasCheckConstraint("CK_Candidato_Edad", "[edad] IS NULL OR [edad] BETWEEN 16 AND 99");
            t.HasCheckConstraint("CK_Candidato_CodigoPostal",
                "[codigo_postal] IS NULL OR [codigo_postal] LIKE '[0-9][0-9][0-9][0-9][0-9]'");
            t.HasCheckConstraint("CK_Candidato_SalarioEsperado", "[salario_esperado] IS NULL OR [salario_esperado] > 0");
            t.HasCheckConstraint("CK_Candidato_Modalidad",
                "[modalidad_preferida] IS NULL OR [modalidad_preferida] IN ('Presencial','Hibrido','Remoto')");
            t.HasCheckConstraint("CK_Candidato_Completitud", "[completitud_pct] BETWEEN 0 AND 100");
        });

        b.HasKey(x => x.UsuarioId);
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id").ValueGeneratedNever();
        b.Property(x => x.Nombres).HasColumnName("nombres").HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.ApellidoPaterno).HasColumnName("apellido_paterno").HasColumnType("nvarchar(80)").IsRequired();
        b.Property(x => x.ApellidoMaterno).HasColumnName("apellido_materno").HasColumnType("nvarchar(80)");
        b.Property(x => x.Edad).HasColumnName("edad").HasColumnType("tinyint");
        b.Property(x => x.Ocupacion).HasColumnName("ocupacion").HasColumnType("nvarchar(120)");
        b.Property(x => x.Curp).HasColumnName("curp").HasColumnType("char(18)");
        b.Property(x => x.Calle).HasColumnName("calle").HasColumnType("nvarchar(120)");
        b.Property(x => x.NumeroExterior).HasColumnName("numero_exterior").HasColumnType("nvarchar(20)");
        b.Property(x => x.Colonia).HasColumnName("colonia").HasColumnType("nvarchar(100)");
        b.Property(x => x.MunicipioAlcaldia).HasColumnName("municipio_alcaldia").HasColumnType("nvarchar(100)");
        b.Property(x => x.EstadoMx).HasColumnName("estado_mx").HasColumnType("nvarchar(60)");
        b.Property(x => x.CodigoPostal).HasColumnName("codigo_postal").HasColumnType("char(5)");
        b.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasColumnType("nvarchar(120)");
        b.Property(x => x.SalarioEsperado).HasColumnName("salario_esperado").HasColumnType("decimal(12,2)");
        b.Property(x => x.ModalidadPreferida).HasColumnName("modalidad_preferida").HasColumnType("varchar(20)");
        b.Property(x => x.FotoUrl).HasColumnName("foto_url").HasColumnType("nvarchar(500)");
        b.Property(x => x.CvPerfilUrl).HasColumnName("cv_perfil_url").HasColumnType("nvarchar(500)");
        b.Property(x => x.CompletitudPct).HasColumnName("completitud_pct").HasColumnType("tinyint")
            .HasDefaultValue((byte)0);

        b.HasIndex(x => x.Curp).IsUnique().HasDatabaseName("UQ_Candidato_Curp").HasFilter("[curp] IS NOT NULL");

        b.HasOne(x => x.Usuario)
            .WithOne(u => u.Candidato)
            .HasForeignKey<Candidato>(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}