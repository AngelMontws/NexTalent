using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NexTalent.Identidad.Application.Auth;

namespace NexTalent.Identidad.Infrastructure.Auth;

public sealed class EmisorTokenJwt(IConfiguration configuracion, TimeProvider reloj) : IEmisorToken
{
    private const int MinutosPorDefecto = 15;
    private const int BytesMinimosClave = 32;

    public string Emitir(DatosToken datos)
    {
        var clave = configuracion["Jwt:Key"];
        if (clave is not { Length: >= BytesMinimosClave })
        {
            throw new JwtNoConfiguradoException();
        }

        var minutos = int.TryParse(configuracion["Jwt:ExpiresMinutes"], out var m)
            ? Math.Clamp(m, 1, 1440)
            : MinutosPorDefecto;
        var ahora = reloj.GetUtcNow().UtcDateTime;

        var claims = new Dictionary<string, object>
        {
            ["sub"] = datos.UsuarioId.ToString(),
            ["role"] = datos.Rol,
            ["email"] = datos.Email,
            ["jti"] = Guid.NewGuid().ToString("N")
        };
        if (datos.EmpresaId is { } empresaId)
        {
            claims["empresa_id"] = empresaId.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = configuracion["Jwt:Issuer"] ?? "NexTalent.Identidad",
            Audience = configuracion["Jwt:Audience"] ?? "NexTalent.Api",
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = ahora.AddMinutes(minutos),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}