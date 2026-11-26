using MassTransit;
using Microsoft.EntityFrameworkCore;
using NexTalent.Vacantes.Infrastructure.Persistence.Entities;

namespace NexTalent.Vacantes.Infrastructure.Persistence;

/// <summary>
/// Esquema vacantes. Sigue el Paquete Documental de Base de Datos v1.0 (02_Diccionario_de_Datos, hoja 2).
/// snake_case, fechas en UTC como datetime2(0), ids uniqueidentifier con NEWSEQUENTIALID() y sin llaves
/// foraneas hacia otros esquemas. El Full-Text, sp_BuscarVacantes y el trigger de la bitacora los agrega
/// Database/Scripts/02_Objetos_Extra.sql despues de las migraciones.
/// </summary>
public sealed class VacantesDbContext(DbContextOptions<VacantesDbContext> options) : DbContext(options)
{
    public const string Esquema = "vacantes";

    public DbSet<EmpresaResumen> Empresas => Set<EmpresaResumen>();
    public DbSet<Vacante> Vacantes => Set<Vacante>();
    public DbSet<Habilidad> Habilidades => Set<Habilidad>();
    public DbSet<VacanteHabilidad> VacanteHabilidades => Set<VacanteHabilidad>();
    public DbSet<ReporteVacante> Reportes => Set<ReporteVacante>();
    public DbSet<BitacoraModeracion> Bitacora => Set<BitacoraModeracion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);

        // Transactional Outbox e Inbox de MassTransit (RNF-09): InboxState, OutboxMessage y OutboxState.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VacantesDbContext).Assembly);
    }
}
