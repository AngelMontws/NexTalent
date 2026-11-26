using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Hangfire.SqlServer;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTalent.Notificaciones.Application;
using NexTalent.Notificaciones.Infrastructure.Data;
using NexTalent.Notificaciones.Infrastructure.Mensajeria;
using NexTalent.Notificaciones.Infrastructure.Tareas;

namespace NexTalent.Notificaciones.Infrastructure;

public static class DependencyInjection
{
    public const string EsquemaHangfire = "HangFire";

    public static IServiceCollection AgregarPersistencia(
        this IServiceCollection servicios, string conexion, IConfiguration configuracion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conexion);

        servicios.AddDbContext<NotificacionesDbContext>(o => Configurar(o, conexion));
        servicios.AddSingleton(TimeProvider.System);

        servicios.AddScoped<IContactos, ContactosRepositorio>();
        servicios.AddScoped<IVinculaciones, VinculacionesRepositorio>();
        servicios.AddScoped<INotificaciones, NotificacionesRepositorio>();
        servicios.AddScoped<IMensajesProcesados, MensajesProcesadosRepositorio>();
        servicios.AddScoped<IAlertas, AlertasRepositorio>();

        // Simulador de WhatsApp y correo como respaldo local seguro mientras no haya credenciales productivas.
        servicios.AddSingleton<SimuladorWhatsapp>();
        servicios.AddSingleton<IWhatsapp>(sp => sp.GetRequiredService<SimuladorWhatsapp>());
        servicios.AddSingleton<ICorreo, SimuladorCorreo>();

        var urlPostulaciones = configuracion["Servicios:PostulacionesUrl"] ?? "http://postulaciones:5003";
        var claveInterna = configuracion["Interno:ApiKey"] ?? "";
        servicios.AddHttpClient<ClientePostulacionesInterno>(c =>
        {
            c.BaseAddress = new Uri(urlPostulaciones);
            if (!string.IsNullOrWhiteSpace(claveInterna))
            {
                c.DefaultRequestHeaders.TryAddWithoutValidation("X-Internal-Api-Key", claveInterna);
            }
        });
        servicios.AddScoped<IPostulaciones>(sp => sp.GetRequiredService<ClientePostulacionesInterno>());

        var semillaClave = configuracion["Whatsapp:OtpKey"]
            ?? configuracion["Interno:ApiKey"]
            ?? configuracion["Jwt:Key"]
            ?? "NexTalent-Clave-Desarrollo-OTP-32Bytes!";
        var claveHmac = SHA256.HashData(Encoding.UTF8.GetBytes(semillaClave));

        var urlPublica = configuracion["Frontend:BaseUrl"];
        servicios.AddScoped(sp => new ServicioVinculacion(
            sp.GetRequiredService<IVinculaciones>(),
            sp.GetRequiredService<IContactos>(),
            sp.GetRequiredService<IWhatsapp>(),
            sp.GetRequiredService<TimeProvider>(),
            claveHmac));

        servicios.AddScoped(sp => new ServicioAvisos(
            sp.GetRequiredService<INotificaciones>(),
            sp.GetRequiredService<IContactos>(),
            sp.GetRequiredService<IWhatsapp>(),
            sp.GetRequiredService<ICorreo>(),
            sp.GetRequiredService<TimeProvider>(),
            vacanteId => string.IsNullOrWhiteSpace(urlPublica) ? null : $"{urlPublica.TrimEnd('/')}/vacantes?similar={vacanteId}"));

        servicios.AddScoped(sp => new ServicioWebhook(
            sp.GetRequiredService<ServicioVinculacion>(),
            sp.GetRequiredService<IContactos>(),
            sp.GetRequiredService<IAlertas>(),
            sp.GetRequiredService<IPostulaciones>(),
            sp.GetRequiredService<IMensajesProcesados>(),
            sp.GetRequiredService<IWhatsapp>(),
            string.IsNullOrWhiteSpace(urlPublica) ? null : $"{urlPublica.TrimEnd('/')}/mi-cuenta"));

        servicios.AddScoped<ServicioRecordatorioDiezDias>();
        return servicios;
    }

    /// <summary>Hangfire en su propio esquema [HangFire] sobre SQL Server (Acuerdo Punto 10).</summary>
    public static IServiceCollection AgregarHangfire(this IServiceCollection servicios, string conexion)
    {
        servicios.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(conexion, new SqlServerStorageOptions
            {
                SchemaName = EsquemaHangfire,
                PrepareSchemaIfNecessary = true,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        servicios.AddHangfireServer(o => o.ServerName = "nextalent-notificaciones");
        return servicios;
    }

    public static void ProgramarTareasRecurrentes(IRecurringJobManager gestor)
    {
        gestor.AddOrUpdate<ServicioAvisos>(
            "reintentar-notificaciones",
            s => s.ReintentarAsync(CancellationToken.None),
            Cron.Minutely());

        gestor.AddOrUpdate<ServicioRecordatorioDiezDias>(
            "recordatorio-10-dias-habiles",
            s => s.EjecutarAsync(CancellationToken.None),
            Cron.Daily(14, 0));
    }

    /// <summary>RabbitMQ con MassTransit 8 y Transactional Outbox sobre SQL Server (RNF-09).</summary>
    public static IServiceCollection AgregarMensajeria(
        this IServiceCollection servicios, string host, string usuario, string contrasena)
    {
        servicios.AddMassTransit(x =>
        {
            Registrar(x);
            x.UsingRabbitMq((contexto, rabbit) =>
            {
                rabbit.Host(host, "/", h =>
                {
                    h.Username(usuario);
                    h.Password(contrasena);
                });
                rabbit.ConfigureEndpoints(contexto);
            });
        });
        return servicios;
    }

    public static void Registrar(IBusRegistrationConfigurator x)
    {
        x.AddEntityFrameworkOutbox<NotificacionesDbContext>(o =>
        {
            o.UseSqlServer();
            o.UseBusOutbox();
        });

        x.AddConfigureEndpointsCallback((contexto, _, endpoint) =>
        {
            endpoint.UseMessageRetry(r => r.Intervals(100, 500, 1000, 5000));
            endpoint.UseEntityFrameworkOutbox<NotificacionesDbContext>(contexto);
        });

        x.AddConsumer<ContactoActualizadoConsumidor>();
        x.AddConsumer<PostulacionCreadaConsumidor>();
        x.AddConsumer<EstatusCambiadoConsumidor>();
        x.AddConsumer<VacanteCerradaNotificacionesConsumidor>();
        x.AddConsumer<EmpresaVerificadaNotificacionesConsumidor>();
    }

    public static void Configurar(DbContextOptionsBuilder opciones, string conexion) =>
        opciones.UseSqlServer(conexion, sql => sql.MigrationsHistoryTable(
            "__EFMigrationsHistory", NotificacionesDbContext.Esquema));
}

/// <summary>Usada solo por dotnet ef. Lee la cadena de ConnectionStrings__DefaultConnection.</summary>
public sealed class FabricaDisenoNotificaciones : IDesignTimeDbContextFactory<NotificacionesDbContext>
{
    public NotificacionesDbContext CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=127.0.0.1,1433;Database=NexTalentDb;User Id=sa;Password=definir-en-ConnectionStrings__DefaultConnection;TrustServerCertificate=True;";

        var opciones = new DbContextOptionsBuilder<NotificacionesDbContext>();
        DependencyInjection.Configurar(opciones, conexion);
        return new NotificacionesDbContext(opciones.Options);
    }
}
