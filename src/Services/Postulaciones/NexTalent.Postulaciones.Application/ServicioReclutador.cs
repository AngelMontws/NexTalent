using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Application;

/// <summary>
/// Cumplidas y faltantes usan la copia actual del perfil; el porcentaje es la instantanea al postularse.
/// </summary>
public sealed record CandidatoDeVacante(
    Guid PostulacionId,
    string Folio,
    Guid CandidatoId,
    EstadoPostulacion Estado,
    decimal CoincidenciaPct,
    IReadOnlyList<Guid> HabilidadesCumplidas,
    IReadOnlyList<Guid> HabilidadesFaltantes,
    DateTimeOffset CreadaEn,
    DateTimeOffset EtapaActualizadaEn);

/// <summary>HU-06.01 y HU-06.02.</summary>
public sealed class ServicioReclutador(
    IPostulacionRepositorio postulaciones,
    IReplicas replicas,
    IUnidadDeTrabajo unidad,
    TimeProvider reloj)
{
    public async Task<IReadOnlyList<CandidatoDeVacante>> ListarCandidatosAsync(
        Usuario usuario, Guid vacanteId, CancellationToken ct)
    {
        usuario.ExigirReclutador();
        var vacante = await replicas.ObtenerVacanteAsync(vacanteId, ct) ?? throw new NoEncontradaException();
        usuario.ExigirReclutadorDe(vacante.EmpresaId);

        var lista = new List<CandidatoDeVacante>();
        foreach (var p in CalculadoraCoincidencia.Ordenar(await postulaciones.ListarPorVacanteAsync(vacanteId, ct)))
        {
            lista.Add(Describir(p, vacante, await replicas.ObtenerCandidatoAsync(p.CandidatoId, ct)));
        }

        return lista;
    }

    /// <summary>Abrir el perfil pasa Enviada a Vista; en otro estado no cambia nada.</summary>
    public async Task<CandidatoDeVacante> AbrirPerfilAsync(Usuario usuario, Guid postulacionId, CancellationToken ct)
    {
        var (p, vacante) = await CargarAsync(usuario, postulacionId, ct);

        if (p.AbrirPerfil(usuario.Id, reloj.GetUtcNow()) is { } cambio)
        {
            await GuardarCambioAsync(p, vacante, cambio, ct);
        }

        return Describir(p, vacante, await replicas.ObtenerCandidatoAsync(p.CandidatoId, ct));
    }

    public async Task<EstadoPostulacion> MoverAsync(
        Usuario usuario, Guid postulacionId, EstadoPostulacion destino, string? motivo, CancellationToken ct)
    {
        var (p, vacante) = await CargarAsync(usuario, postulacionId, ct);

        var cambio = p.Mover(destino, usuario.Id, vacante.Abierta, motivo, reloj.GetUtcNow());
        await GuardarCambioAsync(p, vacante, cambio, ct);
        return p.EstadoActual;
    }

    private async Task<(Postulacion, VacanteLocal)> CargarAsync(Usuario usuario, Guid id, CancellationToken ct)
    {
        usuario.ExigirReclutador();
        var p = await postulaciones.ObtenerAsync(id, ct) ?? throw new NoEncontradaException();
        var vacante = await replicas.ObtenerVacanteAsync(p.VacanteId, ct) ?? throw new VacanteNoDisponibleException();
        usuario.ExigirReclutadorDe(vacante.EmpresaId);
        return (p, vacante);
    }

    private async Task GuardarCambioAsync(Postulacion p, VacanteLocal v, CambioEstado cambio, CancellationToken ct)
    {
        await unidad.PublicarAsync(Eventos.Cambio(p, v, cambio), ct);
        await unidad.GuardarAsync(ct);
    }

    private static CandidatoDeVacante Describir(Postulacion p, VacanteLocal vacante, CandidatoLocal? candidato)
    {
        var niveles = (candidato?.Habilidades ?? [])
            .GroupBy(h => h.HabilidadId)
            .ToDictionary(g => g.Key, g => g.Max(h => h.Nivel));
        var grupos = vacante.Habilidades
            .Where(h => h.Requerida)
            .ToLookup(h => niveles.TryGetValue(h.HabilidadId, out var n) && n >= h.Nivel, h => h.HabilidadId);

        return new CandidatoDeVacante(
            p.Id, p.Folio, p.CandidatoId, p.EstadoActual, p.CoincidenciaPct, [.. grupos[true]], [.. grupos[false]],
            p.CreadaEn, p.EtapaActualizadaEn);
    }
}
