using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTalent.Vacantes.Infrastructure.Persistence.Entities;

namespace NexTalent.Vacantes.Infrastructure.Persistence.Configurations;

public sealed class EmpresaResumenConfiguration : IEntityTypeConfiguration<EmpresaResumen>
{
    public void Configure(EntityTypeBuilder<EmpresaResumen> b)
    {
        b.ToTable("empresa_resumen", t =>
            t.HasCheckConstraint("CK_EmpresaResumen_IndiceRespuesta", "[indice_respuesta_pct] BETWEEN 0 AND 100"));

        b.HasKey(x => x.EmpresaId);
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").ValueGeneratedNever();
        b.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("nvarchar(160)").IsRequired();
        b.Property(x => x.Verificada).HasColumnName("verificada").HasDefaultValue(false);
        b.Property(x => x.IndiceRespuestaPct).HasColumnName("indice_respuesta_pct").HasColumnType("decimal(5,2)")
            .HasDefaultValue(0m);
        b.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en").HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public sealed class VacanteConfiguration : IEntityTypeConfiguration<Vacante>
{
    public void Configure(EntityTypeBuilder<Vacante> b)
    {
        b.ToTable("vacante", t =>
        {
            // RN-02: el rango salarial es obligatorio y coherente, validado por el motor SQL.
            t.HasCheckConstraint("CK_Vacante_SalarioMin_Positivo", "[salario_min] > 0");
            t.HasCheckConstraint("CK_Vacante_SalarioMax_Valido", "[salario_max] >= [salario_min]");
            t.HasCheckConstraint("CK_Vacante_TipoSalario", "[tipo_salario] IN ('Bruto','Neto')");
            t.HasCheckConstraint("CK_Vacante_Modalidad", "[modalidad] IN ('Presencial','Hibrido','Remoto')");
            t.HasCheckConstraint("CK_Vacante_Estado",
                "[estado] IN ('Borrador','Publicada','OcultaPorReportes','Cerrada','Caducada')");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id");
        b.Property(x => x.ReclutadorId).HasColumnName("reclutador_id");
        b.Property(x => x.Titulo).HasColumnName("titulo").HasColumnType("nvarchar(150)").IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion").HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Actividades).HasColumnName("actividades").HasColumnType("nvarchar(2000)");
        b.Property(x => x.Beneficios).HasColumnName("beneficios").HasColumnType("nvarchar(2000)");
        b.Property(x => x.SalarioMin).HasColumnName("salario_min").HasColumnType("decimal(12,2)");
        b.Property(x => x.SalarioMax).HasColumnName("salario_max").HasColumnType("decimal(12,2)");
        b.Property(x => x.TipoSalario).HasColumnName("tipo_salario").HasColumnType("varchar(10)").IsRequired();
        b.Property(x => x.Modalidad).HasColumnName("modalidad").HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.Jornada).HasColumnName("jornada").HasColumnType("varchar(30)").IsRequired();
        b.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasColumnType("nvarchar(120)").IsRequired();
        b.Property(x => x.Estado).HasColumnName("estado").HasColumnType("varchar(25)").HasDefaultValue("Borrador")
            .IsRequired();
        b.Property(x => x.PublicadaEn).HasColumnName("publicada_en").HasColumnType("datetime2(0)");
        b.Property(x => x.ExpiraEn).HasColumnName("expira_en").HasColumnType("datetime2(0)");
        b.Property(x => x.Recordatorio27dEnviado).HasColumnName("recordatorio_27d_enviado").HasDefaultValue(false);
        b.Property(x => x.VersionEvento).HasColumnName("version_evento").HasDefaultValue(0L);
        b.Property(x => x.CreadaEn).HasColumnName("creada_en").HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Covering index de la busqueda: resuelve las tarjetas sin Key Lookup (RNF-01 < 200 ms).
        b.HasIndex(x => new { x.Estado, x.SalarioMin, x.SalarioMax })
            .IncludeProperties(x => new
            {
                x.Titulo, x.EmpresaId, x.Modalidad, x.Jornada, x.Ubicacion, x.TipoSalario, x.PublicadaEn, x.ExpiraEn
            })
            .HasDatabaseName("IX_Vacante_Busqueda");
        b.HasIndex(x => new { x.EmpresaId, x.Estado }).HasDatabaseName("IX_Vacante_Empresa_Estado");
        // Tarea diaria de caducidad (30 d) y aviso del dia 27.
        b.HasIndex(x => new { x.Estado, x.ExpiraEn }).HasDatabaseName("IX_Vacante_Estado_Expira");

        b.HasOne(x => x.Empresa).WithMany(e => e.Vacantes).HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HabilidadConfiguration : IEntityTypeConfiguration<Habilidad>
{
    public void Configure(EntityTypeBuilder<Habilidad> b)
    {
        b.ToTable("habilidad", t =>
            t.HasCheckConstraint("CK_Habilidad_EstadoAprobacion",
                "[estado_aprobacion] IN ('Aprobada','Pendiente','Rechazada')"));

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("nvarchar(80)").IsRequired();
        b.Property(x => x.Categoria).HasColumnName("categoria").HasColumnType("nvarchar(60)").IsRequired();
        b.Property(x => x.EstadoAprobacion).HasColumnName("estado_aprobacion").HasColumnType("varchar(20)")
            .HasDefaultValue("Pendiente").IsRequired();
        b.Property(x => x.PropuestaPor).HasColumnName("propuesta_por");

        b.HasIndex(x => x.Nombre).IsUnique().HasDatabaseName("UQ_Habilidad_Nombre");
    }
}

public sealed class VacanteHabilidadConfiguration : IEntityTypeConfiguration<VacanteHabilidad>
{
    public void Configure(EntityTypeBuilder<VacanteHabilidad> b)
    {
        b.ToTable("vacante_habilidad", t =>
            t.HasCheckConstraint("CK_VacanteHabilidad_Nivel", "[nivel] IN (1,2,3)"));

        b.HasKey(x => new { x.VacanteId, x.HabilidadId });
        b.Property(x => x.VacanteId).HasColumnName("vacante_id");
        b.Property(x => x.HabilidadId).HasColumnName("habilidad_id");
        b.Property(x => x.Nivel).HasColumnName("nivel").HasColumnType("tinyint");
        b.Property(x => x.EsRequerida).HasColumnName("es_requerida");

        // Filtros facetados y calculo RN-03.
        b.HasIndex(x => new { x.HabilidadId, x.Nivel })
            .IncludeProperties(x => x.VacanteId)
            .HasDatabaseName("IX_VacanteHabilidad_Habilidad");

        b.HasOne(x => x.Vacante).WithMany(v => v.Habilidades).HasForeignKey(x => x.VacanteId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Habilidad).WithMany().HasForeignKey(x => x.HabilidadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReporteVacanteConfiguration : IEntityTypeConfiguration<ReporteVacante>
{
    public void Configure(EntityTypeBuilder<ReporteVacante> b)
    {
        b.ToTable("reporte_vacante", t =>
            t.HasCheckConstraint("CK_ReporteVacante_Motivo",
                "[motivo_tipo] IN ('PideDinero','VacanteFalsa','Discriminatoria','Otro')"));

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.VacanteId).HasColumnName("vacante_id");
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        b.Property(x => x.MotivoTipo).HasColumnName("motivo_tipo").HasColumnType("varchar(30)").IsRequired();
        b.Property(x => x.Detalle).HasColumnName("detalle").HasColumnType("nvarchar(500)");
        b.Property(x => x.Fecha).HasColumnName("fecha").HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Un reporte por persona y vacante (HU-08.02); tres reportes distintos ocultan la vacante.
        b.HasIndex(x => new { x.VacanteId, x.UsuarioId }).IsUnique().HasDatabaseName("UQ_ReporteVacante_Vacante_Usuario");

        b.HasOne(x => x.Vacante).WithMany(v => v.Reportes).HasForeignKey(x => x.VacanteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BitacoraModeracionConfiguration : IEntityTypeConfiguration<BitacoraModeracion>
{
    public void Configure(EntityTypeBuilder<BitacoraModeracion> b)
    {
        b.ToTable("bitacora_moderacion", t =>
        {
            t.HasCheckConstraint("CK_BitacoraModeracion_Entidad", "[entidad_tipo] IN ('Vacante','Habilidad')");
            t.HasCheckConstraint("CK_BitacoraModeracion_Motivo", "LEN(LTRIM(RTRIM([motivo]))) >= 5");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.EntidadTipo).HasColumnName("entidad_tipo").HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.EntidadId).HasColumnName("entidad_id");
        b.Property(x => x.AdministradorId).HasColumnName("administrador_id");
        b.Property(x => x.Accion).HasColumnName("accion").HasColumnType("varchar(30)").IsRequired();
        b.Property(x => x.Motivo).HasColumnName("motivo").HasColumnType("nvarchar(500)").IsRequired();
        b.Property(x => x.Fecha).HasColumnName("fecha").HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => new { x.EntidadTipo, x.EntidadId, x.Fecha }).HasDatabaseName("IX_BitacoraModeracion_Entidad");
    }
}
