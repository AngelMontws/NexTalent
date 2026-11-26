using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NexTalent.Postulaciones.Application;

namespace NexTalent.Postulaciones.Api;

/// <summary>
/// JWT de Identidad. PROVISIONAL hasta que Backend 1 confirme algoritmo y nombres: HS256 con
/// Jwt:Issuer, Jwt:Audience y Jwt:Key. Sin clave, todo token se rechaza.
/// </summary>
public static class Seguridad
{
    public const int BytesMinimosClave = 32;

    public static void AgregarJwt(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var clave = configuracion["Jwt:Key"];
        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            // Conserva los nombres sub, role y empresa_id tal como llegan.
            o.MapInboundClaims = false;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = configuracion["Jwt:Issuer"],
                ValidAudience = configuracion["Jwt:Audience"],
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "sub",
                RoleClaimType = "role",
                IssuerSigningKey = clave is { Length: >= BytesMinimosClave }
                    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave))
                    : null
            };
        });
        servicios.AddAuthorization();
    }

    /// <summary>Null si el token no trae un sub valido.</summary>
    public static Usuario? Usuario(ClaimsPrincipal principal)
    {
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var id))
        {
            return null;
        }

        Guid? empresa = Guid.TryParse(principal.FindFirstValue("empresa_id"), out var e) ? e : null;
        return new Usuario(id, principal.FindFirstValue("role") ?? "", empresa);
    }

    /// <summary>X-Internal-Api-Key de Notificaciones, comparada en tiempo constante.</summary>
    public static bool ClaveInternaValida(string? recibida, string? esperada) =>
        recibida is not null && esperada is { Length: >= BytesMinimosClave }
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recibida), Encoding.UTF8.GetBytes(esperada));
}
