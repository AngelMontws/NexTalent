using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexTalent.Notificaciones.Domain;

namespace NexTalent.Notificaciones.Infrastructure.Data;

/// <summary>
/// Esquema notificaciones. Sigue el Paquete Documental de Base de Datos v1.0 y los acuerdos con Base de Datos
/// (docs/backend2/modelo-datos.md y acuerdos de revision). Fechas en UTC como datetime2(0), snake_case y cero
/// llaves foraneas hacia otros esquemas. Hangfire usa su propio esquema [HangFire].
/// </summary>
public sealed class NotificacionesDbContext(DbContextOptions<NotificacionesDbContext> opciones) : DbContext(opciones)
{
    public const string Esquema = "notificaciones";

    public DbSet<NotificacionFila> Notificaciones => Set<NotificacionFila>();
    public DbSet<ContactoReplica> Contactos => Set<ContactoReplica>();
    public DbSet<CodigoWhatsappFila> CodigosWhatsapp => Set<CodigoWhatsappFila>();
    public DbSet<MensajeWhatsappProcesado> MensajesProcesados => Set<MensajeWhatsappProcesado>();
    public DbSet<PostulacionSeguimiento> Seguimientos => Set<PostulacionSeguimiento>();
    public DbSet<AlertaVacanteEnviada> AlertasEnviadas => Set<AlertaVacanteEnviada>();

