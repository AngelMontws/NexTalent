using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexTalent.Postulaciones.Application;
using NexTalent.Postulaciones.Domain;
using NexTalent.Postulaciones.Infrastructure.Data;

namespace NexTalent.Postulaciones.Infrastructure.Tests;

/// <summary>
/// Pruebas contra SQL Server real. Se omiten si no existe la variable NEXTALENT_PRUEBAS_SQL
/// (cadena de conexion al servidor, sin base). Crean una base temporal y la borran al terminar.
/// </summary>
public sealed class PersistenciaTests : IAsyncLifetime
{
    private static readonly string? Servidor = Environment.GetEnvironmentVariable("NEXTALENT_PRUEBAS_SQL");
    private static readonly Guid Empresa = Guid.NewGuid();
    private static readonly Guid Vacante = Guid.NewGuid();
    private static readonly Guid[] Habilidades = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    private readonly string _base = $"NexTalent_Pruebas_{Guid.NewGuid():N}";
    private readonly Usuario _candidato = new(Guid.NewGuid(), Usuario.RolCandidato, null);
    private readonly Usuario _reclutador = new(Guid.NewGuid(), Usuario.RolReclutador, Empresa);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        if (Servidor is null)
        {
            return;
        }

        await using var db = Nuevo();
        await db.Database.MigrateAsync(Ct);
        db.Vacantes.Add(new VacanteReplica
        {
            VacanteId = Vacante, EmpresaId = Empresa, Titulo = "Puesto", NombreEmpresa = "Empresa", Abierta = true
        });
        db.VacanteHabilidades.AddRange(Habilidades.Select((h, i) => new VacanteHabilidadReplica
        {
            VacanteId = Vacante, HabilidadId = h, Nivel = NivelHabilidad.Intermedio, Requerida = i < 2
        }));
        AgregarCandidato(db, _candidato.Id);
        await db.SaveChangesAsync(Ct);
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
    public async Task Postular_GuardaFolioInstantaneaEHistorial()
    {
        await using var db = Nuevo();

        var r = await Candidatos(db).PostularAsync(_candidato, Vacante, Ct);

        await using var otra = Nuevo();
        var guardada = await new PostulacionRepositorio(otra).ObtenerAsync(r.Id, Ct);
        Assert.NotNull(guardada);
        Assert.Matches("^NT-[0-9]{6}$", guardada.Folio);
        Assert.Equal((100m, 1, EstadoPostulacion.Enviada), (guardada.CoincidenciaPct, guardada.DeseablesCumplidas, guardada.EstadoActual));
        var creacion = Assert.Single(guardada.Historial);
        Assert.Equal((null, CausaCambio.Creacion, _candidato.Id), (creacion.EstadoAnterior, creacion.Causa, creacion.CambiadoPor));
    }

    [Fact]
    public async Task Folios_SonDistintosYConsecutivos()
    {
        await using var db = Nuevo();
        var folios = new GeneradorFolio(db);

        var primero = await folios.SiguienteAsync(Ct);
        var segundo = await folios.SiguienteAsync(Ct);

        Assert.Equal(int.Parse(primero[3..]) + 1, int.Parse(segundo[3..]));
    }

    [Fact]
    public async Task MoverYCerrar_GuardanHistorialEnOrden()
    {
        await using var db = Nuevo();
        var r = await Candidatos(db).PostularAsync(_candidato, Vacante, Ct);

        await using (var paso = Nuevo())
        {
            await Reclutadores(paso).AbrirPerfilAsync(_reclutador, r.Id, Ct);
        }

        await using (var paso = Nuevo())
        {
            await Reclutadores(paso).MoverAsync(_reclutador, r.Id, EstadoPostulacion.Entrevista, "Buen perfil", Ct);
        }

        await using (var paso = Nuevo())
        {
            Assert.Equal(1, await new ServicioCierre(
                new PostulacionRepositorio(paso), new Replicas(paso), Unidad(paso), TimeProvider.System)
                .CerrarAsync(Vacante, "Caducidad", Ct));
        }

        await using var lectura = Nuevo();
        var p = (await new PostulacionRepositorio(lectura).ObtenerAsync(r.Id, Ct))!;
        Assert.Equal(EstadoPostulacion.NoSeleccionado, p.EstadoActual);
        Assert.Equal(
            [CausaCambio.Creacion, CausaCambio.PerfilAbierto, CausaCambio.Avance, CausaCambio.VacanteCerrada],
            p.Historial.OrderBy(c => c.Secuencia).Select(c => c.Causa));
        Assert.Equal("Buen perfil", p.Historial.Single(c => c.Secuencia == 3).Motivo);
        Assert.Null(p.Historial.Single(c => c.Secuencia == 4).CambiadoPor);
    }

