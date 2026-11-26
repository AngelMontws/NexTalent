using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexTalent.Identidad.Infrastructure.Persistence;

namespace NexTalent.Identidad.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentidadInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentidadDbContext>(o => UsarSqlServer(o, connectionString));
        return services;
    }

    internal static void UsarSqlServer(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentidadDbContext.Esquema));
}