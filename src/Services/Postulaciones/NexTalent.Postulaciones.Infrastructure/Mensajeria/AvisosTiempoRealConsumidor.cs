using MassTransit;
using NexTalent.Contracts;
using NexTalent.Postulaciones.Application;

namespace NexTalent.Postulaciones.Infrastructure.Mensajeria;

/// <summary>
/// Reenvia los eventos propios al hub /hubs/estatus. Como llegan desde la Outbox, el aviso sale solo si la
/// transaccion se confirmo, y tambien cubre los cierres automaticos por VacanteCerrada.
/// </summary>
public sealed class AvisosTiempoRealConsumidor(IAvisosTiempoReal avisos)
    : IConsumer<EstatusCambiado>, IConsumer<PostulacionCreada>
{
    public Task Consume(ConsumeContext<EstatusCambiado> contexto) =>
        avisos.EstatusCambiadoAsync(contexto.Message, contexto.CancellationToken);

    public Task Consume(ConsumeContext<PostulacionCreada> contexto) =>
        avisos.PostulacionCreadaAsync(contexto.Message, contexto.CancellationToken);
}
