using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarDecisaoOrcamentoEAcessoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CodigoAcessoEmitidoEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "CodigoAcessoHash",
                table: "OrdensServico",
                type: "varbinary(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EncerradaPorRejeicaoEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoEncerramento",
                table: "OrdensServico",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroEmissoesCodigoAcesso",
                table: "OrdensServico",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OrcamentoAprovadoEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Versao",
                table: "OrdensServico",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DecididoEmUtc",
                table: "Orcamentos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Decisao",
                table: "Orcamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRejeicao",
                table: "Orcamentos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Versao",
                table: "Orcamentos",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "EmissoesCodigoAcessoOS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroEmissao = table.Column<int>(type: "int", nullable: false),
                    AtendenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmitidoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmissoesCodigoAcessoOS", x => x.Id);
                    table.CheckConstraint("CK_EmissoesCodigoAcessoOS_Numero", "[NumeroEmissao] > 0");
                    table.ForeignKey(
                        name: "FK_EmissoesCodigoAcessoOS_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservasItensOrdensServico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReservadaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LiberadaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasItensOrdensServico", x => x.Id);
                    table.CheckConstraint("CK_ReservasItensOrdensServico_Quantidade", "[Quantidade] > 0");
                    table.ForeignKey(
                        name: "FK_ReservasItensOrdensServico_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasItensOrdensServico_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orcamentos_Decisao",
                table: "Orcamentos",
                sql: "[Decisao] IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_EmissoesCodigoAcessoOS_OrdemServicoId_NumeroEmissao",
                table: "EmissoesCodigoAcessoOS",
                columns: new[] { "OrdemServicoId", "NumeroEmissao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservasItensOrdensServico_ItemEstoqueId",
                table: "ReservasItensOrdensServico",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasItensOrdensServico_OrdemServicoId_LiberadaEmUtc",
                table: "ReservasItensOrdensServico",
                columns: new[] { "OrdemServicoId", "LiberadaEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmissoesCodigoAcessoOS");

            migrationBuilder.DropTable(
                name: "ReservasItensOrdensServico");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orcamentos_Decisao",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "CodigoAcessoEmitidoEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "CodigoAcessoHash",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "EncerradaPorRejeicaoEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "MotivoEncerramento",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "NumeroEmissoesCodigoAcesso",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "OrcamentoAprovadoEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "Versao",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "DecididoEmUtc",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "Decisao",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "MotivoRejeicao",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "Versao",
                table: "Orcamentos");
        }
    }
}
