namespace NexTalent.Vacantes.Infrastructure.Persistence.Entities;

/// <summary>
/// Copia local de la empresa (vacantes.empresa_resumen). Se alimenta con el evento EmpresaVerificada de
/// Identidad: sin llave foranea hacia identidad.empresa.
/// </summary>
public sealed class EmpresaResumen
{
    public Guid EmpresaId { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Verificada { get; set; }
    public decimal IndiceRespuestaPct { get; set; }
    public DateTime ActualizadoEn { get; set; }

    public List<Vacante> Vacantes { get; set; } = [];
}

public sealed class Vacante
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    /// <summary>Referencia logica a identidad.reclutador.usuario_id (claim sub del JWT): sin llave foranea.</summary>
    public Guid ReclutadorId { get; set; }

    public string Titulo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string? Actividades { get; set; }
    public string? Beneficios { get; set; }

    /// <summary>RN-02: obligatorios y mayores a cero. MXN mensuales.</summary>
    public decimal SalarioMin { get; set; }
    public decimal SalarioMax { get; set; }
    public string TipoSalario { get; set; } = "Bruto";
    public string Modalidad { get; set; } = null!;
    public string Jornada { get; set; } = null!;
    public string Ubicacion { get; set; } = null!;

    /// <summary>Borrador, Publicada, OcultaPorReportes, Cerrada o Caducada.</summary>
    public string Estado { get; set; } = "Borrador";

    public DateTime? PublicadaEn { get; set; }

    /// <summary>RN-04: publicada_en + 30 dias.</summary>
    public DateTime? ExpiraEn { get; set; }

    public bool Recordatorio27dEnviado { get; set; }

    /// <summary>Crece con cada cambio publicado en los eventos VacantePublicada y VacanteCerrada.</summary>
    public long VersionEvento { get; set; }

    public DateTime CreadaEn { get; set; }

    public EmpresaResumen Empresa { get; set; } = null!;
    public List<VacanteHabilidad> Habilidades { get; set; } = [];
    public List<ReporteVacante> Reportes { get; set; } = [];
}

public sealed class Habilidad
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Categoria { get; set; } = null!;

    /// <summary>Aprobada, Pendiente o Rechazada.</summary>
    public string EstadoAprobacion { get; set; } = "Pendiente";

    /// <summary>Usuario que la propuso (referencia logica a identidad.usuario).</summary>
    public Guid? PropuestaPor { get; set; }
}

public sealed class VacanteHabilidad
{
    public Guid VacanteId { get; set; }
    public Guid HabilidadId { get; set; }

    /// <summary>1 = Basico, 2 = Intermedio, 3 = Avanzado.</summary>
    public byte Nivel { get; set; }

    public bool EsRequerida { get; set; }

    public Vacante Vacante { get; set; } = null!;
    public Habilidad Habilidad { get; set; } = null!;
}

public sealed class ReporteVacante
{
    public Guid Id { get; set; }
    public Guid VacanteId { get; set; }

    /// <summary>Quien reporta (referencia logica a identidad.usuario). Uno por vacante.</summary>
    public Guid UsuarioId { get; set; }

    public string MotivoTipo { get; set; } = null!;
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }

    public Vacante Vacante { get; set; } = null!;
}

/// <summary>Registro inmutable de decisiones de moderacion (HU-08.03): sin UPDATE ni DELETE.</summary>
public sealed class BitacoraModeracion
{
    public Guid Id { get; set; }

    /// <summary>Vacante o Habilidad.</summary>
    public string EntidadTipo { get; set; } = null!;

    public Guid EntidadId { get; set; }
    public Guid AdministradorId { get; set; }
    public string Accion { get; set; } = null!;
    public string Motivo { get; set; } = null!;
    public DateTime Fecha { get; set; }
}
