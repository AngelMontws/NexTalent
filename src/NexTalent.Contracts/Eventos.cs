namespace NexTalent.Contracts;

// Contratos de docs/backend2/contratos-eventos.md. Reglas:
// - EventoId sirve para descartar repetidos. Fechas en UTC.
// - Version crece en cada cambio de la misma entidad; se ignora una version anterior.
// - Estados, causas y niveles viajan como texto (ver Valores.cs).
// - Nunca viajan CURP, CV, direccion, datos bancarios ni contrasenas.

/// <summary>Identidad aprueba o rechaza una empresa.</summary>
public sealed record EmpresaVerificada(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid EmpresaId,
    string Nombre,
    bool Verificada,
    long Version);

/// <summary>Nivel: ver <see cref="Niveles"/>.</summary>
public sealed record HabilidadDePerfil(Guid HabilidadId, string Nivel);

/// <summary>Identidad publica el perfil completo. Las banderas evitan copiar datos personales.</summary>
public sealed record PerfilHabilidadesActualizado(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid CandidatoId,
    IReadOnlyList<HabilidadDePerfil> Habilidades,
    bool TieneNombre,
    bool TieneContacto,
    long Version);

/// <summary>Nivel: ver <see cref="Niveles"/>.</summary>
public sealed record HabilidadDeVacante(Guid HabilidadId, string Nivel, bool EsRequerida);

/// <summary>Vacantes la publica al publicar, editar, renovar o reabrir.</summary>
public sealed record VacantePublicada(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid VacanteId,
    Guid EmpresaId,
    string NombreEmpresa,
    string Titulo,
    Guid? ReclutadorId,
    decimal SalarioMin,
    decimal SalarioMax,
    string TipoSalario,
    DateTimeOffset PublicadaEn,
    DateTimeOffset ExpiraEn,
    IReadOnlyList<HabilidadDeVacante> Habilidades,
    long Version);

/// <summary>Cierre definitivo. Causa: ver <see cref="CausasCierre"/>. Comparte Version con VacantePublicada.</summary>
public sealed record VacanteCerrada(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid VacanteId,
    Guid EmpresaId,
    string Titulo,
    string Causa,
    DateTimeOffset CerradaEn,
    long Version);

/// <summary>Postulaciones crea una postulacion (T01).</summary>
public sealed record PostulacionCreada(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid PostulacionId,
    string Folio,
    Guid CandidatoId,
    Guid VacanteId,
    Guid EmpresaId,
    string TituloVacante,
    string NombreEmpresa,
    DateTimeOffset CreadaEn);

/// <summary>
/// Cambio de etapa (T02 a T06). Estados: ver <see cref="Estados"/>. Causa: ver <see cref="CausasCambio"/>.
/// El motivo no viaja porque puede contener texto personal.
/// </summary>
public sealed record EstatusCambiado(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid PostulacionId,
    string Folio,
    Guid CandidatoId,
    Guid VacanteId,
    Guid EmpresaId,
    string TituloVacante,
    string NombreEmpresa,
    string EstadoAnterior,
    string EstadoNuevo,
    string Causa,
    DateTimeOffset CambiadoEn,
    int Secuencia);
