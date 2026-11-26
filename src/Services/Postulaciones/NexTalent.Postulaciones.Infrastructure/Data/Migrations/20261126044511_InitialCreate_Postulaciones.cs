using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTalent.Postulaciones.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_Postulaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "postulaciones");

            migrationBuilder.CreateSequence(
                name: "folio_seq",
                schema: "postulaciones");

            migrationBuilder.CreateTable(
                name: "candidato_replica",
                schema: "postulaciones",
                columns: table => new
                {
                    candidato_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tiene_nombre = table.Column<bool>(type: "bit", nullable: false),
                    tiene_contacto = table.Column<bool>(type: "bit", nullable: false),
                    version_evento = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidato_replica", x => x.candidato_id);
                });

            migrationBuilder.CreateTable(
                name: "vacante_replica",
                schema: "postulaciones",
                columns: table => new
                {
                    vacante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    nombre_empresa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    salario_min = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    salario_max = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    tipo_salario = table.Column<string>(type: "varchar(10)", nullable: true),
                    expira_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    estado = table.Column<string>(type: "varchar(25)", nullable: false),
                    actualizada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    version_evento = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vacante_replica", x => x.vacante_id);
                    table.CheckConstraint("ck_vacante_replica_estado", "estado IN ('Activa', 'Cerrada')");
                });

            migrationBuilder.CreateTable(
                name: "candidato_habilidad_replica",
                schema: "postulaciones",
                columns: table => new
                {
                    candidato_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    habilidad_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nivel = table.Column<string>(type: "varchar(12)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidato_habilidad_replica", x => new { x.candidato_id, x.habilidad_id });
                    table.CheckConstraint("ck_candidato_habilidad_replica_nivel", "nivel IN ('Basico', 'Intermedio', 'Avanzado')");
                    table.ForeignKey(
                        name: "fk_candidato_habilidad_replica_candidato",
                        column: x => x.candidato_id,
                        principalSchema: "postulaciones",
                        principalTable: "candidato_replica",
                        principalColumn: "candidato_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "postulacion",
                schema: "postulaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    folio = table.Column<string>(type: "varchar(20)", nullable: false),
                    candidato_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    vacante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    coincidencia_pct = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    deseables_cumplidas = table.Column<int>(type: "int", nullable: false),
                    creada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    canal_origen = table.Column<string>(type: "varchar(15)", nullable: false),
                    estado_actual = table.Column<string>(type: "varchar(25)", nullable: false),
                    etapa_actualizada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    recordatorio_10d_enviado = table.Column<bool>(type: "bit", nullable: false),
                    version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postulacion", x => x.id);
                    table.CheckConstraint("ck_postulacion_canal_origen", "canal_origen IN ('Web', 'WhatsApp')");
                    table.CheckConstraint("ck_postulacion_coincidencia_pct", "coincidencia_pct BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_postulacion_deseables_cumplidas", "deseables_cumplidas >= 0");
                    table.CheckConstraint("ck_postulacion_estado_actual", "estado_actual IN ('Enviada', 'Vista', 'EnRevision', 'Entrevista', 'OfertaContratado', 'NoSeleccionado')");
                    table.ForeignKey(
                        name: "fk_postulacion_vacante_replica",
                        column: x => x.vacante_id,
                        principalSchema: "postulaciones",
                        principalTable: "vacante_replica",
                        principalColumn: "vacante_id");
                });

            migrationBuilder.CreateTable(
                name: "vacante_habilidad_replica",
                schema: "postulaciones",
                columns: table => new
                {
                    vacante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    habilidad_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nivel = table.Column<string>(type: "varchar(12)", nullable: false),
                    requerida = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vacante_habilidad_replica", x => new { x.vacante_id, x.habilidad_id });
                    table.CheckConstraint("ck_vacante_habilidad_replica_nivel", "nivel IN ('Basico', 'Intermedio', 'Avanzado')");
                    table.ForeignKey(
                        name: "fk_vacante_habilidad_replica_vacante",
                        column: x => x.vacante_id,
                        principalSchema: "postulaciones",
                        principalTable: "vacante_replica",
                        principalColumn: "vacante_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historial_estado",
                schema: "postulaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    secuencia = table.Column<int>(type: "int", nullable: false),
                    estado_anterior = table.Column<string>(type: "varchar(25)", nullable: true),
                    estado_nuevo = table.Column<string>(type: "varchar(25)", nullable: false),
                    causa = table.Column<string>(type: "varchar(25)", nullable: false),
                    cambiado_por = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    fecha = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    postulacion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historial_estado", x => x.id);
                    table.CheckConstraint("ck_historial_estado_causa", "causa IN ('Creacion', 'PerfilAbierto', 'Avance', 'Descarte', 'VacanteCerrada', 'Reactivacion')");
                    table.CheckConstraint("ck_historial_estado_motivo_reactivacion", "estado_anterior IS NULL OR estado_anterior <> 'NoSeleccionado' OR LEN(LTRIM(motivo)) > 0");
                    table.CheckConstraint("ck_historial_estado_nuevo", "estado_nuevo IN ('Enviada', 'Vista', 'EnRevision', 'Entrevista', 'OfertaContratado', 'NoSeleccionado')");
                    table.ForeignKey(
                        name: "fk_historial_estado_postulacion",
                        column: x => x.postulacion_id,
                        principalSchema: "postulaciones",
                        principalTable: "postulacion",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "uq_historial_estado_postulacion_secuencia",
                schema: "postulaciones",
                table: "historial_estado",
                columns: new[] { "postulacion_id", "secuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_postulacion_vacante_coincidencia",
                schema: "postulaciones",
                table: "postulacion",
                columns: new[] { "vacante_id", "coincidencia_pct", "deseables_cumplidas" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "uq_postulacion_candidato_vacante",
                schema: "postulaciones",
                table: "postulacion",
                columns: new[] { "candidato_id", "vacante_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_postulacion_folio",
                schema: "postulaciones",
                table: "postulacion",
                column: "folio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vacante_replica_empresa",
                schema: "postulaciones",
                table: "vacante_replica",
                column: "empresa_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidato_habilidad_replica",
                schema: "postulaciones");

            migrationBuilder.DropTable(
                name: "historial_estado",
                schema: "postulaciones");

            migrationBuilder.DropTable(
                name: "vacante_habilidad_replica",
                schema: "postulaciones");

            migrationBuilder.DropTable(
                name: "candidato_replica",
                schema: "postulaciones");

            migrationBuilder.DropTable(
                name: "postulacion",
                schema: "postulaciones");

            migrationBuilder.DropTable(
                name: "vacante_replica",
                schema: "postulaciones");

            migrationBuilder.DropSequence(
                name: "folio_seq",
                schema: "postulaciones");
        }
    }
}
