namespace NexTalent.Identidad.Application.Auth;

public static class RolesToken
{
    public const string Candidato = "Candidato";
    public const string Reclutador = "Reclutador";
    public const string Administrador = "Administrador";

    public static string? Normalizar(string? rol) => rol?.Trim().ToLowerInvariant() switch
    {
        "candidato" => Candidato,
        "reclutador" => Reclutador,
        "administrador" or "admin" => Administrador,
        _ => null
    };
}

public sealed record UsuarioAutenticable(Guid Id, string Email, string PasswordHash, string Rol, Guid? EmpresaId);

public sealed record DatosToken(Guid UsuarioId, string Email, string Rol, Guid? EmpresaId);

public interface IUsuariosAutenticacion
{
    Task<UsuarioAutenticable?> BuscarPorEmailAsync(string email, CancellationToken ct);
}

public interface IVerificadorContrasena
{
    bool Verificar(string contrasena, string? hashAlmacenado);
}

public interface IEmisorToken
{
    /// <exception cref="JwtNoConfiguradoException">Jwt:Key ausente o menor a 32 caracteres.</exception>
    string Emitir(DatosToken datos);
}

public sealed class JwtNoConfiguradoException()
    : Exception("Jwt:Key no esta configurada o tiene menos de 32 caracteres.");