namespace NexTalent.Postulaciones.Domain;

/// <summary>Base de los errores de reglas de negocio.</summary>
public abstract class ReglaPostulacionException(string mensaje) : Exception(mensaje);

public sealed class TransicionInvalidaException(EstadoPostulacion desde, EstadoPostulacion hacia)
    : ReglaPostulacionException($"No se puede pasar de {desde} a {hacia}.");

public sealed class MotivoObligatorioException()
    : ReglaPostulacionException("Reactivar una postulacion requiere un motivo.");

public sealed class VacanteNoAbiertaException()
    : ReglaPostulacionException("La vacante ya esta cerrada.");
