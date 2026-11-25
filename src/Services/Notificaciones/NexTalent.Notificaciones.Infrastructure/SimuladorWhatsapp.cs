using System.Collections.Concurrent;
using System.Text.Json;
using NexTalent.Notificaciones.Application;

namespace NexTalent.Notificaciones.Infrastructure;

public sealed record MensajeSimulado(string Id, string Telefono, string Plantilla, string Texto);

/// <summary>
/// Plan B para la demo: guarda en memoria lo que se enviaria y arma webhooks firmados como los de Meta.
/// Solo debe registrarse en Development: guarda codigos en claro y no prueba la entrega real (RNF-03).
/// </summary>
public sealed class SimuladorWhatsapp : IWhatsapp
{
    private const int Maximo = 100;
    private readonly ConcurrentQueue<MensajeSimulado> _enviados = new();

    public IReadOnlyCollection<MensajeSimulado> Enviados => [.. _enviados];

    public Task<ResultadoEnvio> EnviarCodigoAsync(string telefono, string codigo, CancellationToken ct) =>
        GuardarAsync(telefono, "codigo", codigo);

    public Task<ResultadoEnvio> EnviarPlantillaAsync(string telefono, string plantilla, string texto, CancellationToken ct) =>
        GuardarAsync(telefono, plantilla, texto);

    public Task<ResultadoEnvio> EnviarTextoAsync(string telefono, string texto, CancellationToken ct) =>
        GuardarAsync(telefono, "texto", texto);

    public static byte[] WebhookTexto(string telefono, string texto) =>
        Webhook(new { from = telefono, id = NuevoId(), type = "text", text = new { body = texto } });

    public static byte[] WebhookBoton(string telefono, string idAlerta, string boton) =>
        Webhook(new
        {
            context = new { id = idAlerta },
            from = telefono,
            id = NuevoId(),
            type = "button",
            button = new { payload = boton, text = boton }
        });

    private static byte[] Webhook(object mensaje) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        @object = "whatsapp_business_account",
        entry = new[]
        {
            new { id = "simulador", changes = new[] { new { field = "messages", value = new { messages = new[] { mensaje } } } } }
        }
    });

    private Task<ResultadoEnvio> GuardarAsync(string telefono, string plantilla, string texto)
    {
        var mensaje = new MensajeSimulado(NuevoId(), telefono, plantilla, texto);
        _enviados.Enqueue(mensaje);
        while (_enviados.Count > Maximo && _enviados.TryDequeue(out _))
        {
        }

        return Task.FromResult(ResultadoEnvio.Ok(mensaje.Id));
    }

    private static string NuevoId() => $"sim.{Guid.NewGuid():N}";
}
