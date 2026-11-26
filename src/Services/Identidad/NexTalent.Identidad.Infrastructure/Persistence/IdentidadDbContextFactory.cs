using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NexTalent.Identidad.Infrastructure.Persistence;

/// <summary>Usada solo por dotnet ef. Lee la cadena de ConnectionStrings__DefaultConnection.</summary>
public sealed class IdentidadDbContextFactory : IDesignTimeDbContextFactory<IdentidadDbContext>
{
    public IdentidadDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=127.0.0.1,1433;Database=NexTalentDb;User Id=sa;Password=disenio;TrustServerCertificate=True;";

        var opciones = new DbContextOptionsBuilder<IdentidadDbContext>();
        DependencyInjection.UsarSqlServer(opciones, cadena);
        return new IdentidadDbContext(opciones.Options);
    }
}