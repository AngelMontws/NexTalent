using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuario", t =>
        {
            t.HasCheckConstraint("CK_Usuario_Rol", "[rol] IN ('Candidato','Reclutador','Administrador')");
            t.HasCheckConstraint("CK_Usuario_OptIn_Fecha", "[whatsapp_opt_in] = 0 OR [whatsapp_opt_in_fecha] IS NOT NULL");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.Email).HasColumnName("email").HasColumnType("nvarchar(256)").IsRequired();
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("nvarchar(512)").IsRequired();
        b.Property(x => x.TelefonoWhatsapp).HasColumnName("telefono_whatsapp").HasColumnType("varchar(16)");
        b.Property(x => x.Rol).HasColumnName("rol").HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.WhatsappOptIn).HasColumnName("whatsapp_opt_in").HasDefaultValue(false);
        b.Property(x => x.WhatsappOptInFecha).HasColumnName("whatsapp_opt_in_fecha").HasColumnType("datetime2(0)");
        b.Property(x => x.AvisoPrivacidadVersion).HasColumnName("aviso_privacidad_version").HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.AvisoPrivacidadAceptadoEn).HasColumnName("aviso_privacidad_aceptado_en").HasColumnType("datetime2(0)");
        b.Property(x => x.EmailVerificado).HasColumnName("email_verificado").HasDefaultValue(false);
        b.Property(x => x.IntentosFallidos).HasColumnName("intentos_fallidos").HasDefaultValue(0);
        b.Property(x => x.BloqueadoHasta).HasColumnName("bloqueado_hasta").HasColumnType("datetime2(0)");
        b.Property(x => x.CreadoEn).HasColumnName("creado_en").HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Usuario_Email");
        b.HasIndex(x => x.TelefonoWhatsapp).IsUnique().HasDatabaseName("UQ_Usuario_Telefono")
            .HasFilter("[telefono_whatsapp] IS NOT NULL");
    }
}