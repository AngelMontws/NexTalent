using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NexTalent.Contracts;
using NexTalent.Postulaciones.Application;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Infrastructure.Data;

public sealed class PostulacionRepositorio(PostulacionesDbContext db) : IPostulacionRepositorio
{
    public Task<Postulacion?> ObtenerAsync(Guid id, CancellationToken ct) =>
        ConHistorial().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Postulacion?> BuscarAsync(Guid candidatoId, Guid vacanteId, CancellationToken ct) =>
        ConHistorial().FirstOrDefaultAsync(p => p.CandidatoId == candidatoId && p.VacanteId == vacanteId, ct);

    public async Task<IReadOnlyList<Postulacion>> ListarPorCandidatoAsync(Guid candidatoId, CancellationToken ct) =>
        await ConHistorial().Where(p => p.CandidatoId == candidatoId).ToListAsync(ct);

    public async Task<IReadOnlyList<Postulacion>> ListarPorVacanteAsync(Guid vacanteId, CancellationToken ct) =>
        await ConHistorial().Where(p => p.VacanteId == vacanteId).ToListAsync(ct);

    public void Agregar(Postulacion postulacion) => db.Postulaciones.Add(postulacion);

    private IQueryable<Postulacion> ConHistorial() => db.Postulaciones.Include(p => p.Historial).AsSplitQuery();
}

public sealed class Replicas(PostulacionesDbContext db) : IReplicas
{
    public async Task<VacanteLocal?> ObtenerVacanteAsync(Guid vacanteId, CancellationToken ct)
    {
        var v = await db.Vacantes.AsNoTracking().FirstOrDefaultAsync(x => x.VacanteId == vacanteId, ct);
        if (v is null)
        {
            return null;
        }

        var habilidades = await db.VacanteHabilidades.AsNoTracking()
            .Where(h => h.VacanteId == vacanteId)
            .Select(h => new HabilidadVacante(h.HabilidadId, h.Nivel, h.Requerida))
            .ToListAsync(ct);

        return new VacanteLocal(v.VacanteId, v.EmpresaId, v.Titulo, v.NombreEmpresa ?? "", v.Abierta, habilidades);
    }

    public async Task<CandidatoLocal?> ObtenerCandidatoAsync(Guid candidatoId, CancellationToken ct)
    {
        var c = await db.Candidatos.AsNoTracking().FirstOrDefaultAsync(x => x.CandidatoId == candidatoId, ct);
        if (c is null)
        {
            return null;
        }

        var habilidades = await db.CandidatoHabilidades.AsNoTracking()
            .Where(h => h.CandidatoId == candidatoId)
            .Select(h => new HabilidadCandidato(h.HabilidadId, h.Nivel))
            .ToListAsync(ct);

        return new CandidatoLocal(c.CandidatoId, c.TieneNombre, c.TieneContacto, habilidades);
    }
}

/// <summary>Folio NT-000001, de una secuencia de SQL Server. Puede dejar huecos.</summary>
public sealed class GeneradorFolio(PostulacionesDbContext db) : IGeneradorFolio
{
    public async Task<string> SiguienteAsync(CancellationToken ct)
    {
        // Comando directo: EF Core envolveria la consulta en una subconsulta, donde SQL Server no permite NEXT VALUE FOR.
        var conexion = db.Database.GetDbConnection();
        var cerrada = conexion.State == System.Data.ConnectionState.Closed;
        if (cerrada)
        {
            await conexion.OpenAsync(ct);
        }

        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText =
                $"SELECT NEXT VALUE FOR [{PostulacionesDbContext.Esquema}].[{PostulacionesDbContext.SecuenciaFolio}]";
            comando.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            var siguiente = Convert.ToInt64(await comando.ExecuteScalarAsync(ct));
            return $"NT-{siguiente:D6}";
        }
        finally
        {
            if (cerrada)
            {
                await conexion.CloseAsync();
            }
        }
    }
}

/// <summary>
/// Con MassTransit configurado, Publish guarda el evento en OutboxMessage y SaveChanges lo confirma junto con
/// los datos: o se guardan ambos o ninguno (RNF-09). Sin RabbitMQ configurado, solo se registra un aviso.
/// </summary>
public sealed class UnidadDeTrabajo(
    PostulacionesDbContext db, ILogger<UnidadDeTrabajo> log, IPublishEndpoint? bus = null) : IUnidadDeTrabajo
{
    private const int LlaveDuplicada = 2601;
    private const int RestriccionUnica = 2627;

    private readonly List<object> _eventos = [];

    public IReadOnlyList<object> EventosSinPublicar => _eventos;

    public Task PublicarAsync(PostulacionCreada evento, CancellationToken ct) =>
        bus is null ? Anotar(evento) : bus.Publish(evento, ct);

    public Task PublicarAsync(EstatusCambiado evento, CancellationToken ct) =>
        bus is null ? Anotar(evento) : bus.Publish(evento, ct);

    public async Task GuardarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoConcurrenciaException();
        }
        catch (DbUpdateException ex) when (Duplicado(ex, "uq_postulacion_candidato_vacante"))
        {
            throw new YaPostuladoException();
        }
        catch (DbUpdateException ex) when (Duplicado(ex, "uq_historial_estado_postulacion_secuencia"))
        {
            // Otra persona registro el mismo paso del historial primero: mismo caso que rowversion.
            throw new ConflictoConcurrenciaException();
        }

        if (_eventos.Count > 0)
        {
            log.LogWarning("{Cantidad} eventos sin publicar: RabbitMQ no esta configurado.", _eventos.Count);
        }
    }

    private static bool Duplicado(DbUpdateException ex, string indice) =>
        ex.InnerException is SqlException { Number: LlaveDuplicada or RestriccionUnica } sql
        && sql.Message.Contains(indice, StringComparison.Ordinal);

    private Task Anotar(object evento)
    {
        _eventos.Add(evento);
        return Task.CompletedTask;
    }
}
