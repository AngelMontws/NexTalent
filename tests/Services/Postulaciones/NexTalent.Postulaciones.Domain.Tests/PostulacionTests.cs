namespace NexTalent.Postulaciones.Domain.Tests;

public class PostulacionTests
{
    private static readonly Guid Reclutador = Guid.NewGuid();
    private static readonly DateTimeOffset Fecha = new(2026, 11, 25, 10, 0, 0, TimeSpan.FromHours(-6));

    // Copia independiente de la matriz del documento, en el orden del enum.
    // P = PerfilAbierto, A = Avance, D = Descarte, R = Reactivacion, - = no permitido.
    private static readonly string[] Matriz =
    [
        "-PAAAD",
        "--AAAD",
        "---AAD",
        "----AD",
        "-----D",
        "-RRR--"
    ];

    public static TheoryData<EstadoPostulacion, EstadoPostulacion, char> Combinaciones()
    {
        var datos = new TheoryData<EstadoPostulacion, EstadoPostulacion, char>();
        for (var i = 0; i < 6; i++)
        {
            for (var j = 0; j < 6; j++)
            {
                datos.Add((EstadoPostulacion)(i + 1), (EstadoPostulacion)(j + 1), Matriz[i][j]);
            }
        }

        return datos;
    }

    [Theory]
    [MemberData(nameof(Combinaciones))]
    public void Mover_CumpleLaMatriz(EstadoPostulacion desde, EstadoPostulacion hacia, char esperado)
    {
        var p = En(desde);
        var antes = p.Historial.Count;

        if (esperado == '-')
        {
            Assert.Throws<TransicionInvalidaException>(() => p.Mover(hacia, Reclutador, true, "motivo", Fecha));
            Assert.Equal(desde, p.EstadoActual);
            Assert.Equal(antes, p.Historial.Count);
            return;
        }

        var cambio = p.Mover(hacia, Reclutador, true, "motivo", Fecha);

        var causa = esperado switch
        {
            'P' => CausaCambio.PerfilAbierto,
            'A' => CausaCambio.Avance,
            'D' => CausaCambio.Descarte,
            _ => CausaCambio.Reactivacion
        };
        Assert.Equal(hacia, p.EstadoActual);
        Assert.Equal(antes + 1, p.Historial.Count);
        Assert.Same(cambio, p.Historial[^1]);
        Assert.Equal(desde, cambio.EstadoAnterior);
        Assert.Equal(hacia, cambio.EstadoNuevo);
        Assert.Equal(causa, cambio.Causa);
        Assert.Equal(Reclutador, cambio.CambiadoPor);
    }

    [Fact]
    public void Crear_IniciaEnEnviadaConPrimerRegistro()
    {
        var p = Nueva();

        var cambio = Assert.Single(p.Historial);
        Assert.Equal(EstadoPostulacion.Enviada, p.EstadoActual);
        Assert.Null(cambio.EstadoAnterior);
        Assert.Equal(p.CandidatoId, cambio.CambiadoPor);
        Assert.Equal(1, cambio.Secuencia);
        Assert.Equal(TimeSpan.Zero, cambio.Fecha.Offset);
        Assert.Equal(75m, p.CoincidenciaPct);
    }

