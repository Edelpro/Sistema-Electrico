using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaElectrico.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSuministros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Suministros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroSuministro = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NumeroContrato = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Cuen = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DireccionServicio = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Canton = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Parroquia = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PlanGeocodigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodigoTarifa = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    NombreTarifa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EstadoServicio = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClienteTitularId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suministros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suministros_Clientes_ClienteTitularId",
                        column: x => x.ClienteTitularId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suministros_ClienteTitularId",
                table: "Suministros",
                column: "ClienteTitularId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Suministros");
        }
    }
}
