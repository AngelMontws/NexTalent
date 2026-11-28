using System.Security.Cryptography;
using NexTalent.Identidad.Application.Auth;

namespace NexTalent.Identidad.Infrastructure.Auth;

/// <summary>Formato: PBKDF2$iteraciones$saltBase64$hashBase64 (SHA256).</summary>
public sealed class VerificadorContrasenaPbkdf2 : IVerificadorContrasena
{
    private const string Prefijo = "PBKDF2";
    private const int MaximoIteraciones = 1_000_000;

    public bool Verificar(string contrasena, string? hashAlmacenado)
    {
        var partes = hashAlmacenado?.Split('$');
        if (partes is not { Length: 4 }
            || partes[0] != Prefijo
            || !int.TryParse(partes[1], out var iteraciones)
            || iteraciones is < 1 or > MaximoIteraciones)
        {
            return false;
        }

        byte[] sal;
        byte[] esperado;
        try
        {
            sal = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (sal.Length == 0 || esperado.Length == 0)
        {
            return false;
        }

        var calculado = Rfc2898DeriveBytes.Pbkdf2(
            contrasena, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}