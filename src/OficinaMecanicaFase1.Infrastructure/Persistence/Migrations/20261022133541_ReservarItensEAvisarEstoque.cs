using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReservarItensEAvisarEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReservaConfirmadaEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReservaRecusadaEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SituacaoReserva",
                table: "OrdensServico",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AvisosReservaRecusada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantidadeSolicitada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SaldoDisponivelObservado = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RegistradoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvidoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisosReservaRecusada", x => x.Id);
                    table.CheckConstraint("CK_AvisosReservaRecusada_Quantidades", "[QuantidadeSolicitada] > 0 AND [SaldoDisponivelObservado] >= 0");
                    table.ForeignKey(
                        name: "FK_AvisosReservaRecusada_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AvisosReservaRecusada_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservasItensOrdensServico_OrdemServicoId_ItemEstoqueId",
                table: "ReservasItensOrdensServico",
                columns: new[] { "OrdemServicoId", "ItemEstoqueId" },
                unique: true,
                filter: "[LiberadaEmUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosReservaRecusada_ItemEstoqueId",
                table: "AvisosReservaRecusada",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosReservaRecusada_OrdemServicoId",
                table: "AvisosReservaRecusada",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosReservaRecusada_ResolvidoEmUtc_RegistradoEmUtc",
                table: "AvisosReservaRecusada",
                columns: new[] { "ResolvidoEmUtc", "RegistradoEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvisosReservaRecusada");

            migrationBuilder.DropIndex(
                name: "IX_ReservasItensOrdensServico_OrdemServicoId_ItemEstoqueId",
                table: "ReservasItensOrdensServico");

            migrationBuilder.DropColumn(
                name: "ReservaConfirmadaEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "ReservaRecusadaEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "SituacaoReserva",
                table: "OrdensServico");
        }
    }
}
