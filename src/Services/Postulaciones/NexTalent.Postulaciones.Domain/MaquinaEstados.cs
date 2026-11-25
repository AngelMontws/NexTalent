namespace NexTalent.Postulaciones.Domain;

/// <summary>Unica fuente de las transiciones permitidas.</summary>
public static class MaquinaEstados
{
    /// <summary>Pendiente = recibe "No seleccionado" cuando se cierra la vacante (HU-05.03).</summary>
    public static bool EsPendiente(EstadoPostulacion estado) =>
        estado is >= EstadoPostulacion.Enviada and <= EstadoPostulacion.Entrevista;

    /// <summary>Causa de un movimiento del reclutador, o null si no esta permitido.</summary>
    public static CausaCambio? CausaDeMovimiento(EstadoPostulacion desde, EstadoPostulacion hacia)
    {
        if (!Enum.IsDefined(desde) || !Enum.IsDefined(hacia) || desde == hacia)
        {
            return null;
        }

        if (desde == EstadoPostulacion.NoSeleccionado)
        {
            return hacia is EstadoPostulacion.Vista or EstadoPostulacion.EnRevision or EstadoPostulacion.Entrevista
                ? CausaCambio.Reactivacion
                : null;
        }

        if (hacia == EstadoPostulacion.NoSeleccionado)
        {
            return CausaCambio.Descarte;
        }

        if (hacia < desde)
        {
            return null;
        }

        return hacia == EstadoPostulacion.Vista ? CausaCambio.PerfilAbierto : CausaCambio.Avance;
    }
}
