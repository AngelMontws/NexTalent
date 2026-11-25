namespace NexTalent.Postulaciones.Application;

/// <summary>Base de los errores de casos de uso. La Api los convierte en codigos HTTP.</summary>
public abstract class AplicacionException(string mensaje) : Exception(mensaje);

/// <summary>403: rol incorrecto, o recurso de otra persona o empresa.</summary>
public sealed class AccesoDenegadoException()
    : AplicacionException("No tienes permiso para esta accion.");

/// <summary>404.</summary>
public sealed class NoEncontradaException()
    : AplicacionException("No se encontro el recurso.");

/// <summary>409: HU-05.01.</summary>
public sealed class YaPostuladoException()
    : AplicacionException("Ya te postulaste");

/// <summary>400: HU-02.04 pide indicar exactamente que falta.</summary>
public sealed class PerfilIncompletoException(IReadOnlyList<string> faltantes)
    : AplicacionException("Tu perfil esta incompleto.")
{
    public IReadOnlyList<string> Faltantes { get; } = faltantes;
}

/// <summary>503: la copia local de la vacante aun no llega; se puede reintentar.</summary>
public sealed class VacanteNoDisponibleException()
    : AplicacionException("La vacante aun no esta disponible. Intenta de nuevo.");

/// <summary>409: otra persona modifico la postulacion primero.</summary>
public sealed class ConflictoConcurrenciaException()
    : AplicacionException("La postulacion cambio mientras la editabas. Vuelve a cargarla.");
