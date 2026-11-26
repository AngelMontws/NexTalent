using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexTalent.Notificaciones.Application;
using NexTalent.Notificaciones.Domain;
using NexTalent.Notificaciones.Infrastructure.Data;

namespace NexTalent.Notificaciones.Infrastructure.Tareas;

/// <summary>
/// RN-04 (Acuerdo 4.B): job recurrente de Hangfire en Notificaciones. Revisa postulaciones activas cuya ultima
/// actualizacion supere 10 dias habiles y tengan recordatorio_10d_enviado = 0, envia el recordatorio al reclutador
/// y sincroniza la bandera en Postulaciones.
/// </summary>
public sealed class ServicioRecordatorioDiezDias(
    NotificacionesDbContext db,
    ICorreo correo,
    ClientePostulacionesInterno postulaciones,
    TimeProvider reloj,
    ILogger<ServicioRecordatorioDiezDias> log)
{
    public const int UmbralDiasHabiles = 10;

    public async Task<int> EjecutarAsync(CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var candidatos = await db.Seguimientos
            .Where(x => x.Pendiente && !x.Recordatorio10dEnviado)
            .ToListAsync(ct);

        var enviados = 0;
        foreach (var s in candidatos)
        {
            if (ContarDiasHabiles(s.UltimoCambioEn.UtcDateTime, ahora.UtcDateTime) < UmbralDiasHabiles)
            {
                continue;
            }

            var reclutadores = await db.Contactos.AsNoTracking()
                .Where(x => x.EmpresaId == s.EmpresaId && x.Rol == "Reclutador")
                .ToListAsync(ct);

            if (reclutadores.Count == 0)
            {
                continue;
            }

            var exitoAlguno = false;
            var asunto = $"NexTalent | Recordatorio de postulación pendiente ({s.Folio})";
            var texto = $"NexTalent | La postulación {s.Folio} para '{s.TituloVacante}' lleva {UmbralDiasHabiles} o más días hábiles en '{s.EstadoActual}' sin movimiento.";

            foreach (var r in reclutadores)
            {
                var envio = await correo.EnviarAsync(r.Email, asunto, texto, ct);
                db.Notificaciones.Add(new NotificacionFila
                {
                    Id = Guid.CreateVersion7(),
                    UsuarioId = r.UsuarioId,
                    PostulacionId = s.PostulacionId,
                    VacanteId = s.VacanteId,
                    EmpresaId = s.EmpresaId,
                    ReferenciaFolio = s.Folio,
                    TituloVacante = s.TituloVacante,
                    Canal = Canal.Email,
                    Plantilla = "RN-04:Recordatorio10d",
                    EstadoEnvio = envio.Exito ? EstadoEnvio.Enviada : EstadoEnvio.Fallida,
                    Intentos = 1,
                    CreadaEn = ahora,
                    EnviadaEn = envio.Exito ? ahora : null,
                    IdExterno = envio.IdExterno,
                    UltimoError = envio.CodigoError
                });
                exitoAlguno |= envio.Exito;
            }

            if (!exitoAlguno)
            {
                await db.SaveChangesAsync(ct);
                continue;
            }

            s.Recordatorio10dEnviado = true;
            s.RecordatorioEnviadoEn = ahora;
            await db.SaveChangesAsync(ct);
            enviados++;

            try
            {
                await postulaciones.MarcarRecordatorioEnviadoAsync(s.PostulacionId, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                log.LogWarning("Recordatorio enviado para {PostulacionId}, pendiente sincronizar bandera en Postulaciones.", s.PostulacionId);
            }
        }

        return enviados;
    }

    /// <summary>Cuenta dias habiles en Mexico (lunes a viernes, excluyendo descansos obligatorios LFT Art. 74).</summary>
    public static int ContarDiasHabiles(DateTime desdeUtc, DateTime hastaUtc)
    {
        var inicio = DateOnly.FromDateTime(desdeUtc.Date);
        var fin = DateOnly.FromDateTime(hastaUtc.Date);
        if (fin <= inicio)
        {
            return 0;
        }

        var habiles = 0;
        for (var dia = inicio.AddDays(1); dia <= fin; dia = dia.AddDays(1))
        {
            if (dia.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !EsFestivoOficialMx(dia))
            {
                habiles++;
            }
        }

        return habiles;
    }

    private static bool EsFestivoOficialMx(DateOnly fecha)
    {
        if ((fecha.Month == 1 && fecha.Day == 1)
            || (fecha.Month == 5 && fecha.Day == 1)
            || (fecha.Month == 9 && fecha.Day == 16)
            || (fecha.Month == 12 && fecha.Day == 25))
        {
            return true;
        }

        // Primer lunes de febrero, tercer lunes de marzo y tercer lunes de noviembre (LFT Art. 74).
        if (fecha.DayOfWeek == DayOfWeek.Monday)
        {
            if (fecha.Month == 2 && fecha.Day <= 7) return true;
            if (fecha.Month == 3 && fecha.Day is >= 15 and <= 21) return true;
            if (fecha.Month == 11 && fecha.Day is >= 15 and <= 21) return true;
        }

        return false;
    }
}
