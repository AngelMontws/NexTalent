using NexTalent.Contracts;
using NexTalent.Notificaciones.Domain;

namespace NexTalent.Notificaciones.Application.Tests;

public class ServicioAvisosTests
{
    private const string Telefono = "+525512345678";

    private readonly Memoria _m = new();
    private readonly Guid _usuario = Guid.NewGuid();
    private readonly Guid _postulacion = Guid.NewGuid();
    private readonly ServicioAvisos _s;

    public ServicioAvisosTests()
    {
        _s = new(_m, _m, _m, _m, _m, _ => "URL");
        _m.Contactos[_usuario] = new(_usuario, "a@ejemplo.com", Telefono, true);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ConOptIn_EnviaPorWhatsappYRegistra()
    {
        await _s.ProcesarAsync(Evento(Estados.Vista), Ct);

        Assert.Single(_m.Whatsapps);
        Assert.Empty(_m.Correos);
        var n = Assert.Single(_m.Avisos).Notificacion;
        Assert.Equal((EstadoEnvio.Enviada, 1, "wamid"), (n.Estado, n.Intentos, n.IdExterno));
    }

    [Fact]
    public async Task SinOptIn_SoloCorreo()
    {
        _m.Contactos[_usuario] = new(_usuario, "a@ejemplo.com", Telefono, false);

        await _s.ProcesarAsync(Evento(Estados.Vista), Ct);

        Assert.Empty(_m.Whatsapps);
        Assert.Single(_m.Correos);
    }

    [Fact]
    public async Task EventoRepetido_NoAvisaDosVeces()
    {
        var evento = Evento(Estados.Vista);

        await _s.ProcesarAsync(evento, Ct);
        await _s.ProcesarAsync(evento, Ct);

        Assert.Single(_m.Whatsapps);
    }

    [Fact]
    public async Task EventoAtrasado_SeIgnora()
    {
        await _s.ProcesarAsync(Evento(Estados.Entrevista, 4), Ct);
        await _s.ProcesarAsync(Evento(Estados.Vista, 2), Ct);

        Assert.Contains("Entrevista", Assert.Single(_m.Whatsapps).Texto);
    }

    [Fact]
    public async Task Falla_TresReintentosConEsperaCrecienteYLuegoCorreo()
    {
        _m.RespuestaWhatsapp = () => ResultadoEnvio.Fallo("500");
        await _s.ProcesarAsync(Evento(Estados.Vista), Ct);

        foreach (var minutos in new[] { 1, 5, 15 })
        {
            var antes = _m.Whatsapps.Count;
            _m.Ahora = _m.Ahora.AddMinutes(minutos).AddSeconds(-1);
            await _s.ReintentarAsync(Ct);
            Assert.Equal(antes, _m.Whatsapps.Count);
            Assert.Empty(_m.Correos);

            _m.Ahora = _m.Ahora.AddSeconds(1);
            await _s.ReintentarAsync(Ct);
            Assert.Equal(antes + 1, _m.Whatsapps.Count);
        }

        Assert.Equal(4, _m.Whatsapps.Count);
        Assert.Single(_m.Correos);
        Assert.Equal([EstadoEnvio.Fallida, EstadoEnvio.Enviada], _m.Avisos.Select(a => a.Notificacion.Estado));

        _m.Ahora = _m.Ahora.AddDays(1);
        await _s.ReintentarAsync(Ct);
        Assert.Equal(4, _m.Whatsapps.Count);
        Assert.Single(_m.Correos);
    }

    [Fact]
    public async Task Falla_YLuegoFunciona_NoEnviaCorreo()
    {
        _m.RespuestaWhatsapp = () => throw new HttpRequestException();
        await _s.ProcesarAsync(Evento(Estados.Vista), Ct);
        _m.RespuestaWhatsapp = () => ResultadoEnvio.Ok("wamid");
        _m.Ahora = _m.Ahora.AddMinutes(1);

        await _s.ReintentarAsync(Ct);

        Assert.Equal(EstadoEnvio.Enviada, Assert.Single(_m.Avisos).Notificacion.Estado);
        Assert.Empty(_m.Correos);
    }

    [Fact]
    public async Task FallaPermanente_PasaDirectoACorreo()
    {
        _m.RespuestaWhatsapp = () => ResultadoEnvio.Fallo("131030", permanente: true);

        await _s.ProcesarAsync(Evento(Estados.Vista), Ct);

        Assert.Single(_m.Whatsapps);
        Assert.Single(_m.Correos);
    }

    [Fact]
    public async Task BajaAntesDelReintento_YaNoEnviaWhatsapp()
    {
        _m.RespuestaWhatsapp = () => ResultadoEnvio.Fallo("500");
        await _s.ProcesarAsync(Evento(Estados.Vista), Ct);
        _m.Contactos[_usuario] = new(_usuario, "a@ejemplo.com", Telefono, false);
        _m.Ahora = _m.Ahora.AddMinutes(1);

        await _s.ReintentarAsync(Ct);

        Assert.Single(_m.Whatsapps);
        Assert.Single(_m.Correos);
    }

    [Fact]
    public async Task CorreoFalla_TambienSeReintenta()
    {
        _m.Contactos[_usuario] = new(_usuario, "a@ejemplo.com", null, false);
        _m.RespuestaCorreo = () => ResultadoEnvio.Fallo("smtp");
        await _s.ProcesarAsync(Evento(Estados.Vista), Ct);
        _m.RespuestaCorreo = () => ResultadoEnvio.Ok();
        _m.Ahora = _m.Ahora.AddMinutes(1);

        await _s.ReintentarAsync(Ct);

        Assert.Equal(2, _m.Correos.Count);
        Assert.Equal(EstadoEnvio.Enviada, Assert.Single(_m.Avisos).Notificacion.Estado);
    }

    [Fact]
    public async Task SinContacto_FallaParaQueSeReintenteElEvento()
    {
        _m.Contactos.Clear();

        await Assert.ThrowsAsync<ContactoNoDisponibleException>(() => _s.ProcesarAsync(Evento(Estados.Vista), Ct));
        Assert.Empty(_m.Avisos);
    }

    [Theory]
    [InlineData(Estados.Vista, "Vista")]
    [InlineData(Estados.EnRevision, "En revisión")]
    [InlineData(Estados.Entrevista, "Entrevista")]
    [InlineData(Estados.OfertaContratado, "Oferta/Contratado")]
    [InlineData(Estados.NoSeleccionado, "No seleccionado")]
    public void Texto_LlevaPuestoEmpresaFolioYEstado(string estado, string etiqueta)
    {
        var texto = Textos.Estatus(Evento(estado), "URL");

        Assert.StartsWith(
            "NexTalent | Actualización de postulación\nPuesto: Diseño\nEmpresa: Nube\nFolio: F-1\nEstado: " + etiqueta + ".",
            texto);
        Assert.Equal(estado == Estados.NoSeleccionado, texto.EndsWith("\nVacantes similares: URL"));
        Assert.True(texto.Length <= 1024);
    }

    [Fact]
    public void Texto_EstadoSinAviso_Falla()
    {
        Assert.Throws<ArgumentException>(() => Textos.Estatus(Evento(Estados.Enviada), null));
    }

    private EstatusCambiado Evento(string estado, int secuencia = 2) => new(
        Guid.NewGuid(), _m.Ahora, _postulacion, "F-1", _usuario, Guid.NewGuid(), Guid.NewGuid(), "Diseño", "Nube",
        Estados.Enviada, estado, CausasCambio.Avance, _m.Ahora, secuencia);
}
