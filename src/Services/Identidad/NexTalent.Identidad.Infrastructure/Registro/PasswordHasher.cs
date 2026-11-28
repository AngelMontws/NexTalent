using System.Security.Cryptography;

namespace NexTalent.Identidad.Infrastructure.Registro;

/// <summary>Formato PBKDF2$100000$&lt;salt b64&gt;$&lt;hash b64&gt;, igual al de IdentidadDbInitializer.</summary>
public static class PasswordHasher
{
    private const int Iteraciones = 100_000;
    private const int BytesSalt = 16;
    private const int BytesHash = 32;

    public static string Hashear(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(BytesSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, BytesHash);
        return $"PBKDF2${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string almacenado)
    {
        var partes = almacenado.Split('$');
        if (partes.Length != 4 || partes[0] != "PBKDF2" || !int.TryParse(partes[1], out var iter))
        {
            return false;
        }

        var salt = Convert.FromBase64String(partes[2]);
        var esperado = Convert.FromBase64String(partes[3]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(password, salt, iter, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}