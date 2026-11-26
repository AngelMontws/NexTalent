using NexTalent.Contracts;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Application.Tests;

/// <summary>Sustituye base de datos, replicas, Outbox y reloj en las pruebas.</summary>
internal sealed class Memoria : TimeProvider, IPostulacionRepositorio, IReplicas, IGeneradorFolio, IUnidadDeTrabajo
{
    private int _folio;

    public List<Postulacion> Postulaciones { get; } = [];
    public Dictionary<Guid, VacanteLocal> Vacantes { get; } = [];
    public Dictionary<Guid, CandidatoLocal> Candidatos { get; } = [];
    public List<object> Eventos { get; } = [];
    public int Guardados { get; private set; }
    public DateTimeOffset Ahora { get; set; } = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Ahora;

    public Task<Postulacion?> ObtenerAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Postulaciones.Find(p => p.Id == id));

    public Task<Postulacion?> BuscarAsync(Guid candidatoId, Guid vacanteId, CancellationToken ct) =>
        Task.FromResult(Postulaciones.Find(p => p.CandidatoId == candidatoId && p.VacanteId == vacanteId));

    public Task<IReadOnlyList<Postulacion>> ListarPorCandidatoAsync(Guid candidatoId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Postulacion>>(Postulaciones.FindAll(p => p.CandidatoId == candidatoId));

    public Task<IReadOnlyList<Postulacion>> ListarPorVacanteAsync(Guid vacanteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Postulacion>>(Postulaciones.FindAll(p => p.VacanteId == vacanteId));

    public void Agregar(Postulacion postulacion) => Postulaciones.Add(postulacion);

    public Task<VacanteLocal?> ObtenerVacanteAsync(Guid vacanteId, CancellationToken ct) =>
        Task.FromResult(Vacantes.GetValueOrDefault(vacanteId));

    public Task<CandidatoLocal?> ObtenerCandidatoAsync(Guid candidatoId, CancellationToken ct) =>
        Task.FromResult(Candidatos.GetValueOrDefault(candidatoId));

    public Task<string> SiguienteAsync(CancellationToken ct) => Task.FromResult($"F-{++_folio}");

    public Task PublicarAsync(PostulacionCreada evento, CancellationToken ct) => Anotar(evento);

    public Task PublicarAsync(EstatusCambiado evento, CancellationToken ct) => Anotar(evento);

    public Task GuardarAsync(CancellationToken ct)
    {
        Guardados++;
        return Task.CompletedTask;
    }

    private Task Anotar(object evento)
    {
        Eventos.Add(evento);
        return Task.CompletedTask;
    }
}
