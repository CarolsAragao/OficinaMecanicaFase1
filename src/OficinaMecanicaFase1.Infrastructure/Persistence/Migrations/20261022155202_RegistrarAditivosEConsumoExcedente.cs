using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarAditivosEConsumoExcedente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aditivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ServicoCatalogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NomeServico = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NomeItem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecoServico = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecoItem = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginalPodeProsseguir = table.Column<bool>(type: "bit", nullable: false),
                    IdentificadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Situacao = table.Column<int>(type: "int", nullable: false),
                    DecididoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoRejeicao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SituacaoReserva = table.Column<int>(type: "int", nullable: false),
                    Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aditivos", x => x.Id);
                    table.CheckConstraint("CK_Aditivos_Quantidade", "([ItemEstoqueId] IS NULL AND [Quantidade] = 0) OR ([ItemEstoqueId] IS NOT NULL AND [Quantidade] > 0)");
                    table.CheckConstraint("CK_Aditivos_Valores", "[PrecoServico] >= 0 AND [PrecoItem] >= 0 AND [Total] >= 0");
                    table.ForeignKey(
                        name: "FK_Aditivos_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aditivos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aditivos_ServicosCatalogo_ServicoCatalogoId",
                        column: x => x.ServicoCatalogoId,
                        principalTable: "ServicosCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aditivos_ItemEstoqueId",
                table: "Aditivos",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Aditivos_OrdemServicoId_CriadoEmUtc",
                table: "Aditivos",
                columns: new[] { "OrdemServicoId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Aditivos_ServicoCatalogoId",
                table: "Aditivos",
                column: "ServicoCatalogoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aditivos");
        }
    }
}
