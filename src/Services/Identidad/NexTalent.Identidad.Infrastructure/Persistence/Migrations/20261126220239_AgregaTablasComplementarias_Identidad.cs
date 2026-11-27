using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTalent.Identidad.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaTablasComplementarias_Identidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bitacora_moderacion_empresa",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    administrador_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    decision = table.Column<string>(type: "varchar(20)", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bitacora_moderacion_empresa", x => x.id);
                    table.CheckConstraint("CK_BitacoraEmpresa_Decision", "[decision] IN ('Aprobada','Rechazada')");
                    table.CheckConstraint("CK_BitacoraEmpresa_Motivo", "LEN(LTRIM(RTRIM([motivo]))) >= 5");
                    table.ForeignKey(
                        name: "FK_bitacora_moderacion_empresa_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "identidad",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bitacora_moderacion_empresa_usuario_administrador_id",
                        column: x => x.administrador_id,
                        principalSchema: "identidad",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experiencia_laboral",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    candidato_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    puesto = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    empresa_nombre = table.Column<string>(type: "nvarchar(160)", nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    es_actual = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiencia_laboral", x => x.id);
                    table.CheckConstraint("CK_ExperienciaLaboral_Fechas", "[es_actual] = 1 OR ([fecha_fin] IS NOT NULL AND [fecha_fin] >= [fecha_inicio])");
                    table.ForeignKey(
                        name: "FK_experiencia_laboral_candidato_candidato_id",
                        column: x => x.candidato_id,
                        principalSchema: "identidad",
                        principalTable: "candidato",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "formacion_academica",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    candidato_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    institucion = table.Column<string>(type: "nvarchar(160)", nullable: false),
                    titulo_grado = table.Column<string>(type: "nvarchar(160)", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    es_actual = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formacion_academica", x => x.id);
                    table.CheckConstraint("CK_FormacionAcademica_Fechas", "[es_actual] = 1 OR ([fecha_fin] IS NOT NULL AND [fecha_fin] >= [fecha_inicio])");
                    table.ForeignKey(
                        name: "FK_formacion_academica_candidato_candidato_id",
                        column: x => x.candidato_id,
                        principalSchema: "identidad",
                        principalTable: "candidato",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reclutador",
                schema: "identidad",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nombre_contacto = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    aprobado_empresa = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reclutador", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_reclutador_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "identidad",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reclutador_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token_hash = table.Column<string>(type: "varchar(128)", nullable: false),
                    expira_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    revocado_en = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_token_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bitacora_moderacion_empresa_administrador_id",
                schema: "identidad",
                table: "bitacora_moderacion_empresa",
                column: "administrador_id");

            migrationBuilder.CreateIndex(
                name: "IX_BitacoraEmpresa_Empresa_Fecha",
                schema: "identidad",
                table: "bitacora_moderacion_empresa",
                columns: new[] { "empresa_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ExperienciaLaboral_Candidato",
                schema: "identidad",
                table: "experiencia_laboral",
                column: "candidato_id");

            migrationBuilder.CreateIndex(
                name: "IX_FormacionAcademica_Candidato",
                schema: "identidad",
                table: "formacion_academica",
                column: "candidato_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reclutador_Empresa",
                schema: "identidad",
                table: "reclutador",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_Usuario_Expira",
                schema: "identidad",
                table: "refresh_token",
                columns: new[] { "usuario_id", "expira_en" });

            migrationBuilder.CreateIndex(
                name: "UQ_RefreshToken_Hash",
                schema: "identidad",
                table: "refresh_token",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bitacora_moderacion_empresa",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "experiencia_laboral",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "formacion_academica",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "reclutador",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "refresh_token",
                schema: "identidad");
        }
    }
}
