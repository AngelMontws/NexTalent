using NexTalent.Contracts;

namespace NexTalent.Notificaciones.Application;

/// <summary>
/// POR CONFIRMAR. Unico lugar con los textos de Frontend ("Mensajes, estados y vinculacion", 25 nov 2026).
/// </summary>
public static class Textos
{
    public const string CodigoIncorrecto = "El código no coincide. Revisa los 6 dígitos e inténtalo de nuevo.";
    public const string CodigoVencido = "Este código venció. Solicita uno nuevo para continuar.";
    public const string LimiteIntentos = "Por seguridad, espera 15 minutos antes de solicitar otro código.";
    public const string NumeroVinculado = "Tu número de WhatsApp quedó vinculado correctamente.";
    public const string NumeroNoDisponible = "No pudimos enviar el código. Verifica tu número o inténtalo más tarde.";
    public const string Baja =
        "NexTalent: desactivamos tus notificaciones por WhatsApp. No recibirás más mensajes por este canal. "
        + "Puedes reactivarlas desde Mi cuenta > Notificaciones de WhatsApp.";

    public static string CodigoReenviado(string telefono) =>
        $"Enviamos un nuevo código al número terminado en {telefono[^4..]}.";

    /// <summary>
    /// HU-07.03. Se agrega el folio al duplicado porque la historia lo exige, y se omite el CV del perfil
    /// incompleto porque el documento v5 no lo pide. Frontend no dio texto para vacante cerrada: es provisional.
    /// </summary>
    public static string Postulacion(ResultadoPostulacion r, string? urlMiCuenta) => r.Tipo switch
    {
        TipoResultado.Creada =>
            $"¡Postulación enviada!\nTu perfil se registró correctamente.\nFolio: {r.Folio}\n"
            + "Consulta el estado en Mis postulaciones.",
        TipoResultado.YaPostulado =>
            "Ya te postulaste a esta vacante.\nTu postulación está registrada; consulta su estado y folio en "
            + "Mis postulaciones." + (r.Folio is null ? "" : $"\nFolio: {r.Folio}"),
        TipoResultado.PerfilIncompleto =>
            "Tu perfil está incompleto.\nRevisa los datos obligatorios en Mi cuenta para continuar con la postulación."
            + (urlMiCuenta is null ? "" : $"\n{urlMiCuenta}"),
        TipoResultado.VacanteCerrada => "La vacante ya está cerrada.",
        _ => throw new ArgumentOutOfRangeException(nameof(r))
    };

    /// <summary>
    /// NT-041 y NT-044: aviso al reclutador, con el texto del Manual de Identidad (seccion 07). Sin nombre,
    /// CURP, CV ni direccion: solo puesto y folio.
    /// </summary>
    public static string NuevaPostulacion(string tituloVacante, string folio) => $"""
        NexTalent | Nueva postulación
        Una persona se postuló a tu vacante.
        Puesto: {tituloVacante}
        Folio: {folio}
        Consulta el perfil en NexTalent para revisar la postulación.
        """.ReplaceLineEndings("\n");

    /// <summary>HU-06.03: agrega el texto que escribió el reclutador, si lo hay.</summary>
    public static string ConMensajeDelReclutador(string texto, string? mensaje) =>
        string.IsNullOrWhiteSpace(mensaje) ? texto : $"{texto}\n\nMensaje del reclutador:\n{mensaje.Trim()}";

    /// <summary>P-02. El enlace de similares se omite mientras no exista la pagina.</summary>
    public static string Estatus(EstatusCambiado e, string? urlSimilares)
    {
        var (etiqueta, mensaje) = e.EstadoNuevo switch
        {
            Estados.Vista => ("Vista",
                "El reclutador abrió tu postulación. Consulta el seguimiento en Mis postulaciones."),
            Estados.EnRevision => ("En revisión",
                "Tu perfil se encuentra en evaluación. Te avisaremos cuando cambie de etapa."),
            Estados.Entrevista => ("Entrevista",
                "Avanzaste a esta etapa. Revisa el seguimiento en NexTalent para conocer los próximos pasos."),
            // Frontend los separa y el documento v5 los une: falta acordar el texto.
            Estados.OfertaContratado => ("Oferta/Contratado", null),
            Estados.NoSeleccionado => ("No seleccionado",
                "Este proceso concluyó sin selección. Gracias por participar."
                + (urlSimilares is null ? "" : $"\nVacantes similares: {urlSimilares}")),
            _ => throw new ArgumentException($"Estado sin aviso: {e.EstadoNuevo}.", nameof(e))
        };

        var texto = $"""
            NexTalent | Actualización de postulación
            Puesto: {e.TituloVacante}
            Empresa: {e.NombreEmpresa}
            Folio: {e.Folio}
            Estado: {etiqueta}.
            """.ReplaceLineEndings("\n");
        return mensaje is null ? texto : $"{texto}\n{mensaje}";
    }
}
