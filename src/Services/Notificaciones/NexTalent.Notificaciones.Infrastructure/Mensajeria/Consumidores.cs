using MassTransit;
using Microsoft.EntityFrameworkCore;
using NexTalent.Contracts;
using NexTalent.Notificaciones.Application;
using NexTalent.Notificaciones.Domain;
using NexTalent.Notificaciones.Infrastructure.Data;

namespace NexTalent.Notificaciones.Infrastructure.Mensajeria;

/// <summary>
/// Sincroniza notificaciones.contacto_replica. Respeta Version y nunca reactiva una baja mas reciente.
/// </summary>
public sealed class ContactoActualizadoConsumidor(NotificacionesDbContext db)
    : IConsumer<ContactoActualizado>
{
    public async Task Consume(ConsumeContext<ContactoActualizado> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;

        var contacto = await db.Contactos.FirstOrDefaultAsync(x => x.UsuarioId == e.UsuarioId, ct);
        if (contacto is not null && contacto.VersionEvento >= e.Version)
        {
            return;
        }

        if (contacto is null)
        {
            contacto = new ContactoReplica { UsuarioId = e.UsuarioId };
            db.Contactos.Add(contacto);
        }

        contacto.Rol = e.Rol;
        contacto.EmpresaId = e.EmpresaId;
        contacto.Email = e.Email;
        contacto.TelefonoWhatsapp = e.TelefonoWhatsapp;
        contacto.VersionEvento = e.Version;

        // Regla acordada: un evento atrasado no puede reactivar una baja aplicada localmente.
        var bajaPosterior = contacto.WhatsappBajaFecha is { } baja
            && (e.WhatsappOptInFecha is null || e.WhatsappOptInFecha <= baja);

        if (!bajaPosterior)
        {
            contacto.WhatsappOptIn = e.WhatsappOptIn;
            contacto.WhatsappOptInFecha = e.WhatsappOptIn ? (e.WhatsappOptInFecha ?? e.OcurridoEn) : contacto.WhatsappOptInFecha;
        }

        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Registra el inicio del plazo de 10 dias habiles (RN-04) al crearse una postulacion.</summary>
public sealed class PostulacionCreadaConsumidor(NotificacionesDbContext db)
    : IConsumer<PostulacionCreada>
{
    public async Task Consume(ConsumeContext<PostulacionCreada> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;

        var seg = await db.Seguimientos.FirstOrDefaultAsync(x => x.PostulacionId == e.PostulacionId, ct);
        if (seg is not null)
        {
            return;
        }

        db.Seguimientos.Add(new PostulacionSeguimiento
        {
            PostulacionId = e.PostulacionId,
            CandidatoId = e.CandidatoId,
            VacanteId = e.VacanteId,
            EmpresaId = e.EmpresaId,
            Folio = e.Folio,
            TituloVacante = e.TituloVacante,
            EstadoActual = Estados.Enviada,
            Pendiente = true,
            UltimoCambioEn = e.CreadaEn,
            Secuencia = 1,
            Recordatorio10dEnviado = false
        });

        await db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Actualiza el seguimiento de 10 dias habiles (reiniciando recordatorio_10d_enviado) y despacha el aviso P-02.
/// </summary>
public sealed class EstatusCambiadoConsumidor(NotificacionesDbContext db, ServicioAvisos avisos)
    : IConsumer<EstatusCambiado>
{
    public async Task Consume(ConsumeContext<EstatusCambiado> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;

        var seg = await db.Seguimientos.FirstOrDefaultAsync(x => x.PostulacionId == e.PostulacionId, ct);
        if (seg is null)
        {
            seg = new PostulacionSeguimiento
            {
                PostulacionId = e.PostulacionId,
                CandidatoId = e.CandidatoId,
                VacanteId = e.VacanteId,
                EmpresaId = e.EmpresaId,
                Folio = e.Folio,
                TituloVacante = e.TituloVacante
            };
            db.Seguimientos.Add(seg);
        }

        if (e.Secuencia > seg.Secuencia)
        {
            seg.EstadoActual = e.EstadoNuevo;
            seg.Pendiente = e.EstadoNuevo is not (Estados.OfertaContratado or Estados.NoSeleccionado);
            seg.UltimoCambioEn = e.CambiadoEn;
            seg.Secuencia = e.Secuencia;
            seg.Recordatorio10dEnviado = false;
            seg.RecordatorioEnviadoEn = null;
            await db.SaveChangesAsync(ct);
        }

        await avisos.ProcesarAsync(e, ct);
    }
}

/// <summary>Marca como no pendientes los seguimientos de una vacante cerrada (RN-04).</summary>
public sealed class VacanteCerradaNotificacionesConsumidor(NotificacionesDbContext db)
    : IConsumer<VacanteCerrada>
{
    public async Task Consume(ConsumeContext<VacanteCerrada> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;

        var pendientes = await db.Seguimientos
            .Where(x => x.VacanteId == e.VacanteId && x.Pendiente)
            .ToListAsync(ct);

        foreach (var s in pendientes)
        {
            s.Pendiente = false;
            s.EstadoActual = Estados.NoSeleccionado;
            s.UltimoCambioEn = e.CerradaEn;
        }

        if (pendientes.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}

/// <summary>HU-08.01: avisa por correo a los reclutadores de la empresa cuando es aprobada o rechazada.</summary>
public sealed class EmpresaVerificadaNotificacionesConsumidor(
    NotificacionesDbContext db, ICorreo correo, TimeProvider reloj)
    : IConsumer<EmpresaVerificada>
{
    public async Task Consume(ConsumeContext<EmpresaVerificada> contexto)
    {
        var e = contexto.Message;
        var ct = contexto.CancellationToken;

        var reclutadores = await db.Contactos.AsNoTracking()
            .Where(x => x.EmpresaId == e.EmpresaId && x.Rol == "Reclutador")
            .ToListAsync(ct);

        var estado = e.Verificada ? "aprobada" : "rechazada";
        var asunto = $"NexTalent | Verificación de empresa {estado}";
        var cuerpo = $"NexTalent | Tu empresa '{e.Nombre}' fue {estado}."
            + (string.IsNullOrWhiteSpace(e.Motivo) ? "" : $"\nObservaciones: {e.Motivo}");

        foreach (var r in reclutadores)
        {
            if (await db.Notificaciones.AnyAsync(
                x => x.EventoId == e.EventoId && x.UsuarioId == r.UsuarioId && x.Canal == Canal.Email, ct))
            {
                continue;
            }

            var ahora = reloj.GetUtcNow();
            var envio = await correo.EnviarAsync(r.Email, asunto, cuerpo, ct);
            db.Notificaciones.Add(new NotificacionFila
            {
                Id = Guid.CreateVersion7(),
                EventoId = e.EventoId,
                UsuarioId = r.UsuarioId,
                EmpresaId = e.EmpresaId,
                NombreEmpresa = e.Nombre,
                Canal = Canal.Email,
                Plantilla = "HU-08.01:EmpresaVerificada",
                EstadoEnvio = envio.Exito ? EstadoEnvio.Enviada : EstadoEnvio.Fallida,
                Intentos = 1,
                CreadaEn = ahora,
                EnviadaEn = envio.Exito ? ahora : null,
                IdExterno = envio.IdExterno,
                UltimoError = envio.CodigoError
            });
        }

        if (reclutadores.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
