using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using NexTalent.Postulaciones.Application;
using NexTalent.Postulaciones.Infrastructure.Data;

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