    protected override void ConfigureConventions(ModelConfigurationBuilder convenciones)
    {
        convenciones.Properties<DateTimeOffset>().HaveConversion<FechaUtc>().HaveColumnType("datetime2(0)");
        convenciones.Properties<DateTimeOffset?>().HaveConversion<FechaUtc>().HaveColumnType("datetime2(0)");
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.HasDefaultSchema(Esquema);

        // Transactional Outbox e Inbox de MassTransit (RNF-09): InboxState, OutboxMessage y OutboxState.
        modelo.AddInboxStateEntity();
        modelo.AddOutboxMessageEntity();
        modelo.AddOutboxStateEntity();

        modelo.Entity<NotificacionFila>(e =>
        {
            e.ToTable("notificacion", t =>
            {
                t.HasCheckConstraint("ck_notificacion_canal", Valores("canal", typeof(Canal)));
                t.HasCheckConstraint("ck_notificacion_estado_envio", Valores("estado_envio", typeof(EstadoEnvio)));
                t.HasCheckConstraint("ck_notificacion_intentos", "intentos >= 0");
            });

            e.HasKey(x => x.Id).HasName("pk_notificacion");
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.Canal).HasColumnName("canal").HasConversion<string>().HasColumnType("varchar(15)");
            e.Property(x => x.Plantilla).HasColumnName("plantilla").HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.EstadoEnvio).HasColumnName("estado_envio").HasConversion<string>().HasColumnType("varchar(20)");
            e.Property(x => x.Intentos).HasColumnName("intentos");
            e.Property(x => x.CreadaEn).HasColumnName("creada_en");
            e.Property(x => x.EventoId).HasColumnName("evento_id");
            e.Property(x => x.PostulacionId).HasColumnName("postulacion_id");
            e.Property(x => x.VacanteId).HasColumnName("vacante_id");
            e.Property(x => x.EmpresaId).HasColumnName("empresa_id");
            e.Property(x => x.ReferenciaFolio).HasColumnName("referencia_folio").HasColumnType("varchar(20)");
            e.Property(x => x.TituloVacante).HasColumnName("titulo_vacante").HasMaxLength(200);
            e.Property(x => x.NombreEmpresa).HasColumnName("nombre_empresa").HasMaxLength(200);
            e.Property(x => x.EstadoAnterior).HasColumnName("estado_anterior").HasColumnType("varchar(25)");
            e.Property(x => x.EstadoNuevo).HasColumnName("estado_nuevo").HasColumnType("varchar(25)");
            e.Property(x => x.CausaCambio).HasColumnName("causa_cambio").HasColumnType("varchar(25)");
            e.Property(x => x.CambiadoEn).HasColumnName("cambiado_en");
            e.Property(x => x.Secuencia).HasColumnName("secuencia");
            e.Property(x => x.ProximoIntentoEn).HasColumnName("proximo_intento_en");
            e.Property(x => x.EnviadaEn).HasColumnName("enviada_en");
            e.Property(x => x.IdExterno).HasColumnName("id_externo").HasColumnType("varchar(128)");
            e.Property(x => x.UltimoError).HasColumnName("ultimo_error").HasMaxLength(500);
            e.Property(x => x.LeidaEn).HasColumnName("leida_en");

            e.HasIndex(x => new { x.EventoId, x.UsuarioId, x.Canal })
                .IsUnique()
                .HasDatabaseName("uq_notificacion_evento_usuario_canal")
                .HasFilter("[evento_id] IS NOT NULL");
            e.HasIndex(x => new { x.EstadoEnvio, x.ProximoIntentoEn })
                .HasDatabaseName("ix_notificacion_reintentos");
            e.HasIndex(x => new { x.PostulacionId, x.Secuencia })
                .HasDatabaseName("ix_notificacion_postulacion_secuencia")
                .HasFilter("[postulacion_id] IS NOT NULL");
        });

        modelo.Entity<ContactoReplica>(e =>
        {
            e.ToTable("contacto_replica", t =>
            {
                t.HasCheckConstraint("ck_contacto_replica_rol", "rol IN ('Candidato', 'Reclutador', 'Administrador')");
                t.HasCheckConstraint("ck_contacto_replica_opt_in_fecha",
                    "whatsapp_opt_in = 0 OR whatsapp_opt_in_fecha IS NOT NULL");
                t.HasCheckConstraint("ck_contacto_replica_frecuencia",
                    "frecuencia_alertas IN ('Diaria', 'Semanal', 'Ninguna')");
            });

            e.HasKey(x => x.UsuarioId).HasName("pk_contacto_replica");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id").ValueGeneratedNever();
            e.Property(x => x.Rol).HasColumnName("rol").HasColumnType("varchar(20)").IsRequired();
            e.Property(x => x.EmpresaId).HasColumnName("empresa_id");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            e.Property(x => x.TelefonoWhatsapp).HasColumnName("telefono_whatsapp").HasColumnType("varchar(20)");
            e.Property(x => x.WhatsappUserId).HasColumnName("whatsapp_user_id").HasColumnType("varchar(128)");
            e.Property(x => x.WhatsappOptIn).HasColumnName("whatsapp_opt_in").HasDefaultValue(false);
            e.Property(x => x.WhatsappOptInFecha).HasColumnName("whatsapp_opt_in_fecha");
            e.Property(x => x.WhatsappBajaFecha).HasColumnName("whatsapp_baja_fecha");
            e.Property(x => x.FrecuenciaAlertas).HasColumnName("frecuencia_alertas").HasColumnType("varchar(15)")
                .HasDefaultValue("Ninguna").IsRequired();
            e.Property(x => x.VersionEvento).HasColumnName("version_evento").HasDefaultValue(0L);

            e.HasIndex(x => x.TelefonoWhatsapp)
                .IsUnique()
                .HasDatabaseName("uq_contacto_replica_telefono")
                .HasFilter("[telefono_whatsapp] IS NOT NULL");
            e.HasIndex(x => x.WhatsappUserId)
                .IsUnique()
                .HasDatabaseName("uq_contacto_replica_bsuid")
                .HasFilter("[whatsapp_user_id] IS NOT NULL");
            e.HasIndex(x => new { x.EmpresaId, x.Rol })
                .HasDatabaseName("ix_contacto_replica_empresa");
        });

        modelo.Entity<CodigoWhatsappFila>(e =>
        {
            e.ToTable("codigo_whatsapp", t =>
                t.HasCheckConstraint("ck_codigo_whatsapp_intentos", "intentos >= 0"));

            e.HasKey(x => x.Id).HasName("pk_codigo_whatsapp");
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.Telefono).HasColumnName("telefono").HasColumnType("varchar(20)");
            e.Property(x => x.CodigoHash).HasColumnName("codigo_hash").HasColumnType("varbinary(32)");
            e.Property(x => x.EnviadoEn).HasColumnName("enviado_en");
            e.Property(x => x.ExpiraEn).HasColumnName("expira_en");
            e.Property(x => x.Intentos).HasColumnName("intentos").HasDefaultValue(0);
            e.Property(x => x.BloqueadoHasta).HasColumnName("bloqueado_hasta");
            e.Property(x => x.UsadoEn).HasColumnName("usado_en");

            e.HasIndex(x => x.UsuarioId).IsUnique().HasDatabaseName("uq_codigo_whatsapp_usuario");
        });

        modelo.Entity<MensajeWhatsappProcesado>(e =>
        {
            e.ToTable("mensaje_whatsapp_procesado");
            e.HasKey(x => x.MensajeId).HasName("pk_mensaje_whatsapp_procesado");
            e.Property(x => x.MensajeId).HasColumnName("mensaje_id").HasColumnType("varchar(128)");
            e.Property(x => x.RecibidoEn).HasColumnName("recibido_en");
            e.Property(x => x.Resultado).HasColumnName("resultado").HasColumnType("varchar(30)").IsRequired();
        });

        modelo.Entity<PostulacionSeguimiento>(e =>
        {
            e.ToTable("postulacion_seguimiento");
            e.HasKey(x => x.PostulacionId).HasName("pk_postulacion_seguimiento");
            e.Property(x => x.PostulacionId).HasColumnName("postulacion_id").ValueGeneratedNever();
            e.Property(x => x.CandidatoId).HasColumnName("candidato_id");
            e.Property(x => x.VacanteId).HasColumnName("vacante_id");
            e.Property(x => x.EmpresaId).HasColumnName("empresa_id");
            e.Property(x => x.Folio).HasColumnName("folio").HasColumnType("varchar(20)").IsRequired();
            e.Property(x => x.TituloVacante).HasColumnName("titulo_vacante").HasMaxLength(200).IsRequired();
            e.Property(x => x.EstadoActual).HasColumnName("estado_actual").HasColumnType("varchar(25)").IsRequired();
            e.Property(x => x.Pendiente).HasColumnName("pendiente");
            e.Property(x => x.UltimoCambioEn).HasColumnName("ultimo_cambio_en");
            e.Property(x => x.Secuencia).HasColumnName("secuencia");
            e.Property(x => x.Recordatorio10dEnviado).HasColumnName("recordatorio_10d_enviado").HasDefaultValue(false);
            e.Property(x => x.RecordatorioEnviadoEn).HasColumnName("recordatorio_enviado_en");

            e.HasIndex(x => new { x.Pendiente, x.Recordatorio10dEnviado, x.UltimoCambioEn })
                .HasDatabaseName("ix_postulacion_seguimiento_pendientes");
            e.HasIndex(x => x.VacanteId).HasDatabaseName("ix_postulacion_seguimiento_vacante");
        });

        modelo.Entity<AlertaVacanteEnviada>(e =>
        {
            e.ToTable("alerta_vacante_enviada");
            e.HasKey(x => new { x.UsuarioId, x.VacanteId }).HasName("pk_alerta_vacante_enviada");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.VacanteId).HasColumnName("vacante_id");
            e.Property(x => x.IdAlerta).HasColumnName("id_alerta").HasColumnType("varchar(128)");
            e.Property(x => x.Boton).HasColumnName("boton").HasColumnType("varchar(30)");
            e.Property(x => x.EnviadaEn).HasColumnName("enviada_en");

            e.HasIndex(x => new { x.UsuarioId, x.IdAlerta, x.Boton })
                .HasDatabaseName("ix_alerta_vacante_enviada_alerta_boton")
                .HasFilter("[id_alerta] IS NOT NULL");
        });
    }

    private static string Valores(string columna, Type tipo) =>
        $"{columna} IN ({string.Join(", ", Enum.GetNames(tipo).Select(n => $"'{n}'"))})";
}

