namespace NexTalent.Identidad.Domain;

/// <summary>
/// Roles del sistema (documento del proyecto, seccion 2.1) y reglas sobre ellos.
/// Regla de seguridad: el registro publico nunca permite crear un administrador.
/// </summary>
public static class RolesSistema
{
    public const string Candidato = "candidato";
    public const string Reclutador = "reclutador";
    public const string Admin = "admin";

    private static readonly string[] Todos = [Candidato, Reclutador, Admin];
    private static readonly string[] RegistroPublico = [Candidato, Reclutador];

    public static bool EsValido(string? rol) => Contiene(Todos, rol);

    public static bool PermiteRegistroPublico(string? rol) => Contiene(RegistroPublico, rol);

    private static bool Contiene(string[] roles, string? rol) =>
        !string.IsNullOrWhiteSpace(rol)
        && Array.Exists(roles, r => string.Equals(r, rol, StringComparison.OrdinalIgnoreCase));
}