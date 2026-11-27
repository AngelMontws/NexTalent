using System.Collections.Concurrent;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexTalent.Contracts;
using NexTalent.Postulaciones.Application;
using NexTalent.Postulaciones.Domain;
using NexTalent.Postulaciones.Infrastructure.Data;

namespace NexTalent.Postulaciones.Infrastructure.Tests;

/// <summary>
/// Outbox, Inbox y consumidores con SQL Server real y el transporte en memoria de MassTransit.
/// Se omiten sin NEXTALENT_PRUEBAS_SQL. Crean una base temporal y la borran.
/// </summary>
public sealed class MensajeriaTests : IAsyncLifetime
{
    private static readonly string? Servidor = Environment.GetEnvironmentVariable("NEXTALENT_PRUEBAS_SQL");
    private static readonly Guid Empresa = Guid.NewGuid();
    private static readonly Guid Habilidad = Guid.NewGuid();

    private readonly string _base = $"NexTalent_Pruebas_{Guid.NewGuid():N}";
    private ServiceProvider? _proveedor;
    private ITestHarness? _arnes;
    private readonly AvisosRegistrados _avisos = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        if (Servidor is null)
        {
            return;
        }

        var conexion = new SqlConnectionStringBuilder(Servidor) { InitialCatalog = _base }.ConnectionString;
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddSingleton<IAvisosTiempoReal>(_avisos);
        servicios.AgregarPersistencia(conexion);
        servicios.AddMassTransitTestHarness(x =>
        {
            DependencyInjection.Registrar(x);
            // Hace las veces de Notificaciones: comprueba que los eventos realmente salen de la Outbox.
            x.AddConsumer<ReceptorPostulacionCreada>();
            x.AddConsumer<ReceptorEstatusCambiado>();
        });
        _proveedor = servicios.BuildServiceProvider(true);

        await using (var alcance = _proveedor.CreateAsyncScope())
        {
            await alcance.ServiceProvider.GetRequiredService<PostulacionesDbContext>().Database.MigrateAsync(Ct);
        }

        _arnes = _proveedor.GetRequiredService<ITestHarness>();
        await _arnes.Start();

