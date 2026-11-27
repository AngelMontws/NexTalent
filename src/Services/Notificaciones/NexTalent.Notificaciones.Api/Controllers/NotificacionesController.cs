using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexTalent.Notificaciones.Domain;
using NexTalent.Notificaciones.Infrastructure.Data;
using NexTalent.Notificaciones.Infrastructure.Mensajeria;

namespace NexTalent.Notificaciones.Api.Controllers;

/// <summary>Lo que muestra la bandeja. Sin datos personales del candidato: solo vacante y folio.</summary>
public sealed record AvisoWeb(
    Guid Id,
    string Tipo,
    Guid? PostulacionId,
    Guid? VacanteId,
    string? Folio,
    string? TituloVacante,
    string? NombreEmpresa,
    DateTimeOffset CreadaEn,
    DateTimeOffset? LeidaEn);

/// <summary>
/// NT-041: bandeja web de la persona que inició sesión. Por el gateway: /api/notificaciones/**.
/// Cada quien ve y marca solo sus propios avisos; uno ajeno responde 404 para no revelar que existe.
/// </summary>
[ApiController]
[Authorize]
[Route("notificaciones")]
public sealed class NotificacionesController(IServiceProvider servicios) : ControllerBase
{
    private const int Maximo = 50;

    /// <summary>Los 50 avisos más recientes; con soloNoLeidas=true, solo los pendientes.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool soloNoLeidas, CancellationToken ct)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        if (servicios.GetService<NotificacionesDbContext>() is not { } db) return SinBase();

        var avisos = await Propios(db, usuario)
            .Where(x => !soloNoLeidas || x.LeidaEn == null)
            .OrderByDescending(x => x.CreadaEn)
            .Take(Maximo)
            .Select(x => new AvisoWeb(
                x.Id,
                x.Plantilla == PostulacionCreadaConsumidor.PlantillaWeb ? "NuevaPostulacion" : x.Plantilla,
                x.PostulacionId, x.VacanteId, x.ReferenciaFolio, x.TituloVacante, x.NombreEmpresa,
                x.CreadaEn, x.LeidaEn))
            .ToListAsync(ct);
        return Ok(avisos);
    }

    /// <summary>Marca un aviso como leído. Repetirlo no cambia la fecha original.</summary>
    [HttpPatch("{id:guid}/leida")]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken ct)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        if (servicios.GetService<NotificacionesDbContext>() is not { } db) return SinBase();

        var aviso = await Propios(db, usuario).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (aviso is null) return NotFound();

        aviso.LeidaEn ??= Ahora();
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Marca como leídos todos los avisos pendientes.</summary>
    [HttpPost("leidas")]
    public async Task<IActionResult> MarcarTodas(CancellationToken ct)
    {
        if (Usuario() is not { } usuario) return Unauthorized();
        if (servicios.GetService<NotificacionesDbContext>() is not { } db) return SinBase();

        var ahora = Ahora();
        await Propios(db, usuario).Where(x => x.LeidaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeidaEn, ahora), ct);
        return NoContent();
    }

    private DateTimeOffset Ahora() => (servicios.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow();

    private static IQueryable<NotificacionFila> Propios(NotificacionesDbContext db, Guid usuario) =>
        db.Notificaciones.Where(x => x.UsuarioId == usuario && x.Canal == Canal.Web);

    private Guid? Usuario() =>
        Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        && id != Guid.Empty ? id : null;

    private ObjectResult SinBase() =>
        Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Base de datos no configurada.");
}
