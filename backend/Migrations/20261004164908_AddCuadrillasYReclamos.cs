using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaElectrico.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCuadrillasYReclamos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CuadrillaId",
                table: "OrdenesTrabajo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cuadrillas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Zona = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CapacidadMaxima = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuadrillas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reclamos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroCaso = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SuministroId = table.Column<int>(type: "int", nullable: false),
                    PagoId = table.Column<int>(type: "int", nullable: true),
                    OrdenTrabajoId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AsignadoAUsuarioId = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Prioridad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    Resolucion = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCierreUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reclamos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reclamos_OrdenesTrabajo_OrdenTrabajoId",
                        column: x => x.OrdenTrabajoId,
                        principalTable: "OrdenesTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reclamos_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reclamos_Suministros_SuministroId",
                        column: x => x.SuministroId,
                        principalTable: "Suministros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reclamos_Usuarios_AsignadoAUsuarioId",
                        column: x => x.AsignadoAUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reclamos_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuadrillaMiembros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuadrillaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaAsignacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuadrillaMiembros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuadrillaMiembros_Cuadrillas_CuadrillaId",
                        column: x => x.CuadrillaId,
                        principalTable: "Cuadrillas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuadrillaMiembros_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReclamoHistorial",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReclamoId = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReclamoHistorial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReclamoHistorial_Reclamos_ReclamoId",
                        column: x => x.ReclamoId,
                        principalTable: "Reclamos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReclamoHistorial_Usuarios_ActorUsuarioId",
                        column: x => x.ActorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_CuadrillaId",
                table: "OrdenesTrabajo",
                column: "CuadrillaId");

            migrationBuilder.CreateIndex(
                name: "IX_CuadrillaMiembros_CuadrillaId_UsuarioId_Activo",
                table: "CuadrillaMiembros",
                columns: new[] { "CuadrillaId", "UsuarioId", "Activo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuadrillaMiembros_UsuarioId",
                table: "CuadrillaMiembros",
                column: "UsuarioId",
                unique: true,
                filter: "[Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Cuadrillas_Codigo",
                table: "Cuadrillas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReclamoHistorial_ActorUsuarioId",
                table: "ReclamoHistorial",
                column: "ActorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReclamoHistorial_ReclamoId",
                table: "ReclamoHistorial",
                column: "ReclamoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reclamos_AsignadoAUsuarioId",
                table: "Reclamos",
                column: "AsignadoAUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reclamos_CreadoPorUsuarioId",
                table: "Reclamos",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reclamos_NumeroCaso",
                table: "Reclamos",
                column: "NumeroCaso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reclamos_OrdenTrabajoId",
                table: "Reclamos",
                column: "OrdenTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reclamos_PagoId",
                table: "Reclamos",
                column: "PagoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reclamos_SuministroId",
                table: "Reclamos",
                column: "SuministroId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesTrabajo_Cuadrillas_CuadrillaId",
                table: "OrdenesTrabajo",
                column: "CuadrillaId",
                principalTable: "Cuadrillas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesTrabajo_Cuadrillas_CuadrillaId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropTable(
                name: "CuadrillaMiembros");

            migrationBuilder.DropTable(
                name: "ReclamoHistorial");

            migrationBuilder.DropTable(
                name: "Cuadrillas");

            migrationBuilder.DropTable(
                name: "Reclamos");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesTrabajo_CuadrillaId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropColumn(
                name: "CuadrillaId",
                table: "OrdenesTrabajo");
        }
    }
}
