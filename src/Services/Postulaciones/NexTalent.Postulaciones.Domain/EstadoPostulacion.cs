namespace NexTalent.Postulaciones.Domain;

/// <summary>Cinco estados de avance, en orden, y uno de cierre (D-01).</summary>
public enum EstadoPostulacion
{
    Enviada = 1,
    Vista = 2,
    EnRevision = 3,
    Entrevista = 4,
    OfertaContratado = 5,
    NoSeleccionado = 6
}

/// <summary>Por que cambio el estado (T01 a T06 de docs/backend2/maquina-estados.md).</summary>
public enum CausaCambio
{
    Creacion = 1,
    PerfilAbierto = 2,
    Avance = 3,
    Descarte = 4,
    VacanteCerrada = 5,
    Reactivacion = 6
}
