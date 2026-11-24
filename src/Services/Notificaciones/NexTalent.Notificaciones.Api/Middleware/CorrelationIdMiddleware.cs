namespace NexTalent.Notificaciones.Api.Middleware;

/// <summary>
/// Asigna a cada peticion un id de correlacion: usa el que llega en el encabezado
/// X-Correlation-ID (si es seguro) o genera uno nuevo. El id se devuelve en la respuesta
/// y se agrega a todos los logs de la peticion.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private const int LongitudMaxima = 64;

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate siguiente, ILogger<CorrelationIdMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ObtenerOCrearId(context);

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
        {
            await _siguiente(context);

            _logger.LogInformation(
                "{Metodo} {Ruta} respondio {Estado}",
                context.Request.Method,
                context.Request.Path.ToUriComponent(),
                context.Response.StatusCode);
        }
    }

    private static string ObtenerOCrearId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var valores))
        {
            var recibido = valores.ToString();
            if (EsValido(recibido))
            {
                return recibido;
            }
        }

        return Guid.NewGuid().ToString("N");
    }

    // Solo letras, numeros, guion y guion bajo, maximo 64 caracteres.
    // Evita que un cliente inyecte saltos de linea u otro texto raro en los logs.
    private static bool EsValido(string valor) =>
        valor.Length is > 0 and <= LongitudMaxima
        && valor.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}