using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTalent.Identidad.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_Identidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identidad");

            migrationBuilder.CreateTable(
                name: "empresa",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    nombre = table.Column<string>(type: "nvarchar(160)", nullable: false),
                    rfc = table.Column<string>(type: "varchar(13)", nullable: false),
                    verificada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    estado_verificacion = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Pendiente"),
                    creada_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresa", x => x.id);
                    table.CheckConstraint("CK_Empresa_EstadoVerificacion", "[estado_verificacion] IN ('Pendiente','Aprobada','Rechazada')");
                    table.CheckConstraint("CK_Empresa_Rfc_Longitud", "LEN([rfc]) IN (12,13)");
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    email = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(512)", nullable: false),
                    telefono_whatsapp = table.Column<string>(type: "varchar(16)", nullable: true),
                    rol = table.Column<string>(type: "varchar(20)", nullable: false),
                    whatsapp_opt_in = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    whatsapp_opt_in_fecha = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    aviso_privacidad_version = table.Column<string>(type: "varchar(20)", nullable: false),
                    aviso_privacidad_aceptado_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    email_verificado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    intentos_fallidos = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    bloqueado_hasta = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id);
                    table.CheckConstraint("CK_Usuario_OptIn_Fecha", "[whatsapp_opt_in] = 0 OR [whatsapp_opt_in_fecha] IS NOT NULL");
                    table.CheckConstraint("CK_Usuario_Rol", "[rol] IN ('Candidato','Reclutador','Administrador')");
                });

            migrationBuilder.CreateTable(
                name: "candidato",
                schema: "identidad",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nombres = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    apellido_paterno = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    apellido_materno = table.Column<string>(type: "nvarchar(80)", nullable: true),
                    edad = table.Column<byte>(type: "tinyint", nullable: true),
                    ocupacion = table.Column<string>(type: "nvarchar(120)", nullable: true),
                    curp = table.Column<string>(type: "char(18)", nullable: true),
                    calle = table.Column<string>(type: "nvarchar(120)", nullable: true),
                    numero_exterior = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    colonia = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    municipio_alcaldia = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    estado_mx = table.Column<string>(type: "nvarchar(60)", nullable: true),
                    codigo_postal = table.Column<string>(type: "char(5)", nullable: true),
                    ubicacion = table.Column<string>(type: "nvarchar(120)", nullable: true),
                    salario_esperado = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    modalidad_preferida = table.Column<string>(type: "varchar(20)", nullable: true),
                    foto_url = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    cv_perfil_url = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    completitud_pct = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidato", x => x.usuario_id);
                    table.CheckConstraint("CK_Candidato_CodigoPostal", "[codigo_postal] IS NULL OR [codigo_postal] LIKE '[0-9][0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_Candidato_Completitud", "[completitud_pct] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Candidato_Edad", "[edad] IS NULL OR [edad] BETWEEN 16 AND 99");
                    table.CheckConstraint("CK_Candidato_Modalidad", "[modalidad_preferida] IS NULL OR [modalidad_preferida] IN ('Presencial','Hibrido','Remoto')");
                    table.CheckConstraint("CK_Candidato_SalarioEsperado", "[salario_esperado] IS NULL OR [salario_esperado] > 0");
                    table.ForeignKey(
                        name: "FK_candidato_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "candidato_habilidad",
                schema: "identidad",
                columns: table => new
                {
                    candidato_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    habilidad_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nivel = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidato_habilidad", x => new { x.candidato_id, x.habilidad_id });
                    table.CheckConstraint("CK_CandidatoHabilidad_Nivel", "[nivel] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_candidato_habilidad_candidato_candidato_id",
                        column: x => x.candidato_id,
                        principalSchema: "identidad",
                        principalTable: "candidato",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Candidato_Curp",
                schema: "identidad",
                table: "candidato",
                column: "curp",
                unique: true,
                filter: "[curp] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CandidatoHabilidad_Habilidad",
                schema: "identidad",
                table: "candidato_habilidad",
                columns: new[] { "habilidad_id", "nivel" })
                .Annotation("SqlServer:Include", new[] { "candidato_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Empresa_ColaFifo",
                schema: "identidad",
                table: "empresa",
                columns: new[] { "estado_verificacion", "creada_en" });

            migrationBuilder.CreateIndex(
                name: "UQ_Empresa_Rfc",
                schema: "identidad",
                table: "empresa",
                column: "rfc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Usuario_Email",
                schema: "identidad",
                table: "usuario",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Usuario_Telefono",
                schema: "identidad",
                table: "usuario",
                column: "telefono_whatsapp",
                unique: true,
                filter: "[telefono_whatsapp] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidato_habilidad",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "empresa",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "candidato",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "usuario",
                schema: "identidad");
        }
    }
}
