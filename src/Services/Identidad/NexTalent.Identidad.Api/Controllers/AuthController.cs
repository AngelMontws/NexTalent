using Microsoft.AspNetCore.Mvc;
using NexTalent.Identidad.Infrastructure.Registro;

namespace NexTalent.Identidad.Api.Controllers;

/// <summary>Por el gateway: /api/auth/**.</summary>
[ApiController]
[Route("auth")]
public sealed class AuthController(IServiceProvider servicios) : ControllerBase
{
    [HttpPost("registro")]
    [ProducesResponseType(typeof(UsuarioRegistrado), StatusCodes.Status201Created)]
    public async Task<IActionResult> Registro(SolicitudRegistro solicitud, CancellationToken ct)
    {
        if (servicios.GetService<ServicioRegistro>() is not { } servicio)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Base de datos no configurada.");
        }

        var r = await servicio.RegistrarAsync(solicitud, ct);
        return r.Estado switch
        {
            EstadoRegistro.Creado => Created($"/api/auth/usuarios/{r.Usuario!.Id}", r.Usuario),
            EstadoRegistro.EmailDuplicado => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Este correo ya tiene una cuenta."
            }),
            EstadoRegistro.EmpresaNoEncontrada => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "La empresa indicada no existe."
            }),
            _ => ValidationProblem(new ValidationProblemDetails(r.Errores!))
        };
    }
}