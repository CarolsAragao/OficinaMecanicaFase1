using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriarDiagnosticosEOrcamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiagnosticoDescricao",
                table: "OrdensServico",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DiagnosticoIniciadoEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DiagnosticoIniciadoPorId",
                table: "OrdensServico",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DiagnosticoRegistradoEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DiagnosticoRegistradoPorId",
                table: "OrdensServico",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Orcamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeradoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TotalServicos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItens = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orcamentos", x => x.Id);
                    table.CheckConstraint("CK_Orcamentos_Valores", "[TotalServicos] >= 0 AND [TotalItens] >= 0 AND [Total] = [TotalServicos] + [TotalItens]");
                    table.ForeignKey(
                        name: "FK_Orcamentos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LinhasOrcamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrcamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhasOrcamento", x => x.Id);
                    table.CheckConstraint("CK_LinhasOrcamento_Tipo", "[Tipo] IN (1, 2)");
                    table.CheckConstraint("CK_LinhasOrcamento_Valores", "[Quantidade] > 0 AND [PrecoUnitario] >= 0 AND [ValorTotal] >= 0");
                    table.ForeignKey(
                        name: "FK_LinhasOrcamento_Orcamentos_OrcamentoId",
                        column: x => x.OrcamentoId,
                        principalTable: "Orcamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinhasOrcamento_OrcamentoId_Tipo_ReferenciaId",
                table: "LinhasOrcamento",
                columns: new[] { "OrcamentoId", "Tipo", "ReferenciaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_OrdemServicoId",
                table: "Orcamentos",
                column: "OrdemServicoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinhasOrcamento");

            migrationBuilder.DropTable(
                name: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "DiagnosticoDescricao",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "DiagnosticoIniciadoEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "DiagnosticoIniciadoPorId",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "DiagnosticoRegistradoEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "DiagnosticoRegistradoPorId",
                table: "OrdensServico");
        }
    }
}
