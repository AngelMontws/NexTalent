using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NexTalent.Postulaciones.Application;
using NexTalent.Postulaciones.Domain;

namespace NexTalent.Postulaciones.Api;

/// <summary>Convierte los errores de negocio en respuestas ProblemDetails con su codigo HTTP.</summary>
public sealed class ManejoErrores(IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken ct)
    {
        var (estado, titulo) = error switch
        {
            AccesoDenegadoException => (StatusCodes.Status403Forbidden, error.Message),
            NoEncontradaException => (StatusCodes.Status404NotFound, error.Message),
            YaPostuladoException or ConflictoConcurrenciaException or TransicionInvalidaException
                or VacanteNoAbiertaException => (StatusCodes.Status409Conflict, error.Message),
            PerfilIncompletoException or MotivoObligatorioException
                => (StatusCodes.Status400BadRequest, error.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Datos no validos."),
            VacanteNoDisponibleException => (StatusCodes.Status503ServiceUnavailable, error.Message),
            _ => (0, "")
        };

        if (estado == 0)
        {
            return false;
        }

        http.Response.StatusCode = estado;
        var detalle = new ProblemDetails { Status = estado, Title = titulo };
        if (error is PerfilIncompletoException incompleto)
        {
            detalle.Extensions["faltantes"] = incompleto.Faltantes;
        }

        return await problemas.TryWriteAsync(new() { HttpContext = http, ProblemDetails = detalle, Exception = error });
    }
}
