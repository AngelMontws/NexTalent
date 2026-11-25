using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Application;

public sealed record PostulacionResumen(
    Guid Id,
    string Folio,
    Guid VacanteId,
    string TituloVacante,
    string NombreEmpresa,
    EstadoPostulacion Estado,
    decimal CoincidenciaPct,
    int DiasEnEtapa,
    bool Activa,
    DateTimeOffset CreadaEn);

/// <summary>Sin autor ni motivo: falta acordar si el candidato los ve.</summary>
public sealed record EtapaHistorial(EstadoPostulacion? Anterior, EstadoPostulacion Nuevo, DateTimeOffset Fecha);

public sealed record PostulacionDetalle(PostulacionResumen Resumen, IReadOnlyList<EtapaHistorial> Historial);

/// <summary>HU-05.01, HU-05.02 y HU-05.04.</summary>
public sealed class ServicioCandidato(
    IPostulacionRepositorio postulaciones,
    IReplicas replicas,
    IGeneradorFolio folios,
    IUnidadDeTrabajo unidad,
    TimeProvider reloj)
{
    public const int HabilidadesMinimas = 3;

    public async Task<PostulacionResumen> PostularAsync(Usuario usuario, Guid vacanteId, CancellationToken ct)
    {
        usuario.ExigirCandidato();

        if (await postulaciones.BuscarAsync(usuario.Id, vacanteId, ct) is not null)
        {
            throw new YaPostuladoException();
        }

        var vacante = await replicas.ObtenerVacanteAsync(vacanteId, ct) ?? throw new VacanteNoDisponibleException();
        if (!vacante.Abierta)
        {
            throw new VacanteNoAbiertaException();
        }

        var candidato = await replicas.ObtenerCandidatoAsync(usuario.Id, ct)
            ?? new CandidatoLocal(usuario.Id, false, false, []);
        ExigirPerfilMinimo(candidato);

        var postulacion = Postulacion.Crear(
            Guid.CreateVersion7(),
            await folios.SiguienteAsync(ct),
            usuario.Id,
            vacanteId,
            CalculadoraCoincidencia.Calcular(candidato.Habilidades, vacante.Habilidades),
            vacante.Abierta,
            reloj.GetUtcNow());

        postulaciones.Agregar(postulacion);
        await unidad.PublicarAsync(Eventos.Creada(postulacion, vacante), ct);
        await unidad.GuardarAsync(ct);
        return Resumir(postulacion, vacante);
    }

    /// <summary>activas: true = activas, false = cerradas, null = todas.</summary>
    public async Task<IReadOnlyList<PostulacionResumen>> MisPostulacionesAsync(
        Usuario usuario, bool? activas, CancellationToken ct)
    {
        usuario.ExigirCandidato();

        var resultado = new List<PostulacionResumen>();
        foreach (var p in await postulaciones.ListarPorCandidatoAsync(usuario.Id, ct))
        {
            if (activas is null || MaquinaEstados.EsPendiente(p.EstadoActual) == activas)
            {
                resultado.Add(Resumir(p, await replicas.ObtenerVacanteAsync(p.VacanteId, ct)));
            }
        }

        return [.. resultado.OrderByDescending(r => r.CreadaEn)];
    }

    public async Task<PostulacionDetalle> DetalleAsync(Usuario usuario, Guid postulacionId, CancellationToken ct)
    {
        usuario.ExigirCandidato();

        var p = await postulaciones.ObtenerAsync(postulacionId, ct) ?? throw new NoEncontradaException();
        if (p.CandidatoId != usuario.Id)
        {
            throw new AccesoDenegadoException();
        }

        return new PostulacionDetalle(
            Resumir(p, await replicas.ObtenerVacanteAsync(p.VacanteId, ct)),
            [.. p.Historial.OrderBy(c => c.Secuencia)
                .Select(c => new EtapaHistorial(c.EstadoAnterior, c.EstadoNuevo, c.Fecha))]);
    }

    private static void ExigirPerfilMinimo(CandidatoLocal candidato)
    {
        var faltantes = new List<string>();
        if (!candidato.TieneNombre)
        {
            faltantes.Add("nombre");
        }

        if (!candidato.TieneContacto)
        {
            faltantes.Add("contacto");
        }

        if (candidato.Habilidades.Count < HabilidadesMinimas)
        {
            faltantes.Add("habilidades");
        }

        if (faltantes.Count > 0)
        {
            throw new PerfilIncompletoException(faltantes);
        }
    }

    private PostulacionResumen Resumir(Postulacion p, VacanteLocal? v) => new(
        p.Id, p.Folio, p.VacanteId, v?.Titulo ?? "", v?.NombreEmpresa ?? "", p.EstadoActual, p.CoincidenciaPct,
        Math.Max(0, (reloj.GetUtcNow() - p.Historial[^1].Fecha).Days),
        MaquinaEstados.EsPendiente(p.EstadoActual), p.CreadaEn);
}
