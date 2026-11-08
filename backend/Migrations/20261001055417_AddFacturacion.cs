using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaElectrico.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SistemaOrigen = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IdentificadorExterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroFactura = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NombreEmisor = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RucEmisor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Moneda = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PeriodoDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodoHasta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetodoLectura = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LecturaAnterior = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    LecturaActual = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    ConsumoFacturado = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    UnidadConsumo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    SubtotalServicioElectrico = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    SubtotalAlumbradoPublico = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    OtrosCargos = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    SaldoAnterior = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    RecaudacionTerceros = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Subsidios = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalFactura = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    SaldoPendiente = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    TieneDiferencia = table.Column<bool>(type: "bit", nullable: false),
                    SincronizadaEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SuministroId = table.Column<int>(type: "int", nullable: false),
                    MedidorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facturas_Medidores_MedidorId",
                        column: x => x.MedidorId,
                        principalTable: "Medidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Facturas_Suministros_SuministroId",
                        column: x => x.SuministroId,
                        principalTable: "Suministros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacturaLineas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacturaId = table.Column<int>(type: "int", nullable: false),
                    NumeroLinea = table.Column<int>(type: "int", nullable: false),
                    CodigoOriginal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescripcionOriginal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    EsIngresoElectrico = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaLineas_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacturaLineas_FacturaId_NumeroLinea",
                table: "FacturaLineas",
                columns: new[] { "FacturaId", "NumeroLinea" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_MedidorId",
                table: "Facturas",
                column: "MedidorId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_NumeroFactura",
                table: "Facturas",
                column: "NumeroFactura");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_SistemaOrigen_IdentificadorExterno",
                table: "Facturas",
                columns: new[] { "SistemaOrigen", "IdentificadorExterno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_SuministroId",
                table: "Facturas",
                column: "SuministroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturaLineas");

            migrationBuilder.DropTable(
                name: "Facturas");
        }
    }
}
