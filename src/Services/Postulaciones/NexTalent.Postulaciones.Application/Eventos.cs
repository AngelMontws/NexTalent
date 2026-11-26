using NexTalent.Contracts;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Application;

/// <summary>Convierte el dominio en contratos. El motivo nunca se copia.</summary>
internal static class Eventos
{
    public static PostulacionCreada Creada(Postulacion p, VacanteLocal v) => new(
        Guid.NewGuid(), p.CreadaEn, p.Id, p.Folio, p.CandidatoId, p.VacanteId,
        v.EmpresaId, v.Titulo, v.NombreEmpresa, p.CreadaEn);

    public static EstatusCambiado Cambio(Postulacion p, VacanteLocal v, CambioEstado c) => new(
        Guid.NewGuid(), c.Fecha, p.Id, p.Folio, p.CandidatoId, p.VacanteId,
        v.EmpresaId, v.Titulo, v.NombreEmpresa,
        c.EstadoAnterior!.Value.ToString(), c.EstadoNuevo.ToString(), c.Causa.ToString(),
        c.Fecha, c.Secuencia);
}
