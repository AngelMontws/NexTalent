using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NexTalent.Postulaciones.Application;

namespace NexTalent.Postulaciones.Api.TiempoReal;

/// <summary>
/// HU-05.02 y RNF-02: el candidato ve su cambio de estatus sin recargar. El navegador manda el JWT en
/// ?access_token=. Solo escucha: no expone metodos que el cliente pueda llamar.
/// Candidato: grupo propio. Reclutador: grupo de su empresa, para refrescar el panel.
/// </summary>
[Authorize]
public sealed class HubEstatus : Hub
{
    public const string Ruta = "/hubs/estatus";

    // Nombres de los mensajes que escucha el Frontend con connection.on(...).
    public const string MensajeEstatus = "estatusCambiado";
    public const string MensajeNuevaPostulacion = "postulacionCreada";

    public static string GrupoCandidato(Guid id) => $"candidato:{id}";

    public static string GrupoEmpresa(Guid id) => $"empresa:{id}";

    public override async Task OnConnectedAsync()
    {
        var usuario = Context.User is { } principal ? Seguridad.Usuario(principal) : null;
        if (usuario is null)
        {
            Context.Abort();
            return;
        }

        if (string.Equals(usuario.Rol, Usuario.RolCandidato, StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoCandidato(usuario.Id));
        }
        else if (string.Equals(usuario.Rol, Usuario.RolReclutador, StringComparison.OrdinalIgnoreCase)
            && usuario.EmpresaId is { } empresa)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoEmpresa(empresa));
        }

        await base.OnConnectedAsync();
    }
}
