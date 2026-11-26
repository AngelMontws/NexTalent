namespace NexTalent.Postulaciones.Application;

/// <summary>
/// HU-05.03: cierra las postulaciones pendientes de una vacante. Se puede repetir sin efecto.
/// Quien consume el evento marca antes la copia local de la vacante como cerrada.
/// </summary>
public sealed class ServicioCierre(
    IPostulacionRepositorio postulaciones,
    IReplicas replicas,
    IUnidadDeTrabajo unidad,
    TimeProvider reloj)
{
    /// <returns>Cuantas postulaciones se cerraron.</returns>
    public async Task<int> CerrarAsync(Guid vacanteId, string causa, CancellationToken ct)
    {
        var pendientes = await postulaciones.ListarPorVacanteAsync(vacanteId, ct);
        if (pendientes.Count == 0)
        {
            return 0;
        }

        var vacante = await replicas.ObtenerVacanteAsync(vacanteId, ct) ?? throw new VacanteNoDisponibleException();
        var ahora = reloj.GetUtcNow();
        var cerradas = 0;

        foreach (var p in pendientes)
        {
            if (p.CerrarPorVacante(causa, ahora) is { } cambio)
            {
                await unidad.PublicarAsync(Eventos.Cambio(p, vacante, cambio), ct);
                cerradas++;
            }
        }

        if (cerradas > 0)
        {
            await unidad.GuardarAsync(ct);
        }

        return cerradas;
    }

    /// <summary>RN-04: sincroniza la bandera recordatorio_10d_enviado tras enviar el recordatorio desde Notificaciones.</summary>
    public async Task<bool> MarcarRecordatorioAsync(Guid postulacionId, CancellationToken ct)
    {
        var p = await postulaciones.ObtenerAsync(postulacionId, ct);
        if (p is null)
        {
            return false;
        }

        if (!p.RecordatorioEnviado)
        {
            p.MarcarRecordatorioEnviado();
            await unidad.GuardarAsync(ct);
        }

        return true;
    }
}
