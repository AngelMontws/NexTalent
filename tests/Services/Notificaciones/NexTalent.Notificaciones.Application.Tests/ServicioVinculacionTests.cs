namespace NexTalent.Notificaciones.Application.Tests;

public class ServicioVinculacionTests
{
    private const string Numero = "55 1234 5678";
    private const string Telefono = "+525512345678";

    private readonly Memoria _m = new();
    private readonly Guid _usuario = Guid.NewGuid();
    private readonly ServicioVinculacion _s;

    public ServicioVinculacionTests()
    {
        _s = new(_m, _m, _m, _m, new byte[32]);
        _m.Contactos[_usuario] = new(_usuario, "a@ejemplo.com", null, false);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CodigoCorrecto_VinculaYGuardaFecha()
    {
        Assert.True((await Solicitar()).Exito);
        Assert.Matches("^[0-9]{6}$", _m.Codigos[0]);
        Assert.False(_m.Contactos[_usuario].WhatsappOptIn);

        var r = await _s.VerificarAsync(_usuario, _m.Codigos[0], Ct);

        Assert.Equal(new Respuesta(true, Textos.NumeroVinculado), r);
        Assert.True(_m.Contactos[_usuario].WhatsappOptIn);
        Assert.Equal(Telefono, _m.Contactos[_usuario].TelefonoWhatsapp);
        Assert.Equal(_m.Ahora, _m.FechaConsentimiento);
    }

    [Fact]
    public async Task SinConsentimiento_NoEnviaNada()
    {
        Assert.False((await _s.SolicitarCodigoAsync(_usuario, Numero, false, Ct)).Exito);
        Assert.Empty(_m.Codigos);
    }

    [Fact]
    public async Task NumeroDeOtraPersona_NoEnviaNada()
    {
        var otra = Guid.NewGuid();
        _m.Contactos[otra] = new(otra, "b@ejemplo.com", Telefono, true);

        Assert.Equal(Textos.NumeroNoDisponible, (await Solicitar()).Mensaje);
        Assert.Empty(_m.Codigos);
    }

    [Fact]
    public async Task CodigoUsado_NoSirveDosVeces()
    {
        await Solicitar();
        await _s.VerificarAsync(_usuario, _m.Codigos[0], Ct);

        Assert.Equal(Textos.CodigoVencido, (await _s.VerificarAsync(_usuario, _m.Codigos[0], Ct)).Mensaje);
    }

    [Theory]
    [InlineData(599, true)]
    [InlineData(600, false)]
    public async Task Vigencia_DiezMinutos(int segundos, bool sirve)
    {
        await Solicitar();
        _m.Ahora = _m.Ahora.AddSeconds(segundos);

        Assert.Equal(sirve, (await _s.VerificarAsync(_usuario, _m.Codigos[0], Ct)).Exito);
    }

    [Fact]
    public async Task Reenvio_EsperaSesentaSegundosEInvalidaElAnterior()
    {
        await Solicitar();
        _m.Ahora = _m.Ahora.AddSeconds(59);
        Assert.False((await Solicitar()).Exito);
        Assert.Single(_m.Codigos);

        do
        {
            _m.Ahora = _m.Ahora.AddSeconds(60);
            Assert.Equal("Enviamos un nuevo código al número terminado en 5678.", (await Solicitar()).Mensaje);
        }
        while (_m.Codigos[^1] == _m.Codigos[0]);

        Assert.False((await _s.VerificarAsync(_usuario, _m.Codigos[0], Ct)).Exito);
        Assert.True((await _s.VerificarAsync(_usuario, _m.Codigos[^1], Ct)).Exito);
    }

    [Fact]
    public async Task CincoFallos_BloqueanQuinceMinutos()
    {
        await Solicitar();
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(Textos.CodigoIncorrecto, (await _s.VerificarAsync(_usuario, "xxxxxx", Ct)).Mensaje);
        }

        Assert.Equal(Textos.LimiteIntentos, (await _s.VerificarAsync(_usuario, "xxxxxx", Ct)).Mensaje);
        Assert.Equal(Textos.LimiteIntentos, (await _s.VerificarAsync(_usuario, _m.Codigos[0], Ct)).Mensaje);
        Assert.Equal(Textos.LimiteIntentos, (await Solicitar()).Mensaje);

        _m.Ahora = _m.Ahora.AddMinutes(15);
        Assert.True((await Solicitar()).Exito);
        Assert.True((await _s.VerificarAsync(_usuario, _m.Codigos[^1], Ct)).Exito);
    }

    [Fact]
    public async Task Reenviar_NoRegalaIntentos()
    {
        await Solicitar();
        for (var i = 0; i < 4; i++)
        {
            await _s.VerificarAsync(_usuario, "xxxxxx", Ct);
        }

        _m.Ahora = _m.Ahora.AddSeconds(60);
        await Solicitar();

        Assert.Equal(Textos.LimiteIntentos, (await _s.VerificarAsync(_usuario, "xxxxxx", Ct)).Mensaje);
    }

    [Fact]
    public async Task MetaFalla_IndicaNumeroNoDisponible()
    {
        _m.RespuestaWhatsapp = () => ResultadoEnvio.Fallo("131030");

        Assert.Equal(new Respuesta(false, Textos.NumeroNoDisponible), await Solicitar());
    }

    [Theory]
    [InlineData("BAJA", true)]
    [InlineData("  baja ", true)]
    [InlineData("quiero trabaja", false)]
    [InlineData("", false)]
    public async Task Baja_SeReconoceYConfirmaUnaVez(string texto, bool esBaja)
    {
        _m.Contactos[_usuario] = new(_usuario, "a@ejemplo.com", Telefono, true);

        Assert.Equal(esBaja, await _s.ProcesarTextoAsync(Telefono, texto, Ct));
        Assert.Equal(esBaja, await _s.ProcesarTextoAsync(Telefono, texto, Ct));

        Assert.Equal(!esBaja, _m.Contactos[_usuario].WhatsappOptIn);
        Assert.Equal(esBaja ? [(Telefono, Textos.Baja)] : [], _m.Whatsapps);
    }

    [Theory]
    [InlineData("5512345678", Telefono)]
    [InlineData("+52 (55) 1234-5678", Telefono)]
    [InlineData("551234567", null)]
    [InlineData("55123456789", null)]
    [InlineData("55a2345678", null)]
    [InlineData("+15551234567", null)]
    [InlineData("", null)]
    public void NormalizarTelefono_SoloMexico(string numero, string? esperado)
    {
        Assert.Equal(esperado, ServicioVinculacion.NormalizarTelefono(numero));
    }

    [Fact]
    public void ClaveCorta_NoSePermite()
    {
        Assert.Throws<ArgumentException>(() => new ServicioVinculacion(_m, _m, _m, _m, new byte[31]));
    }

    private Task<Respuesta> Solicitar() => _s.SolicitarCodigoAsync(_usuario, Numero, true, Ct);
}
