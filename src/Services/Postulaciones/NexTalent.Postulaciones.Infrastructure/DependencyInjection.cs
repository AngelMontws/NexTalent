using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using NexTalent.Postulaciones.Application;
using NexTalent.Postulaciones.Infrastructure.Data;
using NexTalent.Postulaciones.Infrastructure.Mensajeria;

namespace NexTalent.Postulaciones.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, string conexion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conexion);

        servicios.AddDbContext<PostulacionesDbContext>(o => Configurar(o, conexion));
        servicios.AddScoped<IPostulacionRepositorio, PostulacionRepositorio>();
        servicios.AddScoped<IReplicas, Replicas>();
        servicios.AddScoped<IGeneradorFolio, GeneradorFolio>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddScoped<ServicioCandidato>();
        servicios.AddScoped<ServicioReclutador>();
        servicios.AddScoped<ServicioCierre>();
        return servicios;
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

    /// <summary>Consumidores, Outbox, Inbox y reintentos. Tambien lo usan las pruebas.</summary>
    public static void Registrar(IBusRegistrationConfigurator x)
    {
        x.AddEntityFrameworkOutbox<PostulacionesDbContext>(o =>
        {
            o.UseSqlServer();
            o.UseBusOutbox();
        });

        // Inbox: un mensaje repetido no se procesa dos veces. Los cambios del consumidor y lo que publica
        // se guardan en la misma transaccion.
        x.AddConfigureEndpointsCallback((contexto, _, endpoint) =>
        {
            endpoint.UseMessageRetry(r => r.Intervals(100, 500, 1000, 5000));
            endpoint.UseEntityFrameworkOutbox<PostulacionesDbContext>(contexto);
        });

        x.AddConsumer<VacantePublicadaConsumidor>();
        x.AddConsumer<VacanteCerradaConsumidor>();
        x.AddConsumer<PerfilHabilidadesActualizadoConsumidor>();
    }

    /// <summary>El historial de migraciones va en el esquema del servicio, para no chocar con los demas.</summary>
    public static void Configurar(DbContextOptionsBuilder opciones, string conexion) =>
        opciones.UseSqlServer(conexion, sql => sql.MigrationsHistoryTable(
            "__EFMigrationsHistory", PostulacionesDbContext.Esquema));
}

/// <summary>
/// Lo usa dotnet ef. Crear migraciones no se conecta; aplicarlas requiere la variable
/// ConnectionStrings__DefaultConnection.
/// </summary>
public sealed class FabricaDiseno : IDesignTimeDbContextFactory<PostulacionesDbContext>
{
    public PostulacionesDbContext CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=NexTalent;Integrated Security=false;TrustServerCertificate=True";
        var opciones = new DbContextOptionsBuilder<PostulacionesDbContext>();
        DependencyInjection.Configurar(opciones, conexion);
        return new PostulacionesDbContext(opciones.Options);
    }
}
