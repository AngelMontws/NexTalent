namespace NexTalent.Postulaciones.Application;

/// <summary>Datos del JWT: sub, role (Candidato, Reclutador, Administrador) y empresa_id. El rol se compara sin distinguir mayusculas.</summary>
public sealed record Usuario(Guid Id, string Rol, Guid? EmpresaId)
{
    public const string RolCandidato = "Candidato";
    public const string RolReclutador = "Reclutador";

    internal void ExigirCandidato() => Exigir(RolCandidato);

    internal void ExigirReclutadorDe(Guid empresaId)
    {
        ExigirReclutador();
        if (EmpresaId != empresaId)
        {
            throw new AccesoDenegadoException();
        }
    }

    /// <summary>Se llama antes de buscar, para no revelar si el recurso existe.</summary>
    internal void ExigirReclutador() => Exigir(RolReclutador);

    private void Exigir(string rol)
    {
        if (!string.Equals(Rol, rol, StringComparison.OrdinalIgnoreCase))
        {
            throw new AccesoDenegadoException();
        }
    }
}
