using NexTalent.Contracts;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Application.Tests;

public class ServiciosTests
{
    private static readonly Guid Empresa = Guid.NewGuid();
    private static readonly Guid Vacante = Guid.NewGuid();
    private static readonly Guid[] Habilidades = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    private readonly Memoria _m = new();
    private readonly Usuario _candidato = new(Guid.NewGuid(), Usuario.RolCandidato, null);
    private readonly Usuario _reclutador = new(Guid.NewGuid(), Usuario.RolReclutador, Empresa);
    private readonly ServicioCandidato _candidatos;
    private readonly ServicioReclutador _reclutadores;
    private readonly ServicioCierre _cierre;

    public ServiciosTests()
    {
        _candidatos = new(_m, _m, _m, _m, _m);
        _reclutadores = new(_m, _m, _m, _m);
        _cierre = new(_m, _m, _m, _m);
        PonerVacante(abierta: true);
        PonerCandidato(_candidato.Id, Habilidades);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Postular_CreaFolioInstantaneaYEvento()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);

        Assert.Equal("F-1", r.Folio);
        Assert.Equal(100m, r.CoincidenciaPct);
        Assert.True(r.Activa);
        var evento = Assert.IsType<PostulacionCreada>(Assert.Single(_m.Eventos));
        Assert.Equal((r.Id, r.Folio, Empresa), (evento.PostulacionId, evento.Folio, evento.EmpresaId));
        Assert.Equal(1, _m.Guardados);
    }

    [Fact]
    public async Task Postular_LaInstantaneaNoCambiaSiCambiaElPerfil()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);
        PonerCandidato(_candidato.Id, []);

        var detalle = await _candidatos.DetalleAsync(_candidato, r.Id, Ct);

        Assert.Equal(100m, detalle.Resumen.CoincidenciaPct);
    }

    [Fact]
    public async Task Postular_DosVeces_FallaSinDuplicar()
    {
        await _candidatos.PostularAsync(_candidato, Vacante, Ct);

        await Assert.ThrowsAsync<YaPostuladoException>(() => _candidatos.PostularAsync(_candidato, Vacante, Ct));

        Assert.Single(_m.Postulaciones);
        Assert.Single(_m.Eventos);
    }

    [Fact]
    public async Task Postular_YaPostuladoEnVacanteCerrada_AvisaQueYaSePostulo()
    {
        await _candidatos.PostularAsync(_candidato, Vacante, Ct);
        PonerVacante(abierta: false);

        await Assert.ThrowsAsync<YaPostuladoException>(() => _candidatos.PostularAsync(_candidato, Vacante, Ct));
    }

    [Fact]
    public async Task Postular_VacanteCerrada_Falla()
    {
        PonerVacante(abierta: false);

        await Assert.ThrowsAsync<VacanteNoAbiertaException>(() => _candidatos.PostularAsync(_candidato, Vacante, Ct));

        Assert.Equal(0, _m.Guardados);
    }

    [Fact]
    public async Task Postular_VacanteSinReplica_PermiteReintentar()
    {
        await Assert.ThrowsAsync<VacanteNoDisponibleException>(
            () => _candidatos.PostularAsync(_candidato, Guid.NewGuid(), Ct));
    }

    [Theory]
    [InlineData(false, true, 3, "nombre")]
    [InlineData(true, false, 3, "contacto")]
    [InlineData(true, true, 2, "habilidades")]
    [InlineData(false, false, 0, "nombre,contacto,habilidades")]
    public async Task Postular_PerfilIncompleto_IndicaQueFalta(bool nombre, bool contacto, int cuantas, string falta)
    {
        PonerCandidato(_candidato.Id, Habilidades[..cuantas], nombre, contacto);

        var error = await Assert.ThrowsAsync<PerfilIncompletoException>(
            () => _candidatos.PostularAsync(_candidato, Vacante, Ct));

        Assert.Equal(falta, string.Join(',', error.Faltantes));
        Assert.Empty(_m.Postulaciones);
    }

    [Fact]
    public async Task Postular_PerfilSinReplica_SeTrataComoIncompleto()
    {
        var nuevo = new Usuario(Guid.NewGuid(), Usuario.RolCandidato, null);

        await Assert.ThrowsAsync<PerfilIncompletoException>(() => _candidatos.PostularAsync(nuevo, Vacante, Ct));
    }

    [Theory]
    [InlineData("reclutador")]
    [InlineData("admin")]
    [InlineData("")]
    public async Task Candidato_RolIncorrecto_EsRechazado(string rol)
    {
        var otro = new Usuario(Guid.NewGuid(), rol, Empresa);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => _candidatos.PostularAsync(otro, Vacante, Ct));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => _candidatos.MisPostulacionesAsync(otro, null, Ct));
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => _candidatos.DetalleAsync(otro, Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task Detalle_DeOtroCandidato_EsRechazado()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);
        var otro = new Usuario(Guid.NewGuid(), Usuario.RolCandidato, null);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => _candidatos.DetalleAsync(otro, r.Id, Ct));
        Assert.Empty(await _candidatos.MisPostulacionesAsync(otro, null, Ct));
    }

    [Fact]
    public async Task Detalle_HistorialEnOrden()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);
        await _reclutadores.AbrirPerfilAsync(_reclutador, r.Id, Ct);
        await _reclutadores.MoverAsync(_reclutador, r.Id, EstadoPostulacion.Entrevista, null, Ct);

        var detalle = await _candidatos.DetalleAsync(_candidato, r.Id, Ct);

        Assert.Equal(
            [EstadoPostulacion.Enviada, EstadoPostulacion.Vista, EstadoPostulacion.Entrevista],
            detalle.Historial.Select(h => h.Nuevo));
    }

    [Fact]
    public async Task MisPostulaciones_FiltraYCuentaDiasEnLaEtapa()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);
        _m.Ahora = _m.Ahora.AddDays(2).AddHours(5);

        Assert.Equal(2, Assert.Single(await _candidatos.MisPostulacionesAsync(_candidato, true, Ct)).DiasEnEtapa);
        Assert.Empty(await _candidatos.MisPostulacionesAsync(_candidato, false, Ct));

        await _reclutadores.MoverAsync(_reclutador, r.Id, EstadoPostulacion.NoSeleccionado, null, Ct);

        Assert.Empty(await _candidatos.MisPostulacionesAsync(_candidato, true, Ct));
        Assert.Equal(0, Assert.Single(await _candidatos.MisPostulacionesAsync(_candidato, false, Ct)).DiasEnEtapa);
    }

    [Fact]
    public async Task AbrirPerfil_PublicaUnaSolaVez()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);

        await _reclutadores.AbrirPerfilAsync(_reclutador, r.Id, Ct);
        await _reclutadores.AbrirPerfilAsync(_reclutador, r.Id, Ct);

        var evento = Assert.Single(_m.Eventos.OfType<EstatusCambiado>());
        Assert.Equal(Estados.Enviada, evento.EstadoAnterior);
        Assert.Equal(Estados.Vista, evento.EstadoNuevo);
        Assert.Equal(CausasCambio.PerfilAbierto, evento.Causa);
        Assert.Equal(2, evento.Secuencia);
        Assert.Equal(2, _m.Guardados);
    }

    [Fact]
    public async Task Mover_ElMotivoNoViajaEnElEvento()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);

        await _reclutadores.MoverAsync(_reclutador, r.Id, EstadoPostulacion.NoSeleccionado, "texto privado", Ct);

        var evento = Assert.Single(_m.Eventos.OfType<EstatusCambiado>());
        Assert.DoesNotContain("texto privado", evento.ToString());
        Assert.Equal("texto privado", _m.Postulaciones[0].Historial[^1].Motivo);
    }

    [Fact]
    public async Task Mover_TransicionInvalida_NoPublicaNiGuarda()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);

        await Assert.ThrowsAsync<TransicionInvalidaException>(
            () => _reclutadores.MoverAsync(_reclutador, r.Id, EstadoPostulacion.Enviada, null, Ct));

        Assert.Single(_m.Eventos);
        Assert.Equal(1, _m.Guardados);
    }

    [Fact]
    public async Task Reclutador_DeOtraEmpresaOSinEmpresaORolIncorrecto_EsRechazado()
    {
        var r = await _candidatos.PostularAsync(_candidato, Vacante, Ct);
        Usuario[] intrusos =
        [
            new(Guid.NewGuid(), Usuario.RolReclutador, Guid.NewGuid()),
            new(Guid.NewGuid(), Usuario.RolReclutador, null),
            new(Guid.NewGuid(), "admin", Empresa),
            _candidato
        ];

        foreach (var u in intrusos)
        {
            await Assert.ThrowsAsync<AccesoDenegadoException>(
                () => _reclutadores.ListarCandidatosAsync(u, Vacante, Ct));
            await Assert.ThrowsAsync<AccesoDenegadoException>(
                () => _reclutadores.AbrirPerfilAsync(u, r.Id, Ct));
            await Assert.ThrowsAsync<AccesoDenegadoException>(
                () => _reclutadores.MoverAsync(u, r.Id, EstadoPostulacion.Vista, null, Ct));
        }

        Assert.Equal(EstadoPostulacion.Enviada, _m.Postulaciones[0].EstadoActual);
    }

    [Fact]
    public async Task Reclutador_PostulacionInexistente_NoEncontrada()
    {
        await Assert.ThrowsAsync<NoEncontradaException>(
            () => _reclutadores.AbrirPerfilAsync(_reclutador, Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task ListarCandidatos_OrdenaYMuestraCumplidasYFaltantes()
    {
        var menor = new Usuario(Guid.NewGuid(), Usuario.RolCandidato, null);
        PonerCandidato(menor.Id, [Habilidades[0], Guid.NewGuid(), Guid.NewGuid()]);
        await _candidatos.PostularAsync(menor, Vacante, Ct);
        await _candidatos.PostularAsync(_candidato, Vacante, Ct);

        var lista = await _reclutadores.ListarCandidatosAsync(_reclutador, Vacante, Ct);

        Assert.Equal([_candidato.Id, menor.Id], lista.Select(c => c.CandidatoId));
        Assert.Equal([Habilidades[0]], lista[1].HabilidadesCumplidas);
        Assert.Equal([Habilidades[1]], lista[1].HabilidadesFaltantes);
        Assert.Equal(50m, lista[1].CoincidenciaPct);
    }

    [Fact]
    public async Task Cierre_SoloPendientes_YRepetirNoHaceNada()
    {
        var oferta = new Usuario(Guid.NewGuid(), Usuario.RolCandidato, null);
        PonerCandidato(oferta.Id, Habilidades);
        var contratada = await _candidatos.PostularAsync(oferta, Vacante, Ct);
        await _reclutadores.MoverAsync(_reclutador, contratada.Id, EstadoPostulacion.OfertaContratado, null, Ct);
        var pendiente = await _candidatos.PostularAsync(_candidato, Vacante, Ct);
        _m.Eventos.Clear();
        PonerVacante(abierta: false);

        Assert.Equal(1, await _cierre.CerrarAsync(Vacante, CausasCierre.Caducidad, Ct));
        Assert.Equal(0, await _cierre.CerrarAsync(Vacante, CausasCierre.Caducidad, Ct));

        var evento = Assert.IsType<EstatusCambiado>(Assert.Single(_m.Eventos));
        Assert.Equal(pendiente.Id, evento.PostulacionId);
        Assert.Equal(Estados.NoSeleccionado, evento.EstadoNuevo);
        Assert.Equal(CausasCambio.VacanteCerrada, evento.Causa);
    }

    [Fact]
    public async Task Cierre_VacanteSinPostulaciones_NoFalla()
    {
        Assert.Equal(0, await _cierre.CerrarAsync(Guid.NewGuid(), CausasCierre.Manual, Ct));
    }

    [Fact]
    public async Task Whatsapp_CreaConCanalYDevuelveFolioTambienEnDuplicado()
    {
        var primera = await _candidatos.PostularDesdeWhatsappAsync(_candidato.Id, Vacante, Ct);
        var segunda = await _candidatos.PostularDesdeWhatsappAsync(_candidato.Id, Vacante, Ct);

        Assert.Equal(new ResultadoWhatsapp(ResultadoWhatsapp.Creada, "F-1"), primera);
        Assert.Equal(new ResultadoWhatsapp(ResultadoWhatsapp.YaPostulado, "F-1"), segunda);
        Assert.Equal(CanalOrigen.WhatsApp, Assert.Single(_m.Postulaciones).CanalOrigen);
    }

    [Fact]
    public async Task Whatsapp_PerfilIncompletoYVacanteCerrada_NoLanzan()
    {
        var incompleto = Guid.NewGuid();
        PonerCandidato(incompleto, [], nombre: true, contacto: true);
        Assert.Equal(ResultadoWhatsapp.PerfilIncompleto,
            (await _candidatos.PostularDesdeWhatsappAsync(incompleto, Vacante, Ct)).Tipo);

        PonerVacante(abierta: false);
        Assert.Equal(ResultadoWhatsapp.VacanteCerrada,
            (await _candidatos.PostularDesdeWhatsappAsync(_candidato.Id, Vacante, Ct)).Tipo);
    }

    [Fact]
    public async Task Web_GuardaCanalWeb()
    {
        await _candidatos.PostularAsync(_candidato, Vacante, Ct);

        Assert.Equal(CanalOrigen.Web, Assert.Single(_m.Postulaciones).CanalOrigen);
    }

    [Fact]
    public void ResultadoWhatsapp_CoincideConNotificaciones()
    {
        Assert.Equal(
            ["Creada", "PerfilIncompleto", "VacanteCerrada", "YaPostulado"],
            typeof(ResultadoWhatsapp).GetFields().Select(f => (string)f.GetRawConstantValue()!).Order());
    }

    [Fact]
    public void Contratos_UsanLosMismosTextosQueElDominio()
    {
        Assert.Equal(Textos(typeof(Estados)), Enum.GetNames<EstadoPostulacion>().Order());
        Assert.Equal(
            Textos(typeof(CausasCambio)),
            Enum.GetNames<CausaCambio>().Except([nameof(CausaCambio.Creacion)]).Order());
        Assert.Equal(Textos(typeof(Niveles)), Enum.GetNames<NivelHabilidad>().Order());
    }

    private static IEnumerable<string> Textos(Type tipo) =>
        tipo.GetFields().Select(f => (string)f.GetRawConstantValue()!).Order();

    private void PonerVacante(bool abierta) => _m.Vacantes[Vacante] = new(
        Vacante, Empresa, "Puesto", "Empresa", abierta,
        [
            new(Habilidades[0], NivelHabilidad.Basico, true),
            new(Habilidades[1], NivelHabilidad.Basico, true),
            new(Habilidades[2], NivelHabilidad.Basico, false)
        ]);

    private void PonerCandidato(Guid id, Guid[] habilidades, bool nombre = true, bool contacto = true) =>
        _m.Candidatos[id] = new(
            id, nombre, contacto, [.. habilidades.Select(h => new HabilidadCandidato(h, NivelHabilidad.Intermedio))]);
}
