using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExecutarServicosERegistrarConsumoReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConcluidoEmUtc",
                table: "ServicosSolicitadosOS",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcluidoPorId",
                table: "ServicosSolicitadosOS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IniciadoEmUtc",
                table: "ServicosSolicitadosOS",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IniciadoPorId",
                table: "ServicosSolicitadosOS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantidadeConsumida",
                table: "ReservasItensOrdensServico",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExecucaoIniciadaEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExecucaoIniciadaPorId",
                table: "OrdensServico",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsumosItensOrdensServico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MecanicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RegistradoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumosItensOrdensServico", x => x.Id);
                    table.CheckConstraint("CK_ConsumosItensOrdensServico_Quantidade", "[Quantidade] > 0");
                    table.ForeignKey(
                        name: "FK_ConsumosItensOrdensServico_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumosItensOrdensServico_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosItensOrdensServico_ItemEstoqueId",
                table: "ConsumosItensOrdensServico",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosItensOrdensServico_OrdemServicoId_RegistradoEmUtc",
                table: "ConsumosItensOrdensServico",
                columns: new[] { "OrdemServicoId", "RegistradoEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumosItensOrdensServico");

            migrationBuilder.DropColumn(
                name: "ConcluidoEmUtc",
                table: "ServicosSolicitadosOS");

            migrationBuilder.DropColumn(
                name: "ConcluidoPorId",
                table: "ServicosSolicitadosOS");

            migrationBuilder.DropColumn(
                name: "IniciadoEmUtc",
                table: "ServicosSolicitadosOS");

            migrationBuilder.DropColumn(
                name: "IniciadoPorId",
                table: "ServicosSolicitadosOS");

            migrationBuilder.DropColumn(
                name: "QuantidadeConsumida",
                table: "ReservasItensOrdensServico");

            migrationBuilder.DropColumn(
                name: "ExecucaoIniciadaEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "ExecucaoIniciadaPorId",
                table: "OrdensServico");
        }
    }
}
