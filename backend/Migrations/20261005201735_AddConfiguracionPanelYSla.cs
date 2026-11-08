using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SistemaElectrico.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionPanelYSla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreEmpresa = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Ruc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CorreoSoporte = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TelefonoSoporte = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ZonaHoraria = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TamanoMaximoEvidenciaMb = table.Column<int>(type: "int", nullable: false),
                    NotificacionesCorreoActivas = table.Column<bool>(type: "bit", nullable: false),
                    NotificacionesWhatsAppActivas = table.Column<bool>(type: "bit", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesSistema", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesSistema_Usuarios_ActualizadoPorUsuarioId",
                        column: x => x.ActualizadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PoliticasSla",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    TipoOrden = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipoZona = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HorasObjetivo = table.Column<int>(type: "int", nullable: false),
                    HoraInicioLaboral = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFinLaboral = table.Column<TimeSpan>(type: "time", nullable: false),
                    IncluyeFinDeSemana = table.Column<bool>(type: "bit", nullable: false),
                    VigenteDesdeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VigenteHastaUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasSla", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoliticasSla_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionesSistema",
                columns: new[] { "Id", "ActualizadoPorUsuarioId", "CorreoSoporte", "Direccion", "FechaActualizacionUtc", "NombreEmpresa", "NotificacionesCorreoActivas", "NotificacionesWhatsAppActivas", "Ruc", "TamanoMaximoEvidenciaMb", "TelefonoSoporte", "ZonaHoraria" },
                values: new object[] { 1, null, "", "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Empresa Eléctrica", false, false, "", 5, "", "America/Guayaquil" });

            migrationBuilder.InsertData(
                table: "PoliticasSla",
                columns: new[] { "Id", "Activa", "CreadoPorUsuarioId", "HoraFinLaboral", "HoraInicioLaboral", "HorasObjetivo", "IncluyeFinDeSemana", "Motivo", "TipoOrden", "TipoZona", "Version", "VigenteDesdeUtc", "VigenteHastaUtc" },
                values: new object[,]
                {
                    { 1, true, null, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 8, 0, 0, 0), 10, false, "Política inicial según referencia del SRS.", "RECONEXION", "URBANA", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, true, null, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 8, 0, 0, 0), 24, false, "Política inicial según referencia del SRS.", "RECONEXION", "RURAL", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesSistema_ActualizadoPorUsuarioId",
                table: "ConfiguracionesSistema",
                column: "ActualizadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticasSla_CreadoPorUsuarioId",
                table: "PoliticasSla",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticasSla_TipoOrden_TipoZona_Activa",
                table: "PoliticasSla",
                columns: new[] { "TipoOrden", "TipoZona", "Activa" },
                unique: true,
                filter: "[Activa] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticasSla_TipoOrden_TipoZona_Version",
                table: "PoliticasSla",
                columns: new[] { "TipoOrden", "TipoZona", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesSistema");

            migrationBuilder.DropTable(
                name: "PoliticasSla");
        }
    }
}
