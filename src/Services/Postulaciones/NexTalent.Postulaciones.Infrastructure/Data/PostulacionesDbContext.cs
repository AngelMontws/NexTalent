using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Infrastructure.Data;

/// <summary>
/// Esquema postulaciones. Sigue el Paquete Documental de Base de Datos v1.0 (02_Diccionario_de_Datos) y lo
/// aprobado por Base de Datos (docs/backend2/modelo-datos.md). Fechas en UTC como datetime2(0).
/// Sin llaves foraneas hacia otros esquemas. El trigger del historial, los permisos y la vista del indice
/// de respuesta los agrega Base de Datos con su script, despues de estas migraciones.
/// </summary>
public sealed class PostulacionesDbContext(DbContextOptions<PostulacionesDbContext> opciones) : DbContext(opciones)
{
    public const string Esquema = "postulaciones";
    public const string SecuenciaFolio = "folio_seq";

    public DbSet<Postulacion> Postulaciones => Set<Postulacion>();
    public DbSet<VacanteReplica> Vacantes => Set<VacanteReplica>();
    public DbSet<VacanteHabilidadReplica> VacanteHabilidades => Set<VacanteHabilidadReplica>();
    public DbSet<CandidatoReplica> Candidatos => Set<CandidatoReplica>();
    public DbSet<CandidatoHabilidadReplica> CandidatoHabilidades => Set<CandidatoHabilidadReplica>();

