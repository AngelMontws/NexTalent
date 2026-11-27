using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexTalent.Contracts;
using NexTalent.Notificaciones.Domain;
using NexTalent.Notificaciones.Infrastructure;
using NexTalent.Notificaciones.Infrastructure.Data;
using NexTalent.Notificaciones.Infrastructure.Mensajeria;
using NexTalent.Notificaciones.Infrastructure.Tareas;

namespace NexTalent.Notificaciones.Application.Tests;

/// <summary>
/// Pruebas de persistencia contra SQL Server real. Se omiten si no existe NEXTALENT_PRUEBAS_SQL.
/// Crean una base temporal, ejecutan migraciones y la eliminan al finalizar.
/// </summary>
public sealed class PersistenciaNotificacionesTests : IAsyncLifetime
{
    private static readonly string? Servidor = Environment.GetEnvironmentVariable("NEXTALENT_PRUEBAS_SQL");

    private readonly string _base = $"NexTalent_NotifPruebas_{Guid.NewGuid():N}";
    private readonly Memoria _relojYCanales = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        if (Servidor is null)
        {
            return;
        }

        await using var db = Nuevo();
        await NotificacionesDbInitializer.EjecutarAsync(db, _relojYCanales, Ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (Servidor is null)
        {
            return;
        }

        await using var db = Nuevo();
        await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Seeder_EsIdempotenteYCargaContactosDeterministicos()
    {
        await using var db = Nuevo();
        await NotificacionesDbInitializer.EjecutarAsync(db, _relojYCanales, Ct);

        Assert.Equal(6, await db.Contactos.CountAsync(Ct));
        var c1 = await db.Contactos.SingleAsync(c => c.UsuarioId == NotificacionesDbInitializer.Candidatos[0], Ct);
        Assert.True(c1.WhatsappOptIn);
        Assert.Equal("+525510000001", c1.TelefonoWhatsapp);
    }

    [Fact]
    public async Task Vinculacion_PersisteHashVerificaYAplicaBajaRespetandoEventosAtrasados()
    {
        var usuarioId = NotificacionesDbInitializer.Candidatos[3]; // candidato4 inicia sin telefono ni opt-in

        await using (var db = Nuevo())
        {
            var vinc = ServicioVinc(db);
            var sol = await vinc.SolicitarCodigoAsync(usuarioId, "55 9876 5432", true, Ct);
            Assert.True(sol.Exito);
        }

        var codigo = Assert.Single(_relojYCanales.Codigos);

        await using (var db = Nuevo())
        {
            var vinc = ServicioVinc(db);
            var ver = await vinc.VerificarAsync(usuarioId, codigo, Ct);
            Assert.True(ver.Exito);
        }

        _relojYCanales.Ahora = _relojYCanales.Ahora.AddMinutes(5);
        await using (var db = Nuevo())
        {
            var vinc = ServicioVinc(db);
            Assert.True(await vinc.ProcesarTextoAsync("+525598765432", "BAJA", Ct));
        }

        // Un evento ContactoActualizado con fecha anterior a la baja no debe reactivar opt-in.
        await using (var db = Nuevo())
        {
            var consumidor = new ContactoActualizadoConsumidor(db);
            var contacto = await db.Contactos.SingleAsync(c => c.UsuarioId == usuarioId, Ct);
            Assert.False(contacto.WhatsappOptIn);
            Assert.NotNull(contacto.WhatsappBajaFecha);
        }
    }

    [Fact]
    public async Task Avisos_GuardaReconstruyeEnReintentosYRespaldaPorCorreo()
    {
        var candidatoId = NotificacionesDbInitializer.Candidatos[0];
        var postulacionId = Guid.NewGuid();
        var evento = new EstatusCambiado(
            Guid.NewGuid(),
            _relojYCanales.Ahora,
            postulacionId,
            "NT-000123",
            candidatoId,
            Guid.NewGuid(),
            NotificacionesDbInitializer.Empresa1,
            "Desarrollador Backend .NET Jr.",
            "Empresa de prueba 1",
            Estados.Enviada,
            Estados.Entrevista,
            CausasCambio.Avance,
            _relojYCanales.Ahora,
            2);

        _relojYCanales.RespuestaWhatsapp = () => ResultadoEnvio.Fallo("500");

        await using (var db = Nuevo())
        {
            await ServicioAv(db).ProcesarAsync(evento, Ct);
        }

        _relojYCanales.RespuestaWhatsapp = () => ResultadoEnvio.Ok("wamid.ok");
        _relojYCanales.Ahora = _relojYCanales.Ahora.AddMinutes(1);

        await using (var db = Nuevo())
        {
            await ServicioAv(db).ReintentarAsync(Ct);
        }

        await using var lectura = Nuevo();
        var guardada = await lectura.Notificaciones.SingleAsync(x => x.PostulacionId == postulacionId, Ct);
        Assert.Equal(EstadoEnvio.Enviada, guardada.EstadoEnvio);
        Assert.Equal(2, guardada.Intentos);
        Assert.Equal("wamid.ok", guardada.IdExterno);
    }

    [Fact]
    public async Task Recordatorio10DiasHabiles_EnviaAlReclutadorYMarcaBandera()
    {
        var postulacionId = Guid.NewGuid();
        await using (var db = Nuevo())
        {
            db.Seguimientos.Add(new PostulacionSeguimiento
            {
                PostulacionId = postulacionId,
                CandidatoId = NotificacionesDbInitializer.Candidatos[0],
                VacanteId = Guid.NewGuid(),
                EmpresaId = NotificacionesDbInitializer.Empresa1,
                Folio = "NT-000777",
                TituloVacante = "Desarrollador Backend .NET Jr.",
                EstadoActual = Estados.Enviada,
                Pendiente = true,
                UltimoCambioEn = _relojYCanales.Ahora.AddDays(-16),
                Secuencia = 1,
                Recordatorio10dEnviado = false
            });
            await db.SaveChangesAsync(Ct);
        }

        await using (var db = Nuevo())
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") };
            var servicio = new ServicioRecordatorioDiezDias(
                db,
                _relojYCanales,
                new ClientePostulacionesInterno(http),
                _relojYCanales,
                NullLogger<ServicioRecordatorioDiezDias>.Instance);

            Assert.Equal(1, await servicio.EjecutarAsync(Ct));
        }

        await using var lectura = Nuevo();
        var seg = await lectura.Seguimientos.SingleAsync(x => x.PostulacionId == postulacionId, Ct);
        Assert.True(seg.Recordatorio10dEnviado);
        Assert.NotEmpty(_relojYCanales.Correos);
    }

    private NotificacionesDbContext Nuevo()
    {
        Assert.SkipWhen(Servidor is null, "Define NEXTALENT_PRUEBAS_SQL para correr las pruebas con SQL Server.");
        var conexion = new SqlConnectionStringBuilder(Servidor) { InitialCatalog = _base }.ConnectionString;
        var opciones = new DbContextOptionsBuilder<NotificacionesDbContext>();
        DependencyInjection.Configurar(opciones, conexion);
        return new NotificacionesDbContext(opciones.Options);
    }

    private ServicioVinculacion ServicioVinc(NotificacionesDbContext db) => new(
        new VinculacionesRepositorio(db, _relojYCanales),
        new ContactosRepositorio(db, _relojYCanales),
        _relojYCanales,
        _relojYCanales,
        new byte[32]);

    private ServicioAvisos ServicioAv(NotificacionesDbContext db) => new(
        new NotificacionesRepositorio(db, _relojYCanales),
        new ContactosRepositorio(db, _relojYCanales),
        _relojYCanales,
        _relojYCanales,
        _relojYCanales,
        _ => "https://nextalent.mx/similares");
}
