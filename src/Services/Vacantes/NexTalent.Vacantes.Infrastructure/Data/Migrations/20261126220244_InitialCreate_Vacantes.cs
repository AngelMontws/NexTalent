using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTalent.Vacantes.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_Vacantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vacantes");

            migrationBuilder.CreateTable(
                name: "bitacora_moderacion",
                schema: "vacantes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    entidad_tipo = table.Column<string>(type: "varchar(20)", nullable: false),
                    entidad_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    administrador_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    accion = table.Column<string>(type: "varchar(30)", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bitacora_moderacion", x => x.id);
                    table.CheckConstraint("CK_BitacoraModeracion_Entidad", "[entidad_tipo] IN ('Vacante','Habilidad')");
                    table.CheckConstraint("CK_BitacoraModeracion_Motivo", "LEN(LTRIM(RTRIM([motivo]))) >= 5");
                });

            migrationBuilder.CreateTable(
                name: "empresa_resumen",
                schema: "vacantes",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(160)", nullable: false),
                    verificada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    indice_respuesta_pct = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    actualizado_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresa_resumen", x => x.empresa_id);
                    table.CheckConstraint("CK_EmpresaResumen_IndiceRespuesta", "[indice_respuesta_pct] BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "habilidad",
                schema: "vacantes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    nombre = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    categoria = table.Column<string>(type: "nvarchar(60)", nullable: false),
                    estado_aprobacion = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Pendiente"),
                    propuesta_por = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habilidad", x => x.id);
                    table.CheckConstraint("CK_Habilidad_EstadoAprobacion", "[estado_aprobacion] IN ('Aprobada','Pendiente','Rechazada')");
                });

            migrationBuilder.CreateTable(
                name: "InboxState",
                schema: "vacantes",
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
                name: "OutboxState",
                schema: "vacantes",
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
                name: "vacante",
                schema: "vacantes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reclutador_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    actividades = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    beneficios = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    salario_min = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    salario_max = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    tipo_salario = table.Column<string>(type: "varchar(10)", nullable: false),
                    modalidad = table.Column<string>(type: "varchar(20)", nullable: false),
                    jornada = table.Column<string>(type: "varchar(30)", nullable: false),
                    ubicacion = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    estado = table.Column<string>(type: "varchar(25)", nullable: false, defaultValue: "Borrador"),
                    publicada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    expira_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    recordatorio_27d_enviado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    version_evento = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    creada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vacante", x => x.id);
                    table.CheckConstraint("CK_Vacante_Estado", "[estado] IN ('Borrador','Publicada','OcultaPorReportes','Cerrada','Caducada')");
                    table.CheckConstraint("CK_Vacante_Modalidad", "[modalidad] IN ('Presencial','Hibrido','Remoto')");
                    table.CheckConstraint("CK_Vacante_SalarioMax_Valido", "[salario_max] >= [salario_min]");
                    table.CheckConstraint("CK_Vacante_SalarioMin_Positivo", "[salario_min] > 0");
                    table.CheckConstraint("CK_Vacante_TipoSalario", "[tipo_salario] IN ('Bruto','Neto')");
                    table.ForeignKey(
                        name: "FK_vacante_empresa_resumen_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "vacantes",
                        principalTable: "empresa_resumen",
                        principalColumn: "empresa_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "vacantes",
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
                        principalSchema: "vacantes",
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalSchema: "vacantes",
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateTable(
                name: "reporte_vacante",
                schema: "vacantes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    vacante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    motivo_tipo = table.Column<string>(type: "varchar(30)", nullable: false),
                    detalle = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    fecha = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporte_vacante", x => x.id);
                    table.CheckConstraint("CK_ReporteVacante_Motivo", "[motivo_tipo] IN ('PideDinero','VacanteFalsa','Discriminatoria','Otro')");
                    table.ForeignKey(
                        name: "FK_reporte_vacante_vacante_vacante_id",
                        column: x => x.vacante_id,
                        principalSchema: "vacantes",
                        principalTable: "vacante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vacante_habilidad",
                schema: "vacantes",
                columns: table => new
                {
                    vacante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    habilidad_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nivel = table.Column<byte>(type: "tinyint", nullable: false),
                    es_requerida = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vacante_habilidad", x => new { x.vacante_id, x.habilidad_id });
                    table.CheckConstraint("CK_VacanteHabilidad_Nivel", "[nivel] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_vacante_habilidad_habilidad_habilidad_id",
                        column: x => x.habilidad_id,
                        principalSchema: "vacantes",
                        principalTable: "habilidad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vacante_habilidad_vacante_vacante_id",
                        column: x => x.vacante_id,
                        principalSchema: "vacantes",
                        principalTable: "vacante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BitacoraModeracion_Entidad",
                schema: "vacantes",
                table: "bitacora_moderacion",
                columns: new[] { "entidad_tipo", "entidad_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "UQ_Habilidad_Nombre",
                schema: "vacantes",
                table: "habilidad",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                schema: "vacantes",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                schema: "vacantes",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                schema: "vacantes",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "vacantes",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true,
                filter: "[InboxMessageId] IS NOT NULL AND [InboxConsumerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                schema: "vacantes",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true,
                filter: "[OutboxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                schema: "vacantes",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "UQ_ReporteVacante_Vacante_Usuario",
                schema: "vacantes",
                table: "reporte_vacante",
                columns: new[] { "vacante_id", "usuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vacante_Busqueda",
                schema: "vacantes",
                table: "vacante",
                columns: new[] { "estado", "salario_min", "salario_max" })
                .Annotation("SqlServer:Include", new[] { "titulo", "empresa_id", "modalidad", "jornada", "ubicacion", "tipo_salario", "publicada_en", "expira_en" });

            migrationBuilder.CreateIndex(
                name: "IX_Vacante_Empresa_Estado",
                schema: "vacantes",
                table: "vacante",
                columns: new[] { "empresa_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Vacante_Estado_Expira",
                schema: "vacantes",
                table: "vacante",
                columns: new[] { "estado", "expira_en" });

            migrationBuilder.CreateIndex(
                name: "IX_VacanteHabilidad_Habilidad",
                schema: "vacantes",
                table: "vacante_habilidad",
                columns: new[] { "habilidad_id", "nivel" })
                .Annotation("SqlServer:Include", new[] { "vacante_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bitacora_moderacion",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "reporte_vacante",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "vacante_habilidad",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "InboxState",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "OutboxState",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "habilidad",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "vacante",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "empresa_resumen",
                schema: "vacantes");
        }
    }
}