        // En la aplicacion los arranca el host; el arnes solo arranca el bus.
        _segundoPlano = [.. _proveedor.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .Where(s => s.GetType().Name.Contains("Outbox", StringComparison.Ordinal))];
        Assert.NotEmpty(_segundoPlano);
        foreach (var servicio in _segundoPlano)
        {
            await servicio.StartAsync(Ct);
        }
    }

    private Microsoft.Extensions.Hosting.IHostedService[] _segundoPlano = [];

    public async ValueTask DisposeAsync()
    {
        if (_proveedor is null)
        {
            return;
        }

        foreach (var servicio in _segundoPlano)
        {
            await servicio.StopAsync(CancellationToken.None);
        }

        await _arnes!.Stop();
        await using (var alcance = _proveedor.CreateAsyncScope())
        {
            await alcance.ServiceProvider.GetRequiredService<PostulacionesDbContext>().Database.EnsureDeletedAsync();
        }

        await _proveedor.DisposeAsync();
    }

    [Fact]
    public async Task VacantePublicada_CreaLaCopiaLocal_YUnaVersionViejaSeIgnora()
    {
        var vacante = Guid.NewGuid();
        await Publicar(Publicada(vacante, 2, "Nuevo título"));
        await Publicar(Publicada(vacante, 1, "Título viejo"));

        var copia = await Esperar(db => db.Vacantes.AsNoTracking().SingleOrDefaultAsync(v => v.VacanteId == vacante, Ct),
            v => v is not null && v.VersionEvento == 2);
        Assert.Equal(("Nuevo título", true, Empresa), (copia!.Titulo, copia.Abierta, copia.EmpresaId));
        await EnAlcance(async db => Assert.Equal(1, await db.VacanteHabilidades.CountAsync(h => h.VacanteId == vacante, Ct)));
    }

    [Fact]
    public async Task Postular_PublicaPostulacionCreadaDesdeLaOutbox()
    {
        var vacante = await PrepararVacanteAsync();
        var candidato = await PrepararCandidatoAsync();

        var folio = await EnAlcance(async (db, s) =>
            (await s.GetRequiredService<ServicioCandidato>().PostularAsync(Candidato(candidato), vacante, Ct)).Folio);

        await Esperar(db => db.Set<OutboxMessage>().CountAsync(Ct), pendientes => pendientes == 0);
        Assert.True(await _arnes!.Consumed.Any<PostulacionCreada>(m => m.Context.Message.Folio == folio, Ct));
        await EsperarAviso(() => _avisos.Creadas.Any(e => e.Folio == folio && e.CandidatoId == candidato));
    }

    [Fact]
    public async Task VacanteCerrada_CierraPendientesYPublicaEstatus_UnaSolaVezAunqueLlegueRepetido()
    {
        var vacante = await PrepararVacanteAsync();
        var candidato = await PrepararCandidatoAsync();
        await EnAlcance(async (_, s) =>
            await s.GetRequiredService<ServicioCandidato>().PostularAsync(Candidato(candidato), vacante, Ct));

        var mensaje = Guid.NewGuid();
        var cierre = new VacanteCerrada(Guid.NewGuid(), DateTimeOffset.UtcNow, vacante, Empresa, "Puesto",
            CausasCierre.Caducidad, DateTimeOffset.UtcNow, 9);
        await Publicar(cierre, mensaje);
        await Publicar(cierre, mensaje);

        await Esperar(db => db.Postulaciones.AsNoTracking().SingleAsync(p => p.VacanteId == vacante, Ct),
            p => p.EstadoActual == EstadoPostulacion.NoSeleccionado);
        Assert.True(await _arnes!.Consumed.Any<EstatusCambiado>(m => m.Context.Message.VacanteId == vacante, Ct));
        await Task.Delay(1500, Ct);
        Assert.Equal(1, _arnes.Consumed.Select<EstatusCambiado>(Ct).Count(m => m.Context.Message.VacanteId == vacante));
        await EnAlcance(async db => Assert.False((await db.Vacantes.SingleAsync(v => v.VacanteId == vacante, Ct)).Abierta));

        // SignalR: el candidato recibe el cierre automatico una sola vez, aunque el mensaje llego repetido.
        await EsperarAviso(() => _avisos.Estatus.Any(e => e.VacanteId == vacante));
        Assert.Single(_avisos.Estatus, e => e.VacanteId == vacante
            && e.CandidatoId == candidato && e.EstadoNuevo == Estados.NoSeleccionado);
    }

    [Fact]
    public async Task VacanteCerradaAntesDePublicada_QuedaCerradaYRechazaPostulaciones()
    {
        var vacante = Guid.NewGuid();
        await Publicar(new VacanteCerrada(Guid.NewGuid(), DateTimeOffset.UtcNow, vacante, Empresa, "Puesto",
            CausasCierre.Moderacion, DateTimeOffset.UtcNow, 5));
        await Publicar(Publicada(vacante, 4, "Puesto"));
        var candidato = await PrepararCandidatoAsync();

        await Esperar(db => db.Vacantes.AsNoTracking().SingleOrDefaultAsync(v => v.VacanteId == vacante, Ct),
            v => v is { Abierta: false });
        await Task.Delay(1000, Ct);

        await EnAlcance(async (_, s) => await Assert.ThrowsAsync<VacanteNoAbiertaException>(() =>
            s.GetRequiredService<ServicioCandidato>().PostularAsync(Candidato(candidato), vacante, Ct)));
    }

    [Fact]
    public async Task PerfilActualizado_ReemplazaHabilidades()
    {
        var candidato = Guid.NewGuid();
        await Publicar(Perfil(candidato, 1, [new(Habilidad, Niveles.Basico), new(Guid.NewGuid(), Niveles.Basico)]));
        await Publicar(Perfil(candidato, 2, [new(Habilidad, Niveles.Avanzado)]));

        var copia = await Esperar(db => db.Candidatos.AsNoTracking().SingleOrDefaultAsync(c => c.CandidatoId == candidato, Ct),
            c => c?.VersionEvento == 2);
        Assert.True(copia!.TieneContacto);
        await EnAlcance(async db =>
        {
            var habilidad = await db.CandidatoHabilidades.SingleAsync(h => h.CandidatoId == candidato, Ct);
            Assert.Equal(NivelHabilidad.Avanzado, habilidad.Nivel);
        });
    }

    private async Task<Guid> PrepararVacanteAsync()
    {
        var vacante = Guid.NewGuid();
        await Publicar(Publicada(vacante, 1, "Puesto"));
        await Esperar(db => db.Vacantes.AsNoTracking().AnyAsync(v => v.VacanteId == vacante, Ct), hay => hay);
        return vacante;
    }

    private async Task<Guid> PrepararCandidatoAsync()
    {
        var candidato = Guid.NewGuid();
        await Publicar(Perfil(candidato, 1,
            [new(Habilidad, Niveles.Avanzado), new(Guid.NewGuid(), Niveles.Basico), new(Guid.NewGuid(), Niveles.Basico)]));
        await Esperar(db => db.Candidatos.AsNoTracking().AnyAsync(c => c.CandidatoId == candidato, Ct), hay => hay);
        return candidato;
    }

    private static Usuario Candidato(Guid id) => new(id, Usuario.RolCandidato, null);

    private static VacantePublicada Publicada(Guid vacante, long version, string titulo) => new(
        Guid.NewGuid(), DateTimeOffset.UtcNow, vacante, Empresa, "Empresa", titulo, null, 1000m, 2000m,
        TiposSalario.Bruto, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30),
        [new(Habilidad, Niveles.Intermedio, true)], version);

    private static PerfilHabilidadesActualizado Perfil(Guid candidato, long version, HabilidadDePerfil[] habilidades) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, candidato, habilidades, true, true, version);

    private async Task Publicar<T>(T mensaje, Guid? id = null) where T : class
    {
        Assert.SkipWhen(_arnes is null, "Define NEXTALENT_PRUEBAS_SQL para correr las pruebas con SQL Server.");
        var mensajeId = id ?? NewId.NextGuid();
        await _arnes.Bus.Publish(mensaje, c => c.MessageId = mensajeId, Ct);
        Assert.True(await _arnes.Consumed.Any<T>(m => m.Context.MessageId == mensajeId, Ct));
    }

    private static async Task EsperarAviso(Func<bool> listo)
    {
        for (var i = 0; i < 100 && !listo(); i++)
        {
            await Task.Delay(100, Ct);
        }

        Assert.True(listo(), "No llego el aviso en tiempo real.");
    }

    /// <summary>Consulta hasta que se cumpla la condicion o pasen 10 segundos.</summary>
    private async Task<T> Esperar<T>(Func<PostulacionesDbContext, Task<T>> consulta, Func<T, bool> listo)
    {
        T valor = default!;
        for (var i = 0; i < 100; i++)
        {
            valor = await EnAlcance(consulta);
            if (listo(valor))
            {
                return valor;
            }

            await Task.Delay(100, Ct);
        }

        Assert.Fail($"No se cumplio la condicion; ultimo valor: {valor}");
        return valor;
    }

    private Task EnAlcance(Func<PostulacionesDbContext, Task> accion) =>
        EnAlcance<object?>(async db => { await accion(db); return null; });

    private Task<T> EnAlcance<T>(Func<PostulacionesDbContext, Task<T>> accion) =>
        EnAlcance((db, _) => accion(db));

    private async Task<T> EnAlcance<T>(Func<PostulacionesDbContext, IServiceProvider, Task<T>> accion)
    {
        Assert.SkipWhen(_proveedor is null, "Define NEXTALENT_PRUEBAS_SQL para correr las pruebas con SQL Server.");
        await using var alcance = _proveedor.CreateAsyncScope();
        return await accion(alcance.ServiceProvider.GetRequiredService<PostulacionesDbContext>(), alcance.ServiceProvider);
    }
}

public sealed class ReceptorPostulacionCreada : IConsumer<PostulacionCreada>
{
    public Task Consume(ConsumeContext<PostulacionCreada> contexto) => Task.CompletedTask;
}

public sealed class ReceptorEstatusCambiado : IConsumer<EstatusCambiado>
{
    public Task Consume(ConsumeContext<EstatusCambiado> contexto) => Task.CompletedTask;
}

/// <summary>Hace las veces del hub de SignalR: guarda lo que se habria enviado.</summary>
public sealed class AvisosRegistrados : IAvisosTiempoReal
{
    public ConcurrentQueue<EstatusCambiado> Estatus { get; } = new();

    public ConcurrentQueue<PostulacionCreada> Creadas { get; } = new();

    public Task EstatusCambiadoAsync(EstatusCambiado evento, CancellationToken ct)
    {
        Estatus.Enqueue(evento);
        return Task.CompletedTask;
    }

    public Task PostulacionCreadaAsync(PostulacionCreada evento, CancellationToken ct)
    {
        Creadas.Enqueue(evento);
        return Task.CompletedTask;
    }
}
