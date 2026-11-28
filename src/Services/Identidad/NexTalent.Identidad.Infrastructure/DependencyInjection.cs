using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexTalent.Identidad.Infrastructure.Persistence;
using NexTalent.Identidad.Infrastructure.Registro;

namespace NexTalent.Identidad.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentidadInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentidadDbContext>(o => UsarSqlServer(o, connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ServicioRegistro>();
        return services;
    }

    // MassTransit + EF Outbox no estaba registrado en Identidad: se agrega aqui.
    public static IServiceCollection AgregarMensajeria(
        this IServiceCollection services, string host, string usuario, string contrasena)
    {
        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<IdentidadDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });

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

    internal static void UsarSqlServer(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentidadDbContext.Esquema));
}