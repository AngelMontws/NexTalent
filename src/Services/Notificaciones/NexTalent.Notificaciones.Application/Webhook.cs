using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NexTalent.Notificaciones.Application;

/// <summary>Verificacion y firma del webhook de Meta. Los secretos viven en .env.</summary>
public static class FirmaWebhook
{
    private const string Prefijo = "sha256=";

    /// <summary>HMAC-SHA256 del cuerpo sin modificar. Por comprobar con un webhook real.</summary>
    public static string Firmar(byte[] cuerpo, string appSecret) =>
        Prefijo + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), cuerpo));

    public static bool EsValida(byte[] cuerpo, string? firma, string appSecret) =>
        firma is not null && appSecret.Length > 0 && SonIguales(firma, Firmar(cuerpo, appSecret));

    public static bool EsVerificacionValida(string? modo, string? token, string tokenEsperado) =>
        modo == "subscribe" && token is not null && tokenEsperado.Length > 0 && SonIguales(token, tokenEsperado);

    /// <summary>Compara en tiempo constante.</summary>
    private static bool SonIguales(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}

/// <summary>Telefono puede faltar: Meta garantiza solo UsuarioWhatsapp (BSUID).</summary>
public sealed record MensajeEntrante(
    string Id, string? Telefono, string? UsuarioWhatsapp, string? Texto, string? Boton, string? IdAlerta);

public static class LectorWebhook
{
    /// <summary>Solo textos y botones; ignora estados de entrega y otros tipos. Lanza JsonException si no es JSON.</summary>
    public static IReadOnlyList<MensajeEntrante> Leer(byte[] cuerpo)
    {
        using var documento = JsonDocument.Parse(cuerpo);
        var mensajes = new List<MensajeEntrante>();
        foreach (var entrada in Lista(documento.RootElement, "entry"))
        {
            foreach (var cambio in Lista(entrada, "changes"))
            {
                if (Texto(cambio, "field") != "messages" || Hijo(cambio, "value") is not { } valor)
                {
                    continue;
                }

                foreach (var m in Lista(valor, "messages"))
                {
                    if (Texto(m, "id") is not { } id)
                    {
                        continue;
                    }

                    var tipo = Texto(m, "type");
                    var texto = tipo == "text" ? Texto(Hijo(m, "text"), "body") : null;
                    var boton = tipo == "button" ? Texto(Hijo(m, "button"), "text") : null;
                    if (texto is not null || boton is not null)
                    {
                        mensajes.Add(new(id, Texto(m, "from"), Texto(m, "from_user_id"), texto, boton,
                            Texto(Hijo(m, "context"), "id")));
                    }
                }
            }
        }

        return mensajes;
    }

    private static JsonElement? Hijo(JsonElement? e, string nombre) =>
        e is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty(nombre, out var hijo) ? hijo : null;

    private static IEnumerable<JsonElement> Lista(JsonElement e, string nombre) =>
        Hijo(e, nombre) is { ValueKind: JsonValueKind.Array } lista ? lista.EnumerateArray() : [];

    private static string? Texto(JsonElement? e, string nombre) =>
        Hijo(e, nombre) is { ValueKind: JsonValueKind.String } t ? t.GetString() : null;
}
