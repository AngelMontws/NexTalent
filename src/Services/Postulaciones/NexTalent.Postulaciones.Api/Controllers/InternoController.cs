using Microsoft.AspNetCore.Mvc;
using NexTalent.Postulaciones.Application;

namespace NexTalent.Postulaciones.Api.Controllers;

public sealed record SolicitudInterna(Guid CandidatoId, Guid VacanteId);

/// <summary>
/// Solo para Notificaciones (HU-07.03). El gateway no enruta /interno; ademas exige X-Internal-Api-Key.
/// </summary>
[ApiController]
[Route("interno/postulaciones")]
public sealed class InternoController(IConfiguration configuracion, IServiceProvider servicios) : ControllerBase
{
    public const string Cabecera = "X-Internal-Api-Key";

    [HttpPost]
    public async Task<IActionResult> Postular(SolicitudInterna solicitud, CancellationToken ct)
    {
        var esperada = configuracion["Interno:ApiKey"];
        if (esperada is not { Length: >= Seguridad.BytesMinimosClave })
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Clave interna no configurada.");
        }

        if (!Seguridad.ClaveInternaValida(Request.Headers[Cabecera], esperada))
        {
            return Unauthorized();
        }

        if (servicios.GetService<ServicioCandidato>() is not { } servicio)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Base de datos no configurada.");
        }

        return Ok(await servicio.PostularDesdeWhatsappAsync(solicitud.CandidatoId, solicitud.VacanteId, ct));
    }
}
