using NexTalent.Contracts;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Application;

// Interfaces que implementa Infrastructure. Application no conoce SQL Server ni RabbitMQ.

public interface IPostulacionRepositorio
{
    Task<Postulacion?> ObtenerAsync(Guid id, CancellationToken ct);
    Task<Postulacion?> BuscarAsync(Guid candidatoId, Guid vacanteId, CancellationToken ct);
    Task<IReadOnlyList<Postulacion>> ListarPorCandidatoAsync(Guid candidatoId, CancellationToken ct);
    Task<IReadOnlyList<Postulacion>> ListarPorVacanteAsync(Guid vacanteId, CancellationToken ct);
    void Agregar(Postulacion postulacion);
}

/// <summary>Copias locales alimentadas por eventos: nunca se leen otros esquemas.</summary>
public interface IReplicas
{
    Task<VacanteLocal?> ObtenerVacanteAsync(Guid vacanteId, CancellationToken ct);
    Task<CandidatoLocal?> ObtenerCandidatoAsync(Guid candidatoId, CancellationToken ct);
}

public interface IGeneradorFolio
{
    Task<string> SiguienteAsync(CancellationToken ct);
}

public interface IUnidadDeTrabajo
{
    /// <summary>Se guarda en la Outbox, en la misma transaccion que GuardarAsync.</summary>
    Task PublicarAsync(PostulacionCreada evento, CancellationToken ct);

    /// <summary>Se guarda en la Outbox, en la misma transaccion que GuardarAsync.</summary>
    Task PublicarAsync(EstatusCambiado evento, CancellationToken ct);

    /// <summary>
    /// Lanza <see cref="ConflictoConcurrenciaException"/> si otra persona guardo antes,
    /// o <see cref="YaPostuladoException"/> si el UNIQUE detecta un duplicado simultaneo.
    /// </summary>
    Task GuardarAsync(CancellationToken ct);
}

public sealed record VacanteLocal(
    Guid VacanteId,
    Guid EmpresaId,
    string Titulo,
    string NombreEmpresa,
    bool Abierta,
    IReadOnlyCollection<HabilidadVacante> Habilidades);

public sealed record CandidatoLocal(
    Guid CandidatoId,
    bool TieneNombre,
    bool TieneContacto,
    IReadOnlyCollection<HabilidadCandidato> Habilidades);
