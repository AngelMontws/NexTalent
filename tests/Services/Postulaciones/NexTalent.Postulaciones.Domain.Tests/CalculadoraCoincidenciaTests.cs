namespace NexTalent.Postulaciones.Domain.Tests;

public class CalculadoraCoincidenciaTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid C = Guid.NewGuid();

    [Theory]
    [InlineData(NivelHabilidad.Basico, NivelHabilidad.Basico, true)]
    [InlineData(NivelHabilidad.Basico, NivelHabilidad.Intermedio, false)]
    [InlineData(NivelHabilidad.Basico, NivelHabilidad.Avanzado, false)]
    [InlineData(NivelHabilidad.Intermedio, NivelHabilidad.Basico, true)]
    [InlineData(NivelHabilidad.Intermedio, NivelHabilidad.Intermedio, true)]
    [InlineData(NivelHabilidad.Intermedio, NivelHabilidad.Avanzado, false)]
    [InlineData(NivelHabilidad.Avanzado, NivelHabilidad.Basico, true)]
    [InlineData(NivelHabilidad.Avanzado, NivelHabilidad.Intermedio, true)]
    [InlineData(NivelHabilidad.Avanzado, NivelHabilidad.Avanzado, true)]
    public void Calcular_ComparaNiveles(NivelHabilidad tiene, NivelHabilidad exige, bool cumple)
    {
        var r = CalculadoraCoincidencia.Calcular([new(A, tiene)], [new(A, exige, true)]);

        Assert.Equal(cumple ? 100m : 0m, r.Porcentaje);
    }

    [Fact]
    public void Calcular_RedondeaADosDecimales()
    {
        var r = CalculadoraCoincidencia.Calcular(
            [new(A, NivelHabilidad.Avanzado), new(B, NivelHabilidad.Avanzado)],
            [Requerida(A), Requerida(B), Requerida(C)]);

        Assert.Equal(66.67m, r.Porcentaje);
        Assert.Equal(2, r.RequeridasCumplidas);
        Assert.Equal(3, r.RequeridasTotal);
    }

    [Fact]
    public void Calcular_DeseablesNoAlteranElPorcentaje()
    {
        var r = CalculadoraCoincidencia.Calcular(
            [new(B, NivelHabilidad.Avanzado)],
            [Requerida(A), new(B, NivelHabilidad.Basico, false)]);

        Assert.Equal(0m, r.Porcentaje);
        Assert.Equal(1, r.DeseablesCumplidas);
    }

    [Fact]
    public void Calcular_CandidatoSinHabilidades_DaCero()
    {
        Assert.Equal(0m, CalculadoraCoincidencia.Calcular([], [Requerida(A)]).Porcentaje);
    }

    [Fact]
    public void Calcular_SinRequeridas_Falla()
    {
        Assert.Throws<ArgumentException>(() => CalculadoraCoincidencia.Calcular([], []));
        Assert.Throws<ArgumentException>(
            () => CalculadoraCoincidencia.Calcular([], [new(A, NivelHabilidad.Basico, false)]));
    }

    [Fact]
    public void Calcular_VacanteRepiteHabilidad_Falla()
    {
        Assert.Throws<ArgumentException>(
            () => CalculadoraCoincidencia.Calcular([], [Requerida(A), Requerida(A)]));
    }

    [Fact]
    public void Calcular_CandidatoRepiteHabilidad_UsaElNivelMasAlto()
    {
        var r = CalculadoraCoincidencia.Calcular(
            [new(A, NivelHabilidad.Basico), new(A, NivelHabilidad.Avanzado)],
            [new(A, NivelHabilidad.Avanzado, true)]);

        Assert.Equal(100m, r.Porcentaje);
    }

    [Fact]
    public void Ordenar_PorPorcentajeYDesempataPorDeseables()
    {
        var alto = Con(80m, 0);
        var empateMenos = Con(50m, 1);
        var empateMas = Con(50m, 2);

        var orden = CalculadoraCoincidencia.Ordenar([empateMenos, alto, empateMas]).Select(p => p.Id);

        Assert.Equal([alto.Id, empateMas.Id, empateMenos.Id], orden);
    }

    private static HabilidadVacante Requerida(Guid id) => new(id, NivelHabilidad.Basico, true);

    private static Postulacion Con(decimal porcentaje, int deseables) => Postulacion.Crear(
        Guid.NewGuid(), "F-1", Guid.NewGuid(), Guid.NewGuid(),
        new(porcentaje, 1, 1, deseables), true, DateTimeOffset.UnixEpoch);
}
