using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexTalent.Notificaciones.Application;

namespace NexTalent.Notificaciones.Api.Controllers;

public sealed record SolicitudVincularWhatsapp(string Numero, bool AceptaAvisos);
public sealed record SolicitudVerificarWhatsapp(string Codigo);

/// <summary>
/// HU-07.01: vinculacion opt-in de numero telefonico con codigo OTP de 6 digitos. Requiere JWT.
/// </summary>
[ApiController]
[Authorize]
[Route("whatsapp")]
public sealed class WhatsappVinculacionController(IServiceProvider servicios) : ControllerBase
{
    [HttpPost("vincular")]
    public async Task<IActionResult> Vincular(SolicitudVincularWhatsapp solicitud, CancellationToken ct)
    {
        if (ObtenerUsuarioId(User) is not { } usuarioId)
        {
            return Unauthorized();
        }

        if (servicios.GetService<ServicioVinculacion>() is not { } servicio)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Base de datos no configurada.");
        }

        var respuesta = await servicio.SolicitarCodigoAsync(usuarioId, solicitud.Numero, solicitud.AceptaAvisos, ct);
        return respuesta.Exito ? Ok(respuesta) : BadRequest(respuesta);
    }

    [HttpPost("verificar")]
    public async Task<IActionResult> Verificar(SolicitudVerificarWhatsapp solicitud, CancellationToken ct)
    {
        if (ObtenerUsuarioId(User) is not { } usuarioId)
        {
            return Unauthorized();
        }

        if (servicios.GetService<ServicioVinculacion>() is not { } servicio)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Base de datos no configurada.");
        }

        var respuesta = await servicio.VerificarAsync(usuarioId, solicitud.Codigo, ct);
        return respuesta.Exito ? Ok(respuesta) : BadRequest(respuesta);
    }

    private static Guid? ObtenerUsuarioId(ClaimsPrincipal usuario)
    {
        var texto = usuario.FindFirstValue("sub") ?? usuario.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(texto, out var id) && id != Guid.Empty ? id : null;
    }
}
