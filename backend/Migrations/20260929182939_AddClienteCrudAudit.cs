using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaElectrico.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteCrudAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TipoEntidad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ValorAnteriorJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ValorNuevoJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OcurridoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosAuditoria_Usuarios_ActorUsuarioId",
                        column: x => x.ActorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosAuditoria_ActorUsuarioId",
                table: "EventosAuditoria",
                column: "ActorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAuditoria_OcurridoEnUtc",
                table: "EventosAuditoria",
                column: "OcurridoEnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosAuditoria");
        }
    }
}
