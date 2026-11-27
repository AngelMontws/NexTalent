using MassTransit;
using Microsoft.EntityFrameworkCore;
using NexTalent.Contracts;
using NexTalent.Vacantes.Infrastructure.Persistence;
using NexTalent.Vacantes.Infrastructure.Persistence.Entities;

namespace NexTalent.Vacantes.Infrastructure.Mensajeria;

/// <summary>
/// Sincroniza vacantes.empresa_resumen cuando Identidad aprueba o rechaza una empresa (HU-08.01).
/// Sin llave foranea hacia identidad.empresa.
/// </summary>
public sealed class EmpresaVerificadaConsumidor(VacantesDbContext db, TimeProvider reloj)
    : IConsumer<EmpresaVerificada>
{
    public async Task Consume(ConsumeContext<EmpresaVerificada> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;
        var ahora = reloj.GetUtcNow().UtcDateTime;

        var resumen = await db.Empresas.FirstOrDefaultAsync(x => x.EmpresaId == e.EmpresaId, ct);
        if (resumen is null)
        {
            resumen = new EmpresaResumen
            {
                EmpresaId = e.EmpresaId,
                IndiceRespuestaPct = 0m
            };
            db.Empresas.Add(resumen);
        }

        resumen.Nombre = e.Nombre;
        resumen.Verificada = e.Verificada;
        resumen.ActualizadoEn = ahora;
        await db.SaveChangesAsync(ct);
    }
}
