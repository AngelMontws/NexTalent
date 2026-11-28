using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexTalent.Postulaciones.Application;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Api.Controllers;

public sealed record SolicitudPostular(Guid VacanteId);

/// <summary>Motivo: privado, queda en el historial. MensajeCandidato: lo recibe el candidato por correo (HU-06.03).</summary>
public sealed record SolicitudMover(EstadoPostulacion Estado, string? Motivo, string? MensajeCandidato = null);

/// <summary>
/// Por el gateway: /api/postulaciones/**. Los permisos por rol, propiedad y empresa los revisa Application.
/// </summary>
[ApiController]
[Authorize]
[Route("postulaciones")]
public sealed class PostulacionesController(IServiceProvider servicios) : ControllerBase
{
    /// <summary>HU-05.01. 201 con folio; 409 si ya se postulo o la vacante esta cerrada; 400 con faltantes.</summary>
    [HttpPost]
    public Task<IActionResult> Postular(SolicitudPostular solicitud, CancellationToken ct) =>
        Con<ServicioCandidato>(async (s, u) =>
        {
            var r = await s.PostularAsync(u, solicitud.VacanteId, ct);
            return CreatedAtAction(nameof(Detalle), new { id = r.Id }, r);
        });

    /// <summary>HU-05.02. activas=true solo activas, false solo cerradas, sin valor todas.</summary>
    [HttpGet("mias")]
    public Task<IActionResult> Mias([FromQuery] bool? activas, CancellationToken ct) =>
        Con<ServicioCandidato>(async (s, u) => Ok(await s.MisPostulacionesAsync(u, activas, ct)));

    /// <summary>HU-05.04. Detalle con historial, solo de la propia persona candidata.</summary>
    [HttpGet("{id:guid}")]
    public Task<IActionResult> Detalle(Guid id, CancellationToken ct) =>
        Con<ServicioCandidato>(async (s, u) => Ok(await s.DetalleAsync(u, id, ct)));

    /// <summary>HU-06.01. Candidatos de una vacante de su empresa, ordenados por coincidencia.</summary>
    [HttpGet("vacantes/{vacanteId:guid}/candidatos")]
    public Task<IActionResult> Candidatos(Guid vacanteId, CancellationToken ct) =>
        Con<ServicioReclutador>(async (s, u) => Ok(await s.ListarCandidatosAsync(u, vacanteId, ct)));

    /// <summary>HU-06.01. Abrir el perfil pasa Enviada a Vista.</summary>
    [HttpPost("{id:guid}/abrir-perfil")]
    public Task<IActionResult> AbrirPerfil(Guid id, CancellationToken ct) =>
        Con<ServicioReclutador>(async (s, u) => Ok(await s.AbrirPerfilAsync(u, id, ct)));

    /// <summary>HU-06.02. Mover de etapa; reactivar desde NoSeleccionado exige motivo.</summary>
    [HttpPatch("{id:guid}/estado")]
    public Task<IActionResult> Mover(Guid id, SolicitudMover solicitud, CancellationToken ct) =>
        Con<ServicioReclutador>(async (s, u) =>
            Ok(new { estado = await s.MoverAsync(u, id, solicitud.Estado, solicitud.Motivo, ct, solicitud.MensajeCandidato) }));

    /// <summary>503 si la base de datos no esta configurada.</summary>
    private async Task<IActionResult> Con<TServicio>(Func<TServicio, Usuario, Task<IActionResult>> accion)
        where TServicio : notnull
    {
        if (Seguridad.Usuario(User) is not { } usuario)
        {
            return Unauthorized();
        }

        return servicios.GetService<TServicio>() is { } servicio
            ? await accion(servicio, usuario)
            : Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Base de datos no configurada.");
    }
}
