using System.Text;
using System.Text.Json;
using NexTalent.Notificaciones.Infrastructure;

namespace NexTalent.Notificaciones.Application.Tests;

public class WebhookTests : IAlertas, IPostulaciones, IMensajesProcesados
{
    private const string Telefono = "+525512345678";
    private const string Secreto = "secreto-de-prueba";

    private readonly Memoria _m = new();
    private readonly Guid _usuario = Guid.NewGuid();
    private readonly Guid _vacante = Guid.NewGuid();
    private readonly HashSet<string> _procesados = [];
    private readonly ServicioWebhook _s;
    private int _llamadas;
    private Func<ResultadoPostulacion> _resultado = () => new(TipoResultado.Creada, "F-1");

    public WebhookTests()
    {
        _m.Contactos[_usuario] = new(_usuario, "a@ejemplo.com", Telefono, true);
        _s = new(new(_m, _m, _m, _m, new byte[32]), _m, this, this, this, _m, "URL");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Valor calculado aparte: HMAC-SHA256 con clave "key" (ejemplo conocido del algoritmo).
    [Fact]
    public void Firma_CoincideConUnValorConocido()
    {
        var cuerpo = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");

        Assert.Equal(
            "sha256=f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8",
            FirmaWebhook.Firmar(cuerpo, "key"));
    }

    [Fact]
    public void Firma_RechazaCuerpoAlteradoSecretoDistintoOVacia()
    {
        var cuerpo = Encoding.UTF8.GetBytes("{}");
        var firma = FirmaWebhook.Firmar(cuerpo, Secreto);

        Assert.True(FirmaWebhook.EsValida(cuerpo, firma, Secreto));
        Assert.False(FirmaWebhook.EsValida(Encoding.UTF8.GetBytes("{ }"), firma, Secreto));
        Assert.False(FirmaWebhook.EsValida(cuerpo, firma, "otro"));
        Assert.False(FirmaWebhook.EsValida(cuerpo, null, Secreto));
        Assert.False(FirmaWebhook.EsValida(cuerpo, "", Secreto));
        Assert.False(FirmaWebhook.EsValida(cuerpo, FirmaWebhook.Firmar(cuerpo, ""), ""));
    }

    [Theory]
    [InlineData("subscribe", "token", "token", true)]
    [InlineData("subscribe", "otro", "token", false)]
    [InlineData("subscribe", null, "token", false)]
    [InlineData("subscribe", "", "", false)]
    [InlineData("unsubscribe", "token", "token", false)]
    public void Verificacion_ExigeModoYToken(string modo, string? token, string esperado, bool valida)
    {
        Assert.Equal(valida, FirmaWebhook.EsVerificacionValida(modo, token, esperado));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("""{"entry":[{"changes":[{"field":"messages","value":{"statuses":[{"id":"x"}]}}]}]}""")]
    [InlineData("""{"entry":[{"changes":[{"field":"messages","value":{"messages":[{"id":"x","type":"image"}]}}]}]}""")]
    [InlineData("""{"entry":[{"changes":[{"field":"messages","value":{"messages":[{"id":"x","type":"text","text":5}]}}]}]}""")]
    public void Lector_IgnoraLoQueNoEsTextoNiBoton(string json)
    {
        Assert.Empty(LectorWebhook.Leer(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Lector_JsonInvalido_Falla()
    {
        Assert.ThrowsAny<JsonException>(() => LectorWebhook.Leer(Encoding.UTF8.GetBytes("no es json")));
    }

    [Fact]
    public void Lector_AceptaMensajeSinTelefono()
    {
        const string json = """
            {"entry":[{"changes":[{"field":"messages","value":{"messages":[
              {"from_user_id":"MX.1","id":"w1","type":"button","context":{"id":"alerta"},
               "button":{"payload":"p","text":"Postularme 1"}}]}}]}]}
            """;

        Assert.Equal(
            new MensajeEntrante("w1", null, "MX.1", null, "Postularme 1", "alerta"),
            Assert.Single(LectorWebhook.Leer(Encoding.UTF8.GetBytes(json))));
    }

    [Fact]
    public async Task Postularme_RespondeConFolio_YNoSeRepite()
    {
        var mensajes = Boton();

        await _s.ProcesarAsync(mensajes, Ct);
        await _s.ProcesarAsync(mensajes, Ct);

        Assert.Equal(1, _llamadas);
        Assert.Contains("Folio: F-1", Assert.Single(_m.Whatsapps).Texto);
    }

    [Theory]
    [InlineData(TipoResultado.Creada, "¡Postulación enviada!")]
    [InlineData(TipoResultado.YaPostulado, "Ya te postulaste a esta vacante.")]
    [InlineData(TipoResultado.PerfilIncompleto, "Tu perfil está incompleto.")]
    [InlineData(TipoResultado.VacanteCerrada, "La vacante ya está cerrada.")]
    public async Task Postularme_CadaResultadoTieneSuTexto(TipoResultado tipo, string inicio)
    {
        _resultado = () => new(tipo, tipo is TipoResultado.Creada or TipoResultado.YaPostulado ? "F-1" : null);

        await _s.ProcesarAsync(Boton(), Ct);

        var texto = Assert.Single(_m.Whatsapps).Texto;
        Assert.StartsWith(inicio, texto);
        Assert.Equal(tipo == TipoResultado.PerfilIncompleto, texto.EndsWith("\nURL"));
    }

    [Fact]
    public async Task Postularme_SiPostulacionesNoResponde_MetaPuedeReintentar()
    {
        var mensajes = Boton();
        _resultado = () => throw new HttpRequestException();
        await Assert.ThrowsAsync<HttpRequestException>(() => _s.ProcesarAsync(mensajes, Ct));
        Assert.Empty(_procesados);

        _resultado = () => new(TipoResultado.Creada, "F-1");
        await _s.ProcesarAsync(mensajes, Ct);

        Assert.Single(_m.Whatsapps);
    }

    [Fact]
    public async Task Postularme_SiFallaLaConfirmacion_SeReintentaYConservaElFolio()
    {
        var mensajes = Boton();
        _m.RespuestaWhatsapp = () => ResultadoEnvio.Fallo("500");
        await Assert.ThrowsAsync<EnvioFallidoException>(() => _s.ProcesarAsync(mensajes, Ct));

        _m.RespuestaWhatsapp = () => ResultadoEnvio.Ok();
        _resultado = () => new(TipoResultado.YaPostulado, "F-1");
        await _s.ProcesarAsync(mensajes, Ct);

        Assert.Contains("Folio: F-1", _m.Whatsapps[^1].Texto);
    }

    [Theory]
    [InlineData("otra-alerta", "Postularme 1")]
    [InlineData("alerta", "Postularme 9")]
    public async Task Postularme_AlertaOBotonDesconocido_NoHaceNada(string alerta, string boton)
    {
        await _s.ProcesarAsync(Boton(alerta, boton), Ct);

        Assert.Equal(0, _llamadas);
        Assert.Empty(_m.Whatsapps);
    }

    [Fact]
    public async Task SinOptIn_NoPostulaNiResponde()
    {
        _m.Contactos[_usuario] = new(_usuario, "a@ejemplo.com", Telefono, false);

        await _s.ProcesarAsync(Boton(), Ct);

        Assert.Equal(0, _llamadas);
        Assert.Empty(_m.Whatsapps);
    }

    [Fact]
    public async Task Baja_LlegaPorElWebhook()
    {
        await _s.ProcesarAsync(LectorWebhook.Leer(SimuladorWhatsapp.WebhookTexto("5215512345678", "baja")), Ct);

        Assert.False(_m.Contactos[_usuario].WhatsappOptIn);
        Assert.Equal(Textos.Baja, Assert.Single(_m.Whatsapps).Texto);
    }

    [Fact]
    public async Task SinTelefono_IdentificaPorBsuid()
    {
        _m.UsuariosWhatsapp["MX.1"] = _usuario;

        await _s.ProcesarAsync([new("w1", null, "MX.1", null, "Postularme 1", "alerta")], Ct);

        Assert.Equal(1, _llamadas);
    }

    [Theory]
    [InlineData("5215512345678", Telefono)]
    [InlineData("525512345678", Telefono)]
    [InlineData("+525512345678", Telefono)]
    [InlineData("15551234567", null)]
    [InlineData("abc", null)]
    public async Task Telefono_AceptaLosFormatosDeMeta(string telefono, string? esperado)
    {
        await _s.ProcesarAsync([new("w1", telefono, null, null, "Postularme 1", "alerta")], Ct);

        Assert.Equal(esperado is null ? 0 : 1, _llamadas);
    }

    [Fact]
    public async Task Simulador_GuardaLoEnviadoConLimite()
    {
        var simulador = new SimuladorWhatsapp();
        for (var i = 0; i < 101; i++)
        {
            Assert.True((await simulador.EnviarTextoAsync(Telefono, $"{i}", Ct)).Exito);
        }

        Assert.Equal(100, simulador.Enviados.Count);
        Assert.Equal("100", simulador.Enviados.Last().Texto);
    }

    Task<Guid?> IAlertas.BuscarVacanteAsync(Guid usuarioId, string idAlerta, string boton, CancellationToken ct) =>
        Task.FromResult<Guid?>(
            usuarioId == _usuario && idAlerta == "alerta" && boton == "Postularme 1" ? _vacante : null);

    Task<ResultadoPostulacion> IPostulaciones.PostularAsync(Guid candidatoId, Guid vacanteId, CancellationToken ct)
    {
        Assert.Equal((_usuario, _vacante), (candidatoId, vacanteId));
        _llamadas++;
        return Task.FromResult(_resultado());
    }

    Task<bool> IMensajesProcesados.ExisteAsync(string mensajeId, CancellationToken ct) =>
        Task.FromResult(_procesados.Contains(mensajeId));

    Task IMensajesProcesados.MarcarAsync(string mensajeId, CancellationToken ct)
    {
        _procesados.Add(mensajeId);
        return Task.CompletedTask;
    }

    private static IReadOnlyList<MensajeEntrante> Boton(string alerta = "alerta", string boton = "Postularme 1") =>
        LectorWebhook.Leer(SimuladorWhatsapp.WebhookBoton("5215512345678", alerta, boton));
}
