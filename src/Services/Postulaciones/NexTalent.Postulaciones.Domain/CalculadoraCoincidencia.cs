namespace NexTalent.Postulaciones.Domain;

public enum NivelHabilidad
{
    Basico = 1,
    Intermedio = 2,
    Avanzado = 3
}

public sealed record HabilidadCandidato(Guid HabilidadId, NivelHabilidad Nivel);

public sealed record HabilidadVacante(Guid HabilidadId, NivelHabilidad Nivel, bool Requerida);

/// <summary>Las deseables no alteran el porcentaje: solo desempatan.</summary>
public sealed record ResultadoCoincidencia(
    decimal Porcentaje,
    int RequeridasCumplidas,
    int RequeridasTotal,
    int DeseablesCumplidas);

/// <summary>RN-03: requeridas cumplidas al nivel exigido / total de requeridas x 100.</summary>
public static class CalculadoraCoincidencia
{
    public static ResultadoCoincidencia Calcular(
        IReadOnlyCollection<HabilidadCandidato> candidato,
        IReadOnlyCollection<HabilidadVacante> vacante)
    {
        ArgumentNullException.ThrowIfNull(candidato);
        ArgumentNullException.ThrowIfNull(vacante);

        if (vacante.Select(v => v.HabilidadId).Distinct().Count() != vacante.Count)
        {
            throw new ArgumentException("La vacante repite una habilidad.", nameof(vacante));
        }

        var total = vacante.Count(v => v.Requerida);
        if (total == 0)
        {
            // HU-03.02 exige al menos una: sin ella el porcentaje no esta definido.
            throw new ArgumentException("La vacante debe tener al menos una habilidad requerida.", nameof(vacante));
        }

        var niveles = candidato
            .GroupBy(c => c.HabilidadId)
            .ToDictionary(g => g.Key, g => g.Max(c => c.Nivel));

        bool Cumple(HabilidadVacante v) =>
            niveles.TryGetValue(v.HabilidadId, out var nivel) && nivel >= v.Nivel;

        var requeridas = vacante.Count(v => v.Requerida && Cumple(v));
        var deseables = vacante.Count(v => !v.Requerida && Cumple(v));
        var porcentaje = Math.Round(requeridas * 100m / total, 2, MidpointRounding.AwayFromZero);

        return new ResultadoCoincidencia(porcentaje, requeridas, total, deseables);
    }

    /// <summary>HU-06.01: mayor porcentaje primero; si empatan, mas deseables cumplidas.</summary>
    public static IOrderedEnumerable<Postulacion> Ordenar(IEnumerable<Postulacion> postulaciones) =>
        postulaciones
            .OrderByDescending(p => p.CoincidenciaPct)
            .ThenByDescending(p => p.DeseablesCumplidas);
}
