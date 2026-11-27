using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTalent.Notificaciones.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregaLeidaEn_Notificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "leida_en",
                schema: "notificaciones",
                table: "notificacion",
                type: "datetime2(0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "leida_en",
                schema: "notificaciones",
                table: "notificacion");
        }
    }
}