    [Fact]
    public async Task DosPostulacionesSimultaneas_LaSegundaEsYaPostulado()
    {
        await using var a = Nuevo();
        await using var b = Nuevo();

        // B ya paso la consulta de duplicado cuando A guarda: decide el indice UNIQUE.
        new PostulacionRepositorio(b).Agregar(Postulacion.Crear(
            Guid.NewGuid(), "NT-999999", _candidato.Id, Vacante, new(100m, 2, 2, 1), true, DateTimeOffset.UtcNow));
        await Candidatos(a).PostularAsync(_candidato, Vacante, Ct);

        await Assert.ThrowsAsync<YaPostuladoException>(() => Unidad(b).GuardarAsync(Ct));
    }

    [Fact]
    public async Task DosReclutadoresALaVez_ElSegundoRecibeConflicto()
    {
        await using var db = Nuevo();
        var r = await Candidatos(db).PostularAsync(_candidato, Vacante, Ct);

        await using var a = Nuevo();
        await using var b = Nuevo();
        var pa = (await new PostulacionRepositorio(a).ObtenerAsync(r.Id, Ct))!;
        var pb = (await new PostulacionRepositorio(b).ObtenerAsync(r.Id, Ct))!;
        pa.Mover(EstadoPostulacion.Entrevista, _reclutador.Id, true, null, DateTimeOffset.UtcNow);
        pb.Mover(EstadoPostulacion.NoSeleccionado, _reclutador.Id, true, null, DateTimeOffset.UtcNow);

        await Unidad(a).GuardarAsync(Ct);
        await Assert.ThrowsAsync<ConflictoConcurrenciaException>(() => Unidad(b).GuardarAsync(Ct));

        await using var lectura = Nuevo();
        var p = (await new PostulacionRepositorio(lectura).ObtenerAsync(r.Id, Ct))!;
        Assert.Equal(EstadoPostulacion.Entrevista, p.EstadoActual);
        Assert.Equal(2, p.Historial.Count);
    }

    [Fact]
    public async Task EstadoInvalido_LoRechazaLaBase()
    {
        await using var db = Nuevo();
        var r = await Candidatos(db).PostularAsync(_candidato, Vacante, Ct);

        var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlAsync(
            $"UPDATE postulaciones.postulacion SET estado_actual = 'Inventado' WHERE id = {r.Id}", Ct));
        Assert.Contains("ck_postulacion_estado_actual", error.Message);
    }

    [Fact]
    public async Task ListarCandidatos_LeeReplicasYOrdena()
    {
        var segundo = new Usuario(Guid.NewGuid(), Usuario.RolCandidato, null);
        await using (var db = Nuevo())
        {
            AgregarCandidato(db, segundo.Id, cumpleRequeridas: 1);
            await db.SaveChangesAsync(Ct);
            await Candidatos(db).PostularAsync(segundo, Vacante, Ct);
        }

        await using (var db = Nuevo())
        {
            await Candidatos(db).PostularAsync(_candidato, Vacante, Ct);
        }

        await using var lectura = Nuevo();
        var lista = await Reclutadores(lectura).ListarCandidatosAsync(_reclutador, Vacante, Ct);

        Assert.Equal([_candidato.Id, segundo.Id], lista.Select(c => c.CandidatoId));
        Assert.Equal([100m, 50m], lista.Select(c => c.CoincidenciaPct));
    }

    private PostulacionesDbContext Nuevo()
    {
        Assert.SkipWhen(Servidor is null, "Define NEXTALENT_PRUEBAS_SQL para correr las pruebas con SQL Server.");
        var conexion = new SqlConnectionStringBuilder(Servidor) { InitialCatalog = _base }.ConnectionString;
        var opciones = new DbContextOptionsBuilder<PostulacionesDbContext>();
        DependencyInjection.Configurar(opciones, conexion);
        return new PostulacionesDbContext(opciones.Options);
    }

    private static void AgregarCandidato(PostulacionesDbContext db, Guid id, int cumpleRequeridas = 2)
    {
        db.Candidatos.Add(new CandidatoReplica { CandidatoId = id, TieneNombre = true, TieneContacto = true });
        db.CandidatoHabilidades.AddRange(
            Habilidades.Take(cumpleRequeridas).Append(Habilidades[2]).Append(Guid.NewGuid())
                .Select(h => new CandidatoHabilidadReplica { CandidatoId = id, HabilidadId = h, Nivel = NivelHabilidad.Avanzado }));
    }

    private static UnidadDeTrabajo Unidad(PostulacionesDbContext db) => new(db, NullLogger<UnidadDeTrabajo>.Instance);

    private static ServicioCandidato Candidatos(PostulacionesDbContext db) => new(
        new PostulacionRepositorio(db), new Replicas(db), new GeneradorFolio(db), Unidad(db), TimeProvider.System);

    private static ServicioReclutador Reclutadores(PostulacionesDbContext db) => new(
        new PostulacionRepositorio(db), new Replicas(db), Unidad(db), TimeProvider.System);
}