internal sealed class FechaUtc() : ValueConverter<DateTimeOffset, DateTime>(
    fecha => fecha.UtcDateTime,
    fecha => new DateTimeOffset(DateTime.SpecifyKind(fecha, DateTimeKind.Utc)));

/// <summary>Registro persistente de notificaciones.notificacion sin almacenar el cuerpo del mensaje.</summary>
public sealed class NotificacionFila
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Canal Canal { get; set; }
    public string Plantilla { get; set; } = "";
    public EstadoEnvio EstadoEnvio { get; set; }
    public int Intentos { get; set; }
    public DateTimeOffset CreadaEn { get; set; }
    public Guid? EventoId { get; set; }
    public Guid? PostulacionId { get; set; }
    public Guid? VacanteId { get; set; }
    public Guid? EmpresaId { get; set; }
    public string? ReferenciaFolio { get; set; }
    public string? TituloVacante { get; set; }
    public string? NombreEmpresa { get; set; }
    public string? EstadoAnterior { get; set; }
    public string? EstadoNuevo { get; set; }
    public string? CausaCambio { get; set; }
    public DateTimeOffset? CambiadoEn { get; set; }
    public int? Secuencia { get; set; }
    public DateTimeOffset? ProximoIntentoEn { get; set; }
    public DateTimeOffset? EnviadaEn { get; set; }
    public string? IdExterno { get; set; }
    public string? UltimoError { get; set; }

    /// <summary>Solo canal Web (NT-041): cuando la persona marco el aviso como leido.</summary>
    public DateTimeOffset? LeidaEn { get; set; }
}