    [Fact]
    public void Crear_VacanteCerrada_Falla()
    {
        Assert.Throws<VacanteNoAbiertaException>(() => Nueva(abierta: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Crear_SinFolio_Falla(string? folio)
    {
        Assert.ThrowsAny<ArgumentException>(() => Postulacion.Crear(
            Guid.NewGuid(), folio!, Guid.NewGuid(), Guid.NewGuid(), new(75m, 3, 4, 0), true, Fecha));
    }

    [Fact]
    public void Crear_SinCandidato_Falla()
    {
        Assert.Throws<ArgumentException>(() => Postulacion.Crear(
            Guid.NewGuid(), "F-1", Guid.Empty, Guid.NewGuid(), new(75m, 3, 4, 0), true, Fecha));
    }

    [Theory]
    [InlineData(EstadoPostulacion.Enviada, true)]
    [InlineData(EstadoPostulacion.Vista, false)]
    [InlineData(EstadoPostulacion.EnRevision, false)]
    [InlineData(EstadoPostulacion.Entrevista, false)]
    [InlineData(EstadoPostulacion.OfertaContratado, false)]
    [InlineData(EstadoPostulacion.NoSeleccionado, false)]
    public void AbrirPerfil_SoloCambiaDesdeEnviada(EstadoPostulacion desde, bool cambia)
    {
        var p = En(desde);
        var antes = p.Historial.Count;

        var cambio = p.AbrirPerfil(Reclutador, Fecha);

        Assert.Equal(cambia, cambio is not null);
        Assert.Equal(cambia ? EstadoPostulacion.Vista : desde, p.EstadoActual);
        Assert.Equal(antes + (cambia ? 1 : 0), p.Historial.Count);
    }

    [Theory]
    [InlineData(EstadoPostulacion.Enviada, true)]
    [InlineData(EstadoPostulacion.Vista, true)]
    [InlineData(EstadoPostulacion.EnRevision, true)]
    [InlineData(EstadoPostulacion.Entrevista, true)]
    [InlineData(EstadoPostulacion.OfertaContratado, false)]
    [InlineData(EstadoPostulacion.NoSeleccionado, false)]
    public void CerrarPorVacante_SoloPendientesYUnaVez(EstadoPostulacion desde, bool cierra)
    {
        var p = En(desde);
        var antes = p.Historial.Count;

        var cambio = p.CerrarPorVacante("Caducidad", Fecha);

        Assert.Equal(cierra, cambio is not null);
        Assert.Equal(cierra ? EstadoPostulacion.NoSeleccionado : desde, p.EstadoActual);
        Assert.Null(p.CerrarPorVacante("Caducidad", Fecha));
        Assert.Equal(antes + (cierra ? 1 : 0), p.Historial.Count);
        if (cierra)
        {
            Assert.Null(cambio!.CambiadoPor);
            Assert.Equal(CausaCambio.VacanteCerrada, cambio.Causa);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reactivar_SinMotivo_FallaSinCambios(string? motivo)
    {
        var p = En(EstadoPostulacion.NoSeleccionado);
        var antes = p.Historial.Count;

        Assert.Throws<MotivoObligatorioException>(
            () => p.Mover(EstadoPostulacion.Vista, Reclutador, true, motivo, Fecha));

        Assert.Equal(EstadoPostulacion.NoSeleccionado, p.EstadoActual);
        Assert.Equal(antes, p.Historial.Count);
    }

    [Theory]
    [InlineData(EstadoPostulacion.NoSeleccionado, EstadoPostulacion.Vista)]
    [InlineData(EstadoPostulacion.Vista, EstadoPostulacion.Entrevista)]
    public void Mover_VacanteCerrada_NoReactivaNiAvanza(EstadoPostulacion desde, EstadoPostulacion hacia)
    {
        var p = En(desde);
        var antes = p.Historial.Count;

        Assert.Throws<VacanteNoAbiertaException>(() => p.Mover(hacia, Reclutador, false, "motivo", Fecha));

        Assert.Equal(desde, p.EstadoActual);
        Assert.Equal(antes, p.Historial.Count);
    }

    [Fact]
    public void Descartar_VacanteCerrada_SePermite()
    {
        var p = En(EstadoPostulacion.OfertaContratado);

        p.Mover(EstadoPostulacion.NoSeleccionado, Reclutador, false, null, Fecha);

        Assert.Equal(EstadoPostulacion.NoSeleccionado, p.EstadoActual);
    }

    [Fact]
    public void Mover_SinReclutador_Falla()
    {
        Assert.Throws<ArgumentException>(
            () => Nueva().Mover(EstadoPostulacion.Vista, Guid.Empty, true, null, Fecha));
    }

    [Fact]
    public void Historial_ConservaOrdenYNoSeModificaDesdeFuera()
    {
        var p = En(EstadoPostulacion.Entrevista);

        Assert.Equal([1, 2], p.Historial.Select(c => c.Secuencia));
        Assert.Throws<NotSupportedException>(() => ((IList<CambioEstado>)p.Historial).Clear());
    }

    private static Postulacion Nueva(bool abierta = true) => Postulacion.Crear(
        Guid.NewGuid(), "F-1", Guid.NewGuid(), Guid.NewGuid(), new(75m, 3, 4, 0), abierta, Fecha);

    private static Postulacion En(EstadoPostulacion estado)
    {
        var p = Nueva();
        if (estado != EstadoPostulacion.Enviada)
        {
            p.Mover(estado, Reclutador, true, null, Fecha);
        }

        return p;
    }
}