    protected override void ConfigureConventions(ModelConfigurationBuilder convenciones)
    {
        convenciones.Properties<DateTimeOffset>().HaveConversion<FechaUtc>().HaveColumnType("datetime2(0)");
        convenciones.Properties<DateTimeOffset?>().HaveConversion<FechaUtc>().HaveColumnType("datetime2(0)");
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.HasDefaultSchema(Esquema);
        modelo.HasSequence<long>(SecuenciaFolio, Esquema).StartsAt(1);

        modelo.Entity<Postulacion>(e =>
        {
            e.ToTable("postulacion", t =>
            {
                t.HasCheckConstraint("ck_postulacion_estado_actual", Valores("estado_actual", typeof(EstadoPostulacion)));
                t.HasCheckConstraint("ck_postulacion_coincidencia_pct", "coincidencia_pct BETWEEN 0 AND 100");
                t.HasCheckConstraint("ck_postulacion_deseables_cumplidas", "deseables_cumplidas >= 0");
                t.HasCheckConstraint("ck_postulacion_canal_origen", Valores("canal_origen", typeof(CanalOrigen)));
            });
            e.HasKey(p => p.Id).HasName("pk_postulacion");
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(p => p.Folio).HasColumnName("folio").HasColumnType("varchar(20)").IsRequired();
            e.Property(p => p.CandidatoId).HasColumnName("candidato_id");
            e.Property(p => p.VacanteId).HasColumnName("vacante_id");
            e.Property(p => p.EstadoActual).HasColumnName("estado_actual")
                .HasConversion<string>().HasColumnType("varchar(25)");
            e.Property(p => p.CoincidenciaPct).HasColumnName("coincidencia_pct").HasColumnType("decimal(5,2)");
            e.Property(p => p.DeseablesCumplidas).HasColumnName("deseables_cumplidas");
            e.Property(p => p.CreadaEn).HasColumnName("creada_en");
            e.Property(p => p.CanalOrigen).HasColumnName("canal_origen")
                .HasConversion<string>().HasColumnType("varchar(15)");
            e.Property(p => p.EtapaActualizadaEn).HasColumnName("etapa_actualizada_en");
            e.Property(p => p.RecordatorioEnviado).HasColumnName("recordatorio_10d_enviado");
            e.Property<byte[]>("Version").HasColumnName("version").IsRowVersion();

            e.HasIndex(p => p.Folio).IsUnique().HasDatabaseName("uq_postulacion_folio");
            e.HasIndex(p => new { p.CandidatoId, p.VacanteId }).IsUnique()
                .HasDatabaseName("uq_postulacion_candidato_vacante");
            e.HasIndex(p => new { p.VacanteId, p.CoincidenciaPct, p.DeseablesCumplidas })
                .IsDescending(false, true, true).HasDatabaseName("ix_postulacion_vacante_coincidencia");

            e.HasOne<VacanteReplica>().WithMany().HasForeignKey(p => p.VacanteId)
                .HasConstraintName("fk_postulacion_vacante_replica").OnDelete(DeleteBehavior.NoAction);

            e.HasMany(p => p.Historial).WithOne()
                .HasForeignKey("PostulacionId").HasConstraintName("fk_historial_estado_postulacion")
                .OnDelete(DeleteBehavior.NoAction);
            e.Navigation(p => p.Historial).HasField("_historial").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelo.Entity<CambioEstado>(e =>
        {
            e.ToTable("historial_estado", t =>
            {
                t.HasCheckConstraint("ck_historial_estado_nuevo", Valores("estado_nuevo", typeof(EstadoPostulacion)));
                t.HasCheckConstraint("ck_historial_estado_causa", Valores("causa", typeof(CausaCambio)));
                t.HasCheckConstraint("ck_historial_estado_motivo_reactivacion",
                    "estado_anterior IS NULL OR estado_anterior <> 'NoSeleccionado' OR LEN(LTRIM(motivo)) > 0");
            });
            e.Property<long>("Id").HasColumnName("id").UseIdentityColumn();
            e.HasKey("Id").HasName("pk_historial_estado");
            e.Property<Guid>("PostulacionId").HasColumnName("postulacion_id");
            e.Property(c => c.Secuencia).HasColumnName("secuencia");
            e.Property(c => c.EstadoAnterior).HasColumnName("estado_anterior")
                .HasConversion<string>().HasColumnType("varchar(25)");
            e.Property(c => c.EstadoNuevo).HasColumnName("estado_nuevo")
                .HasConversion<string>().HasColumnType("varchar(25)");
            e.Property(c => c.Causa).HasColumnName("causa").HasConversion<string>().HasColumnType("varchar(25)");
            e.Property(c => c.CambiadoPor).HasColumnName("cambiado_por");
            e.Property(c => c.Motivo).HasColumnName("motivo").HasMaxLength(Postulacion.MotivoMaximo);
            e.Property(c => c.Fecha).HasColumnName("fecha");
            e.HasIndex("PostulacionId", nameof(CambioEstado.Secuencia)).IsUnique()
                .HasDatabaseName("uq_historial_estado_postulacion_secuencia");
        });

        modelo.Entity<VacanteReplica>(e =>
        {
            e.ToTable("vacante_replica", t =>
                t.HasCheckConstraint("ck_vacante_replica_estado", "estado IN ('Activa', 'Cerrada')"));
            e.HasKey(v => v.VacanteId).HasName("pk_vacante_replica");
            e.Property(v => v.VacanteId).HasColumnName("vacante_id").ValueGeneratedNever();
            e.Property(v => v.EmpresaId).HasColumnName("empresa_id");
            e.Property(v => v.Titulo).HasColumnName("titulo").HasMaxLength(200).IsRequired();
            e.Property(v => v.NombreEmpresa).HasColumnName("nombre_empresa").HasMaxLength(200);
            e.Property(v => v.SalarioMin).HasColumnName("salario_min").HasColumnType("decimal(12,2)");
            e.Property(v => v.SalarioMax).HasColumnName("salario_max").HasColumnType("decimal(12,2)");
            e.Property(v => v.TipoSalario).HasColumnName("tipo_salario").HasColumnType("varchar(10)");
            e.Property(v => v.ExpiraEn).HasColumnName("expira_en");
            e.Property(v => v.Abierta).HasColumnName("estado").HasColumnType("varchar(25)")
                .HasConversion(abierta => abierta ? "Activa" : "Cerrada", estado => estado == "Activa");
            e.Property(v => v.ActualizadaEn).HasColumnName("actualizada_en");
            e.Property(v => v.VersionEvento).HasColumnName("version_evento");
            e.HasIndex(v => v.EmpresaId).HasDatabaseName("ix_vacante_replica_empresa");
        });

        modelo.Entity<VacanteHabilidadReplica>(e =>
        {
            e.ToTable("vacante_habilidad_replica", t =>
                t.HasCheckConstraint("ck_vacante_habilidad_replica_nivel", Valores("nivel", typeof(NivelHabilidad))));
            e.HasKey(h => new { h.VacanteId, h.HabilidadId }).HasName("pk_vacante_habilidad_replica");
            e.Property(h => h.VacanteId).HasColumnName("vacante_id");
            e.Property(h => h.HabilidadId).HasColumnName("habilidad_id");
            e.Property(h => h.Nivel).HasColumnName("nivel").HasConversion<string>().HasColumnType("varchar(12)");
            e.Property(h => h.Requerida).HasColumnName("requerida");
            e.HasOne<VacanteReplica>().WithMany().HasForeignKey(h => h.VacanteId)
                .HasConstraintName("fk_vacante_habilidad_replica_vacante").OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<CandidatoReplica>(e =>
        {
            e.ToTable("candidato_replica");
            e.HasKey(c => c.CandidatoId).HasName("pk_candidato_replica");
            e.Property(c => c.CandidatoId).HasColumnName("candidato_id").ValueGeneratedNever();
            e.Property(c => c.TieneNombre).HasColumnName("tiene_nombre");
            e.Property(c => c.TieneContacto).HasColumnName("tiene_contacto");
            e.Property(c => c.VersionEvento).HasColumnName("version_evento");
        });

        modelo.Entity<CandidatoHabilidadReplica>(e =>
        {
            e.ToTable("candidato_habilidad_replica", t =>
                t.HasCheckConstraint("ck_candidato_habilidad_replica_nivel", Valores("nivel", typeof(NivelHabilidad))));
            e.HasKey(h => new { h.CandidatoId, h.HabilidadId }).HasName("pk_candidato_habilidad_replica");
            e.Property(h => h.CandidatoId).HasColumnName("candidato_id");
            e.Property(h => h.HabilidadId).HasColumnName("habilidad_id");
            e.Property(h => h.Nivel).HasColumnName("nivel").HasConversion<string>().HasColumnType("varchar(12)");
            e.HasOne<CandidatoReplica>().WithMany().HasForeignKey(h => h.CandidatoId)
                .HasConstraintName("fk_candidato_habilidad_replica_candidato").OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>CHECK con los nombres del enum, que son los mismos textos de los contratos.</summary>
    private static string Valores(string columna, Type tipo) =>
        $"{columna} IN ({string.Join(", ", Enum.GetNames(tipo).Select(n => $"'{n}'"))})";
}

/// <summary>Guarda en UTC sin zona horaria y la recupera como UTC.</summary>
internal sealed class FechaUtc() : ValueConverter<DateTimeOffset, DateTime>(
    fecha => fecha.UtcDateTime,
    fecha => new DateTimeOffset(DateTime.SpecifyKind(fecha, DateTimeKind.Utc)));

/// <summary>Copia local alimentada por VacantePublicada y VacanteCerrada.</summary>
public sealed class VacanteReplica
{
    public Guid VacanteId { get; set; }

    /// <summary>NOT NULL: la usa la vista del indice de respuesta. Tambien llega en VacanteCerrada.</summary>
    public Guid EmpresaId { get; set; }

    public string Titulo { get; set; } = "";

    /// <summary>Datos que solo trae VacantePublicada: nulos si el cierre llego primero.</summary>
    public string? NombreEmpresa { get; set; }
    public decimal? SalarioMin { get; set; }
    public decimal? SalarioMax { get; set; }
    public string? TipoSalario { get; set; }
    public DateTimeOffset? ExpiraEn { get; set; }
    /// <summary>Columna estado: Activa o Cerrada.</summary>
    public bool Abierta { get; set; }

    public DateTimeOffset ActualizadaEn { get; set; }
    public long VersionEvento { get; set; }
}

public sealed class VacanteHabilidadReplica
{
    public Guid VacanteId { get; set; }
    public Guid HabilidadId { get; set; }
    public NivelHabilidad Nivel { get; set; }
    public bool Requerida { get; set; }
}

/// <summary>Copia local alimentada por PerfilHabilidadesActualizado. Sin datos personales.</summary>
public sealed class CandidatoReplica
{
    public Guid CandidatoId { get; set; }
    public bool TieneNombre { get; set; }
    public bool TieneContacto { get; set; }
    public long VersionEvento { get; set; }
}

public sealed class CandidatoHabilidadReplica
{
    public Guid CandidatoId { get; set; }
    public Guid HabilidadId { get; set; }
    public NivelHabilidad Nivel { get; set; }
}
