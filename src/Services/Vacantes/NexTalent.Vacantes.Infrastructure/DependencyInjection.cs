using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using NexTalent.Vacantes.Infrastructure.Mensajeria;
using NexTalent.Vacantes.Infrastructure.Persistence;

namespace NexTalent.Vacantes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVacantesInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<VacantesDbContext>(o => UsarSqlServer(o, connectionString));
        services.AddScoped(_ => new BuscadorVacantesDapper(connectionString));
        services.AddSingleton(TimeProvider.System);
        return services;
    }

    public static IServiceCollection AgregarMensajeria(
        this IServiceCollection services, string host, string usuario, string contrasena)
    {
        services.AddMassTransit(x =>
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
        return services;
    }

    public static void Registrar(IBusRegistrationConfigurator x)
    {
        x.AddEntityFrameworkOutbox<VacantesDbContext>(o =>
        {
            o.UseSqlServer();
            o.UseBusOutbox();
        });

        x.AddConfigureEndpointsCallback((contexto, _, endpoint) =>
        {
            endpoint.UseMessageRetry(r => r.Intervals(100, 500, 1000, 5000));
            endpoint.UseEntityFrameworkOutbox<VacantesDbContext>(contexto);
        });

        x.AddConsumer<EmpresaVerificadaConsumidor>();
    }

    public static void UsarSqlServer(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", VacantesDbContext.Esquema));
}

/// <summary>Usada solo por dotnet ef. Lee la cadena de ConnectionStrings__DefaultConnection.</summary>
public sealed class VacantesDbContextFactory : IDesignTimeDbContextFactory<VacantesDbContext>
{
    public VacantesDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=127.0.0.1,1433;Database=NexTalentDb;User Id=sa;Password=definir-en-ConnectionStrings__DefaultConnection;TrustServerCertificate=True;";

        var opciones = new DbContextOptionsBuilder<VacantesDbContext>();
        DependencyInjection.UsarSqlServer(opciones, cadena);
        return new VacantesDbContext(opciones.Options);
    }
}
