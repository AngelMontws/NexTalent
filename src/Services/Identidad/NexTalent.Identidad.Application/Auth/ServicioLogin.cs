namespace NexTalent.Identidad.Application.Auth;

public sealed class ServicioLogin(
    IUsuariosAutenticacion usuarios,
    IVerificadorContrasena verificador,
    IEmisorToken emisor)
{
    // Hash valido pero inexistente: iguala el tiempo de respuesta cuando el correo no existe.
    private static readonly string HashFicticio =
        $"PBKDF2$100000${Convert.ToBase64String(new byte[16])}${Convert.ToBase64String(new byte[32])}";

    /// <returns>El JWT, o null si las credenciales no son validas.</returns>
    public async Task<string?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(email.Trim(), ct);
        var coincide = verificador.Verificar(password, usuario?.PasswordHash ?? HashFicticio);

        if (usuario is null || !coincide || RolesToken.Normalizar(usuario.Rol) is not { } rol)
        {
            return null;
        }

        Guid? empresaId = rol == RolesToken.Reclutador ? usuario.EmpresaId : null;
        return emisor.Emitir(new DatosToken(usuario.Id, usuario.Email, rol, empresaId));
    }
}