using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NexTalent.Identidad.Application.Auth;

namespace NexTalent.Identidad.Api.Controllers;

public sealed record SolicitudLogin(
    [Required(AllowEmptyStrings = false), MaxLength(256)] string Email,
    [Required(AllowEmptyStrings = false), MaxLength(256)] string Password);

public sealed record RespuestaLogin(string AccessToken);

/// <summary>Por el gateway: POST /api/auth/login.</summary>
[ApiController]
[Route("auth")]
public sealed class AuthController(IServiceProvider servicios, ILogger<AuthController> log) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(SolicitudLogin solicitud, CancellationToken ct)
    {
        if (servicios.GetService<ServicioLogin>() is not { } servicio)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Base de datos no configurada.");
        }

        try
        {
            return await servicio.LoginAsync(solicitud.Email, solicitud.Password, ct) is { } token
                ? Ok(new RespuestaLogin(token))
                : Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Correo o contraseña incorrectos");
        }
        catch (JwtNoConfiguradoException ex)
        {
            log.LogError(ex, "Login rechazado: configuracion JWT invalida.");
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Autenticacion no disponible: JWT no configurado.");
        }
    }
}