/// <summary>Copia local de contacto y consentimiento WhatsApp alimentada por ContactoActualizado.</summary>
public sealed class ContactoReplica
{
    public Guid UsuarioId { get; set; }
    public string Rol { get; set; } = "Candidato";
    public Guid? EmpresaId { get; set; }
    public string Email { get; set; } = "";
    public string? TelefonoWhatsapp { get; set; }
    public string? WhatsappUserId { get; set; }
    public bool WhatsappOptIn { get; set; }
    public DateTimeOffset? WhatsappOptInFecha { get; set; }
    public DateTimeOffset? WhatsappBajaFecha { get; set; }
    public string FrecuenciaAlertas { get; set; } = "Ninguna";
    public long VersionEvento { get; set; }
}

/// <summary>Estado de verificacion OTP de 6 digitos con HMAC-SHA256 (HU-07.01).</summary>
public sealed class CodigoWhatsappFila
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string? Telefono { get; set; }
    public byte[]? CodigoHash { get; set; }
    public DateTimeOffset? EnviadoEn { get; set; }
    public DateTimeOffset? ExpiraEn { get; set; }
    public int Intentos { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }
    public DateTimeOffset? UsadoEn { get; set; }
}

/// <summary>Idempotencia de webhooks entrantes de Meta (HU-07.03).</summary>
public sealed class MensajeWhatsappProcesado
{
    public string MensajeId { get; set; } = "";
    public DateTimeOffset RecibidoEn { get; set; }
    public string Resultado { get; set; } = "Procesado";
}

/// <summary>Seguimiento local de postulaciones activas para el recordatorio de 10 dias habiles (RN-04).</summary>
public sealed class PostulacionSeguimiento
{
    public Guid PostulacionId { get; set; }
    public Guid CandidatoId { get; set; }
    public Guid VacanteId { get; set; }
    public Guid EmpresaId { get; set; }
    public string Folio { get; set; } = "";
    public string TituloVacante { get; set; } = "";
    public string EstadoActual { get; set; } = "";
    public bool Pendiente { get; set; }
    public DateTimeOffset UltimoCambioEn { get; set; }
    public int Secuencia { get; set; }
    public bool Recordatorio10dEnviado { get; set; }
    public DateTimeOffset? RecordatorioEnviadoEn { get; set; }
}

/// <summary>Evita repetir vacantes en alertas P-01 y vincula el boton interactivo con la vacante (HU-07.02/03).</summary>
public sealed class AlertaVacanteEnviada
{
    public Guid UsuarioId { get; set; }
    public Guid VacanteId { get; set; }
    public string? IdAlerta { get; set; }
    public string? Boton { get; set; }
    public DateTimeOffset EnviadaEn { get; set; }
}
