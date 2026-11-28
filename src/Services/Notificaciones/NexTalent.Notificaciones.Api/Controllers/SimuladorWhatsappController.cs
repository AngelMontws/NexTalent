using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexTalent.Notificaciones.Infrastructure;
using NexTalent.Notificaciones.Infrastructure.Data;

namespace NexTalent.Notificaciones.Api.Controllers;

/// <summary>
/// SOLO DESARROLLO: lo que el simulador "envió" por WhatsApp a los números de la persona que inició sesión
/// (el de su vinculación en curso y el de su contacto). Sirve para leer el código de 6 dígitos en la demo.
/// En cualquier otro ambiente responde 404, porque el simulador guarda los códigos en claro.
/// </summary>
[ApiController]
[Authorize]
[Route("whatsapp/simulador")]
public sealed class SimuladorWhatsappController(IServiceProvider servicios, IHostEnvironment ambiente) : ControllerBase
{
    private const int Maximo = 20;

    [HttpGet]
    public async Task<IActionResult> Mios(CancellationToken ct)
    {
        if (!ambiente.IsDevelopment()
            || servicios.GetService<SimuladorWhatsapp>() is not { } simulador
            || servicios.GetService<NotificacionesDbContext>() is not { } db)
        {
            return NotFound();
        }

        if (!Guid.TryParse(User.FindFirstValue("sub"), out var usuario))
        {
            return Unauthorized();
        }

        var telefonos = (await db.CodigosWhatsapp.AsNoTracking().Where(x => x.UsuarioId == usuario).Select(x => x.Telefono).ToListAsync(ct))
            .Concat(await db.Contactos.AsNoTracking().Where(x => x.UsuarioId == usuario).Select(x => x.TelefonoWhatsapp).ToListAsync(ct))
            .OfType<string>()
            .ToHashSet();

        return Ok(simulador.Enviados.Where(m => telefonos.Contains(m.Telefono)).Reverse().Take(Maximo));
    }
}
