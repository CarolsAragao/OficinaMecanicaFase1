using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    public partial class CriarCatalogoEEntradasEstoque : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoFisico = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SaldoReservado = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensEstoque", x => x.Id);
                    table.CheckConstraint("CK_ItensEstoque_PecaInteira", "[Tipo] <> 1 OR ([SaldoFisico] = FLOOR([SaldoFisico]) AND [SaldoReservado] = FLOOR([SaldoReservado]))");
                    table.CheckConstraint("CK_ItensEstoque_PrecoUnitario", "[PrecoUnitario] >= 0");
                    table.CheckConstraint("CK_ItensEstoque_Saldos", "[SaldoFisico] >= 0 AND [SaldoReservado] >= 0 AND [SaldoReservado] <= [SaldoFisico]");
                    table.CheckConstraint("CK_ItensEstoque_Tipo", "[Tipo] IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "ServicosCatalogo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrecoBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicosCatalogo", x => x.Id);
                    table.CheckConstraint("CK_ServicosCatalogo_PrecoBase", "[PrecoBase] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "EntradasEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RegistradaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradasEstoque", x => x.Id);
                    table.CheckConstraint("CK_EntradasEstoque_Quantidade", "[Quantidade] > 0");
                    table.ForeignKey(
                        name: "FK_EntradasEstoque_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntradasEstoque_ItemEstoqueId_RegistradaEmUtc",
                table: "EntradasEstoque",
                columns: new[] { "ItemEstoqueId", "RegistradaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensEstoque_Ativo_Nome",
                table: "ItensEstoque",
                columns: new[] { "Ativo", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicosCatalogo_Ativo_Nome",
                table: "ServicosCatalogo",
                columns: new[] { "Ativo", "Nome" });
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntradasEstoque");

            migrationBuilder.DropTable(
                name: "ServicosCatalogo");

            migrationBuilder.DropTable(
                name: "ItensEstoque");
        }
    }
}
