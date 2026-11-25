using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NexTalent.Notificaciones.Application;

namespace NexTalent.Notificaciones.Api.Controllers;

/// <summary>
/// Webhook de Meta. No lleva JWT: lo protege la firma. Sin secretos configurados responde 503.
/// </summary>
[ApiController]
[Route("whatsapp/webhook")]
public sealed class WhatsappWebhookController(
    IConfiguration configuracion, IServiceProvider servicios, ILogger<WhatsappWebhookController> log) : ControllerBase
{
    private const int MaximoBytes = 3 * 1024 * 1024;

    [HttpGet]
    public IActionResult Verificar(
        [FromQuery(Name = "hub.mode")] string? modo,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? reto)
    {
        if (configuracion["Whatsapp:VerifyToken"] is not { Length: > 0 } esperado)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return FirmaWebhook.EsVerificacionValida(modo, token, esperado)
            ? Content(reto ?? "", "text/plain")
            : StatusCode(StatusCodes.Status403Forbidden);
    }

    [HttpPost]
    [RequestSizeLimit(MaximoBytes)]
    public async Task<IActionResult> Recibir(CancellationToken ct)
    {
        if (configuracion["Whatsapp:AppSecret"] is not { Length: > 0 } secreto)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        // La firma se calcula sobre los bytes originales, antes de interpretar el JSON.
        using var memoria = new MemoryStream();
        await Request.Body.CopyToAsync(memoria, ct);
        var cuerpo = memoria.ToArray();
        if (!FirmaWebhook.EsValida(cuerpo, Request.Headers["X-Hub-Signature-256"], secreto))
        {
            return Unauthorized();
        }

        IReadOnlyList<MensajeEntrante> mensajes;
        try
        {
            mensajes = LectorWebhook.Leer(cuerpo);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (mensajes.Count == 0)
        {
            return Ok();
        }

        // Se registra al conectar la base de datos; mientras tanto Meta debe reintentar.
        if (servicios.GetService<ServicioWebhook>() is not { } servicio)
        {
            log.LogWarning("Webhook recibido sin ServicioWebhook registrado.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        await servicio.ProcesarAsync(mensajes, ct);
        return Ok();
    }
}
