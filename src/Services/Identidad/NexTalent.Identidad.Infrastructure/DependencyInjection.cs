using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexTalent.Identidad.Application.Auth;
using NexTalent.Identidad.Infrastructure.Auth;
using NexTalent.Identidad.Infrastructure.Persistence;
using NexTalent.Identidad.Infrastructure.Registro;

namespace NexTalent.Identidad.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentidadInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentidadDbContext>(o => UsarSqlServer(o, connectionString));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IUsuariosAutenticacion, UsuariosAutenticacionRepositorio>();
        services.AddSingleton<IVerificadorContrasena, VerificadorContrasenaPbkdf2>();
        services.AddSingleton<IEmisorToken>(sp => new EmisorTokenJwt(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<TimeProvider>()));
        services.AddScoped<ServicioLogin>();

        return services;
    }

    internal static void UsarSqlServer(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentidadDbContext.Esquema));
}