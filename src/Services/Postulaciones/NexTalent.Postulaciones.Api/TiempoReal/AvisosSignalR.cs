using Microsoft.AspNetCore.SignalR;
using NexTalent.Contracts;
using NexTalent.Postulaciones.Application;

namespace NexTalent.Postulaciones.Api.TiempoReal;

/// <summary>
/// Envia los avisos al hub. El motivo del cambio nunca viaja (no viene en el evento).
/// Con una sola instancia basta; con varias haria falta un backplane (Redis) o una cola por instancia.
/// </summary>
public sealed class AvisosSignalR(IHubContext<HubEstatus> hub) : IAvisosTiempoReal
{
    public Task EstatusCambiadoAsync(EstatusCambiado e, CancellationToken ct)
    {
        var aviso = new AvisoEstatus(
            e.PostulacionId, e.Folio, e.VacanteId, e.TituloVacante, e.NombreEmpresa,
            e.EstadoAnterior, e.EstadoNuevo, e.Causa, e.CambiadoEn, e.Secuencia);

        return hub.Clients
            .Groups(HubEstatus.GrupoCandidato(e.CandidatoId), HubEstatus.GrupoEmpresa(e.EmpresaId))
            .SendAsync(HubEstatus.MensajeEstatus, aviso, ct);
    }

    public Task PostulacionCreadaAsync(PostulacionCreada e, CancellationToken ct) =>
        hub.Clients
            .Group(HubEstatus.GrupoEmpresa(e.EmpresaId))
            .SendAsync(HubEstatus.MensajeNuevaPostulacion,
                new AvisoNuevaPostulacion(e.PostulacionId, e.Folio, e.VacanteId, e.TituloVacante, e.CreadaEn), ct);
}

/// <summary>Lo que recibe el Frontend en "estatusCambiado". Secuencia sirve para ignorar avisos viejos.</summary>
public sealed record AvisoEstatus(
    Guid PostulacionId, string Folio, Guid VacanteId, string TituloVacante, string NombreEmpresa,
    string EstadoAnterior, string EstadoNuevo, string Causa, DateTimeOffset CambiadoEn, int Secuencia);

/// <summary>Lo que recibe el reclutador en "postulacionCreada".</summary>
public sealed record AvisoNuevaPostulacion(
    Guid PostulacionId, string Folio, Guid VacanteId, string TituloVacante, DateTimeOffset CreadaEn);
