using MassTransit;
using Microsoft.EntityFrameworkCore;
using NexTalent.Contracts;
using NexTalent.Postulaciones.Application;
using NexTalent.Postulaciones.Domain;
using NexTalent.Postulaciones.Infrastructure.Data;

namespace NexTalent.Postulaciones.Infrastructure.Mensajeria;

// Cada consumidor corre con el Inbox de MassTransit: un mensaje repetido no se procesa dos veces.
// Version: se ignora un evento mas viejo que la copia guardada.

/// <summary>Copia local de la vacante y sus habilidades (para validar y calcular RN-03).</summary>
public sealed class VacantePublicadaConsumidor(PostulacionesDbContext db, TimeProvider reloj)
    : IConsumer<VacantePublicada>
{
    public async Task Consume(ConsumeContext<VacantePublicada> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;
        var vacante = await db.Vacantes.FirstOrDefaultAsync(v => v.VacanteId == e.VacanteId, ct);
        if (vacante is not null && vacante.VersionEvento >= e.Version)
        {
            return;
        }

        if (vacante is null)
        {
            vacante = new VacanteReplica { VacanteId = e.VacanteId };
            db.Vacantes.Add(vacante);
        }

        vacante.EmpresaId = e.EmpresaId;
        vacante.Titulo = e.Titulo;
        vacante.NombreEmpresa = e.NombreEmpresa;
        vacante.SalarioMin = e.SalarioMin;
        vacante.SalarioMax = e.SalarioMax;
        vacante.TipoSalario = e.TipoSalario;
        vacante.ExpiraEn = e.ExpiraEn;
        vacante.Abierta = true;
        vacante.ActualizadaEn = reloj.GetUtcNow();
        vacante.VersionEvento = e.Version;

        await db.VacanteHabilidades.Where(h => h.VacanteId == e.VacanteId).ExecuteDeleteAsync(ct);
        db.VacanteHabilidades.AddRange(e.Habilidades.Select(h => new VacanteHabilidadReplica
        {
            VacanteId = e.VacanteId, HabilidadId = h.HabilidadId, Nivel = Nivel(h.Nivel), Requerida = h.EsRequerida
        }));

        await db.SaveChangesAsync(ct);
    }

    internal static NivelHabilidad Nivel(string texto) =>
        Enum.TryParse<NivelHabilidad>(texto, ignoreCase: false, out var nivel) && Enum.IsDefined(nivel)
            ? nivel
            : throw new ArgumentException($"Nivel desconocido: {texto}.", nameof(texto));
}

/// <summary>HU-05.03: marca la vacante como cerrada y responde a todas las postulaciones pendientes.</summary>
public sealed class VacanteCerradaConsumidor(PostulacionesDbContext db, ServicioCierre cierre, TimeProvider reloj)
    : IConsumer<VacanteCerrada>
{
    public async Task Consume(ConsumeContext<VacanteCerrada> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;
        var vacante = await db.Vacantes.FirstOrDefaultAsync(v => v.VacanteId == e.VacanteId, ct);
        if (vacante is not null && vacante.VersionEvento > e.Version)
        {
            return;
        }

        if (vacante is null)
        {
            // Puede llegar antes que VacantePublicada: se guarda cerrada para rechazar postulaciones.
            vacante = new VacanteReplica { VacanteId = e.VacanteId, EmpresaId = e.EmpresaId, Titulo = e.Titulo };
            db.Vacantes.Add(vacante);
        }

        vacante.Abierta = false;
        vacante.ActualizadaEn = reloj.GetUtcNow();
        vacante.VersionEvento = e.Version;
        await db.SaveChangesAsync(ct);

        // Guarda cada cambio y su EstatusCambiado en la misma transaccion (Outbox).
        await cierre.CerrarAsync(e.VacanteId, e.Causa, ct);
    }
}

/// <summary>Copia local de habilidades y perfil minimo, sin datos personales.</summary>
public sealed class PerfilHabilidadesActualizadoConsumidor(PostulacionesDbContext db)
    : IConsumer<PerfilHabilidadesActualizado>
{
    public async Task Consume(ConsumeContext<PerfilHabilidadesActualizado> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;
        var candidato = await db.Candidatos.FirstOrDefaultAsync(c => c.CandidatoId == e.CandidatoId, ct);
        if (candidato is not null && candidato.VersionEvento >= e.Version)
        {
            return;
        }

        if (candidato is null)
        {
            candidato = new CandidatoReplica { CandidatoId = e.CandidatoId };
            db.Candidatos.Add(candidato);
        }

        candidato.TieneNombre = e.TieneNombre;
        candidato.TieneContacto = e.TieneContacto;
        candidato.VersionEvento = e.Version;

        await db.CandidatoHabilidades.Where(h => h.CandidatoId == e.CandidatoId).ExecuteDeleteAsync(ct);
        db.CandidatoHabilidades.AddRange(e.Habilidades
            .GroupBy(h => h.HabilidadId)
            .Select(g => new CandidatoHabilidadReplica
            {
                CandidatoId = e.CandidatoId,
                HabilidadId = g.Key,
                Nivel = g.Max(h => VacantePublicadaConsumidor.Nivel(h.Nivel))
            }));

        await db.SaveChangesAsync(ct);
    }
}
