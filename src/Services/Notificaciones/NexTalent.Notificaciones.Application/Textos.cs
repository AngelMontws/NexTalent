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
