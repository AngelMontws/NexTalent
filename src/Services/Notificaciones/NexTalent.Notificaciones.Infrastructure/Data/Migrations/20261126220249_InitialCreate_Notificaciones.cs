using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTalent.Notificaciones.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_Notificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notificaciones");

            migrationBuilder.CreateTable(
                name: "alerta_vacante_enviada",
                schema: "notificaciones",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    vacante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    id_alerta = table.Column<string>(type: "varchar(128)", nullable: true),
                    boton = table.Column<string>(type: "varchar(30)", nullable: true),
                    enviada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerta_vacante_enviada", x => new { x.usuario_id, x.vacante_id });
                });

            migrationBuilder.CreateTable(
                name: "codigo_whatsapp",
                schema: "notificaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    telefono = table.Column<string>(type: "varchar(20)", nullable: true),
                    codigo_hash = table.Column<byte[]>(type: "varbinary(32)", nullable: true),
                    enviado_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    expira_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    intentos = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    bloqueado_hasta = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    usado_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_codigo_whatsapp", x => x.id);
                    table.CheckConstraint("ck_codigo_whatsapp_intentos", "intentos >= 0");
                });

            migrationBuilder.CreateTable(
                name: "contacto_replica",
                schema: "notificaciones",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rol = table.Column<string>(type: "varchar(20)", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    telefono_whatsapp = table.Column<string>(type: "varchar(20)", nullable: true),
                    whatsapp_user_id = table.Column<string>(type: "varchar(128)", nullable: true),
                    whatsapp_opt_in = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    whatsapp_opt_in_fecha = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    whatsapp_baja_fecha = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    frecuencia_alertas = table.Column<string>(type: "varchar(15)", nullable: false, defaultValue: "Ninguna"),
                    version_evento = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contacto_replica", x => x.usuario_id);
                    table.CheckConstraint("ck_contacto_replica_frecuencia", "frecuencia_alertas IN ('Diaria', 'Semanal', 'Ninguna')");
                    table.CheckConstraint("ck_contacto_replica_opt_in_fecha", "whatsapp_opt_in = 0 OR whatsapp_opt_in_fecha IS NOT NULL");
                    table.CheckConstraint("ck_contacto_replica_rol", "rol IN ('Candidato', 'Reclutador', 'Administrador')");
                });

            migrationBuilder.CreateTable(
                name: "InboxState",
                schema: "notificaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiveCount = table.Column<int>(type: "int", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Consumed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Delivered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "mensaje_whatsapp_procesado",
                schema: "notificaciones",
                columns: table => new
                {
                    mensaje_id = table.Column<string>(type: "varchar(128)", nullable: false),
                    recibido_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    resultado = table.Column<string>(type: "varchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mensaje_whatsapp_procesado", x => x.mensaje_id);
                });

            migrationBuilder.CreateTable(
                name: "notificacion",
                schema: "notificaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    canal = table.Column<string>(type: "varchar(15)", nullable: false),
                    plantilla = table.Column<string>(type: "varchar(50)", nullable: false),
                    estado_envio = table.Column<string>(type: "varchar(20)", nullable: false),
                    intentos = table.Column<int>(type: "int", nullable: false),
                    creada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    evento_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    postulacion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    vacante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    referencia_folio = table.Column<string>(type: "varchar(20)", nullable: true),
                    titulo_vacante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    nombre_empresa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    estado_anterior = table.Column<string>(type: "varchar(25)", nullable: true),
                    estado_nuevo = table.Column<string>(type: "varchar(25)", nullable: true),
                    causa_cambio = table.Column<string>(type: "varchar(25)", nullable: true),
                    cambiado_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    secuencia = table.Column<int>(type: "int", nullable: true),
                    proximo_intento_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    enviada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    id_externo = table.Column<string>(type: "varchar(128)", nullable: true),
                    ultimo_error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificacion", x => x.id);
                    table.CheckConstraint("ck_notificacion_canal", "canal IN ('Web', 'Whatsapp', 'Email')");
                    table.CheckConstraint("ck_notificacion_estado_envio", "estado_envio IN ('Pendiente', 'Enviada', 'Fallida')");
                    table.CheckConstraint("ck_notificacion_intentos", "intentos >= 0");
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                schema: "notificaciones",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Delivered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "postulacion_seguimiento",
                schema: "notificaciones",
                columns: table => new
                {
                    postulacion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    candidato_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    vacante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    folio = table.Column<string>(type: "varchar(20)", nullable: false),
                    titulo_vacante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    estado_actual = table.Column<string>(type: "varchar(25)", nullable: false),
                    pendiente = table.Column<bool>(type: "bit", nullable: false),
                    ultimo_cambio_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    secuencia = table.Column<int>(type: "int", nullable: false),
                    recordatorio_10d_enviado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    recordatorio_enviado_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postulacion_seguimiento", x => x.postulacion_id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "notificaciones",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnqueueTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Headers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalSchema: "notificaciones",
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalSchema: "notificaciones",
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateIndex(
                name: "ix_alerta_vacante_enviada_alerta_boton",
                schema: "notificaciones",
                table: "alerta_vacante_enviada",
                columns: new[] { "usuario_id", "id_alerta", "boton" },
                filter: "[id_alerta] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_codigo_whatsapp_usuario",
                schema: "notificaciones",
                table: "codigo_whatsapp",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contacto_replica_empresa",
                schema: "notificaciones",
                table: "contacto_replica",
                columns: new[] { "empresa_id", "rol" });

            migrationBuilder.CreateIndex(
                name: "uq_contacto_replica_bsuid",
                schema: "notificaciones",
                table: "contacto_replica",
                column: "whatsapp_user_id",
                unique: true,
                filter: "[whatsapp_user_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_contacto_replica_telefono",
                schema: "notificaciones",
                table: "contacto_replica",
                column: "telefono_whatsapp",
                unique: true,
                filter: "[telefono_whatsapp] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                schema: "notificaciones",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "ix_notificacion_postulacion_secuencia",
                schema: "notificaciones",
                table: "notificacion",
                columns: new[] { "postulacion_id", "secuencia" },
                filter: "[postulacion_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notificacion_reintentos",
                schema: "notificaciones",
                table: "notificacion",
                columns: new[] { "estado_envio", "proximo_intento_en" });

            migrationBuilder.CreateIndex(
                name: "uq_notificacion_evento_usuario_canal",
                schema: "notificaciones",
                table: "notificacion",
                columns: new[] { "evento_id", "usuario_id", "canal" },
                unique: true,
                filter: "[evento_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                schema: "notificaciones",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                schema: "notificaciones",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "notificaciones",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true,
                filter: "[InboxMessageId] IS NOT NULL AND [InboxConsumerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                schema: "notificaciones",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true,
                filter: "[OutboxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                schema: "notificaciones",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "ix_postulacion_seguimiento_pendientes",
                schema: "notificaciones",
                table: "postulacion_seguimiento",
                columns: new[] { "pendiente", "recordatorio_10d_enviado", "ultimo_cambio_en" });

            migrationBuilder.CreateIndex(
                name: "ix_postulacion_seguimiento_vacante",
                schema: "notificaciones",
                table: "postulacion_seguimiento",
                column: "vacante_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerta_vacante_enviada",
                schema: "notificaciones");

            migrationBuilder.DropTable(
                name: "codigo_whatsapp",
                schema: "notificaciones");

            migrationBuilder.DropTable(
                name: "contacto_replica",
                schema: "notificaciones");

            migrationBuilder.DropTable(
                name: "mensaje_whatsapp_procesado",
                schema: "notificaciones");

            migrationBuilder.DropTable(
                name: "notificacion",
                schema: "notificaciones");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "notificaciones");

            migrationBuilder.DropTable(
                name: "postulacion_seguimiento",
                schema: "notificaciones");

            migrationBuilder.DropTable(
                name: "InboxState",
                schema: "notificaciones");

            migrationBuilder.DropTable(
                name: "OutboxState",
                schema: "notificaciones");
        }
    }
}
