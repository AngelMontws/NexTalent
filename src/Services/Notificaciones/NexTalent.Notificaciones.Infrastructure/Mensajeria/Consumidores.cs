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

/// <summary>
/// Al crearse una postulacion: inicia el plazo de 10 dias habiles (RN-04) y avisa a cada reclutador de la
/// empresa en la bandeja web (NT-041) y, si dio su consentimiento, por WhatsApp (NT-044).
/// Se puede repetir sin duplicar: (evento_id, usuario_id, canal) es unico.
/// </summary>
public sealed class PostulacionCreadaConsumidor(NotificacionesDbContext db, IWhatsapp whatsapp, TimeProvider reloj)
    : IConsumer<PostulacionCreada>
{
    public const string PlantillaWeb = "NT-041:NuevaPostulacion";
    public const string PlantillaWhatsapp = "NT-044:NuevaPostulacion";

    public Task Consume(ConsumeContext<PostulacionCreada> contexto) =>
        ProcesarAsync(contexto.Message, contexto.CancellationToken);

    /// <summary>Separado de Consume para poder probarlo sin MassTransit.</summary>
    public async Task ProcesarAsync(PostulacionCreada e, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();

        if (!await db.Seguimientos.AnyAsync(x => x.PostulacionId == e.PostulacionId, ct))
        {
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
        }

        // Decision 4 de contratos-eventos.md: mientras la vacante no tenga responsable, se avisa a todos los
        // reclutadores de la empresa.
        var reclutadores = await db.Contactos.AsNoTracking()
            .Where(x => x.EmpresaId == e.EmpresaId && x.Rol == "Reclutador")
            .ToListAsync(ct);
        var yaAvisados = (await db.Notificaciones.AsNoTracking()
                .Where(x => x.EventoId == e.EventoId)
                .Select(x => new { x.UsuarioId, x.Canal })
                .ToListAsync(ct))
            .Select(x => (x.UsuarioId, x.Canal))
            .ToHashSet();

        // 1) Bandeja web: queda "Enviada" al guardarse; SignalR (Postulaciones) la hace visible al instante.
        foreach (var r in reclutadores.Where(r => !yaAvisados.Contains((r.UsuarioId, Canal.Web))))
        {
            db.Notificaciones.Add(Fila(e, r.UsuarioId, Canal.Web, PlantillaWeb, EstadoEnvio.Enviada, ahora, ahora));
        }

        // Se guarda antes de enviar WhatsApp: si este paso falla y se reintenta, no se manda el mensaje dos veces.
        await db.SaveChangesAsync(ct);

        // 2) WhatsApp con opt-in. Sin reintentos: el aviso principal es la bandeja web.
        var texto = Textos.NuevaPostulacion(e.TituloVacante, e.Folio);
        foreach (var r in reclutadores.Where(r => r is { WhatsappOptIn: true, TelefonoWhatsapp: not null, WhatsappBajaFecha: null }
                     && !yaAvisados.Contains((r.UsuarioId, Canal.Whatsapp))))
        {
            ResultadoEnvio envio;
            try
            {
                envio = await whatsapp.EnviarPlantillaAsync(r.TelefonoWhatsapp!, PlantillaWhatsapp, texto, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TimeoutException)
            {
                envio = ResultadoEnvio.Fallo("sin_conexion");
            }

            var fila = Fila(e, r.UsuarioId, Canal.Whatsapp, PlantillaWhatsapp,
                envio.Exito ? EstadoEnvio.Enviada : EstadoEnvio.Fallida, ahora, envio.Exito ? ahora : null);
            fila.IdExterno = envio.IdExterno;
            fila.UltimoError = envio.CodigoError;
            db.Notificaciones.Add(fila);
            await db.SaveChangesAsync(ct);
        }
    }

    private static NotificacionFila Fila(
        PostulacionCreada e, Guid usuarioId, Canal canal, string plantilla, EstadoEnvio estado,
        DateTimeOffset ahora, DateTimeOffset? enviadaEn) => new()
    {
        Id = Guid.CreateVersion7(),
        EventoId = e.EventoId,
        UsuarioId = usuarioId,
        Canal = canal,
        Plantilla = plantilla,
        EstadoEnvio = estado,
        Intentos = 1,
        CreadaEn = ahora,
        EnviadaEn = enviadaEn,
        PostulacionId = e.PostulacionId,
        VacanteId = e.VacanteId,
        EmpresaId = e.EmpresaId,
        ReferenciaFolio = e.Folio,
        TituloVacante = e.TituloVacante,
        NombreEmpresa = e.NombreEmpresa
    };
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
