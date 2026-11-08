using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaElectrico.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSuministroCrudAndMedidores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suministros_Cuen",
                table: "Suministros");

            migrationBuilder.DropIndex(
                name: "IX_Suministros_NumeroContrato",
                table: "Suministros");

            migrationBuilder.DropIndex(
                name: "IX_Suministros_NumeroSuministro",
                table: "Suministros");

            migrationBuilder.CreateTable(
                name: "Medidores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroSerie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TipoMedicion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaInstalacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRetiro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SuministroId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medidores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medidores_Suministros_SuministroId",
                        column: x => x.SuministroId,
                        principalTable: "Suministros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suministros_Cuen",
                table: "Suministros",
                column: "Cuen",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suministros_NumeroContrato",
                table: "Suministros",
                column: "NumeroContrato",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suministros_NumeroSuministro",
                table: "Suministros",
                column: "NumeroSuministro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medidores_NumeroSerie",
                table: "Medidores",
                column: "NumeroSerie",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medidores_SuministroId_Estado",
                table: "Medidores",
                columns: new[] { "SuministroId", "Estado" },
                unique: true,
                filter: "[Estado] = 'ACTIVO'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Medidores");

            migrationBuilder.DropIndex(
                name: "IX_Suministros_Cuen",
                table: "Suministros");

            migrationBuilder.DropIndex(
                name: "IX_Suministros_NumeroContrato",
                table: "Suministros");

            migrationBuilder.DropIndex(
                name: "IX_Suministros_NumeroSuministro",
                table: "Suministros");

            migrationBuilder.CreateIndex(
                name: "IX_Suministros_Cuen",
                table: "Suministros",
                column: "Cuen");

            migrationBuilder.CreateIndex(
                name: "IX_Suministros_NumeroContrato",
                table: "Suministros",
                column: "NumeroContrato");

            migrationBuilder.CreateIndex(
                name: "IX_Suministros_NumeroSuministro",
                table: "Suministros",
                column: "NumeroSuministro");
        }
    }
}
