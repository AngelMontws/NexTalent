namespace NexTalent.Postulaciones.Domain;

/// <summary>
/// Postulacion de un candidato a una vacante. El estado solo cambia por sus metodos,
/// y cada cambio agrega exactamente un registro al historial.
/// </summary>
public sealed class Postulacion
{
    /// <summary>Limite de historial_estado.motivo en el diccionario de datos.</summary>
    public const int MotivoMaximo = 300;

    private readonly List<CambioEstado> _historial = [];

    /// <summary>Solo para EF Core al leer de la base de datos.</summary>
    private Postulacion()
    {
        Folio = null!;
    }

    private Postulacion(
        Guid id,
        string folio,
        Guid candidatoId,
        Guid vacanteId,
        ResultadoCoincidencia coincidencia,
        DateTimeOffset creadaEn)
    {
        Id = id;
        Folio = folio;
        CandidatoId = candidatoId;
        VacanteId = vacanteId;
        CoincidenciaPct = coincidencia.Porcentaje;
        DeseablesCumplidas = coincidencia.DeseablesCumplidas;
        CreadaEn = creadaEn;
    }

    public Guid Id { get; }
    public string Folio { get; }
    public Guid CandidatoId { get; }
    public Guid VacanteId { get; }

    /// <summary>Instantanea al postularse: no se recalcula.</summary>
    public decimal CoincidenciaPct { get; }

    /// <summary>Instantanea para desempatar; no altera el porcentaje.</summary>
    public int DeseablesCumplidas { get; }

    public DateTimeOffset CreadaEn { get; }
    public CanalOrigen CanalOrigen { get; private init; }
    public EstadoPostulacion EstadoActual { get; private set; }

    /// <summary>Inicio de la etapa actual: dias en la etapa y plazo de 10 dias habiles.</summary>
    public DateTimeOffset EtapaActualizadaEn { get; private set; }

    /// <summary>Se reinicia con cada cambio de etapa (RN-04).</summary>
    public bool RecordatorioEnviado { get; private set; }
    public IReadOnlyList<CambioEstado> Historial => _historial.AsReadOnly();

    /// <summary>T01. El duplicado y el perfil minimo se validan en Application.</summary>
    public static Postulacion Crear(
        Guid id,
        string folio,
        Guid candidatoId,
        Guid vacanteId,
        ResultadoCoincidencia coincidencia,
        bool vacanteAbierta,
        DateTimeOffset fecha,
        CanalOrigen canal = CanalOrigen.Web)
    {
        ExigirId(id, nameof(id));
        ExigirId(candidatoId, nameof(candidatoId));
        ExigirId(vacanteId, nameof(vacanteId));
        ArgumentException.ThrowIfNullOrWhiteSpace(folio);
        ArgumentNullException.ThrowIfNull(coincidencia);

        if (!vacanteAbierta)
        {
            throw new VacanteNoAbiertaException();
        }

        if (!Enum.IsDefined(canal))
        {
            throw new ArgumentOutOfRangeException(nameof(canal));
        }

        var postulacion = new Postulacion(
            id, folio.Trim(), candidatoId, vacanteId, coincidencia, fecha.ToUniversalTime())
        {
            CanalOrigen = canal
        };
        postulacion.Registrar(null, EstadoPostulacion.Enviada, CausaCambio.Creacion, candidatoId, null, fecha);
        return postulacion;
    }

    /// <summary>T02. Solo cambia si esta en Enviada; devuelve null si no hubo cambio.</summary>
    public CambioEstado? AbrirPerfil(Guid reclutadorId, DateTimeOffset fecha)
    {
        ExigirId(reclutadorId, nameof(reclutadorId));

        return EstadoActual == EstadoPostulacion.Enviada
            ? Registrar(EstadoActual, EstadoPostulacion.Vista, CausaCambio.PerfilAbierto, reclutadorId, null, fecha)
            : null;
    }

    /// <summary>T03, T04 y T06. Que la vacante sea de su empresa se valida en Application.</summary>
    public CambioEstado Mover(
        EstadoPostulacion destino,
        Guid reclutadorId,
        bool vacanteAbierta,
        string? motivo,
        DateTimeOffset fecha)
    {
        ExigirId(reclutadorId, nameof(reclutadorId));

        var causa = MaquinaEstados.CausaDeMovimiento(EstadoActual, destino)
            ?? throw new TransicionInvalidaException(EstadoActual, destino);

        if (!vacanteAbierta && causa is CausaCambio.Avance or CausaCambio.Reactivacion)
        {
            throw new VacanteNoAbiertaException();
        }

        if (causa == CausaCambio.Reactivacion && string.IsNullOrWhiteSpace(motivo))
        {
            throw new MotivoObligatorioException();
        }

        if (motivo?.Trim().Length > MotivoMaximo)
        {
            throw new ArgumentException($"El motivo admite hasta {MotivoMaximo} caracteres.", nameof(motivo));
        }

        return Registrar(EstadoActual, destino, causa, reclutadorId, motivo, fecha);
    }

    /// <summary>Recordatorio de 10 dias habiles enviado al reclutador. No cambia el estado.</summary>
    public void MarcarRecordatorioEnviado() => RecordatorioEnviado = true;

    /// <summary>T05. Se puede repetir sin efecto; devuelve null si ya no estaba pendiente.</summary>
    public CambioEstado? CerrarPorVacante(string? motivo, DateTimeOffset fecha) =>
        MaquinaEstados.EsPendiente(EstadoActual)
            ? Registrar(EstadoActual, EstadoPostulacion.NoSeleccionado, CausaCambio.VacanteCerrada, null, motivo, fecha)
            : null;

    private CambioEstado Registrar(
        EstadoPostulacion? anterior,
        EstadoPostulacion nuevo,
        CausaCambio causa,
        Guid? autor,
        string? motivo,
        DateTimeOffset fecha)
    {
        var texto = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        var cambio = new CambioEstado(
            _historial.Count + 1, anterior, nuevo, causa, autor, texto, fecha.ToUniversalTime());
        _historial.Add(cambio);
        EstadoActual = nuevo;
        EtapaActualizadaEn = cambio.Fecha;
        RecordatorioEnviado = false;
        return cambio;
    }

    private static void ExigirId(Guid valor, string nombre)
    {
        if (valor == Guid.Empty)
        {
            throw new ArgumentException("El identificador es obligatorio.", nombre);
        }
    }
}
