using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Identidad.Infrastructure.Persistence.Entities;

namespace NexTalent.Identidad.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_token");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("varchar(128)").IsRequired();
        b.Property(x => x.ExpiraEn).HasColumnName("expira_en").HasColumnType("datetime2(0)");
        b.Property(x => x.RevocadoEn).HasColumnName("revocado_en").HasColumnType("datetime2(0)");
        b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UQ_RefreshToken_Hash");
        b.HasIndex(x => new { x.UsuarioId, x.ExpiraEn }).HasDatabaseName("IX_RefreshToken_Usuario_Expira");
        b.HasOne(x => x.Usuario).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
