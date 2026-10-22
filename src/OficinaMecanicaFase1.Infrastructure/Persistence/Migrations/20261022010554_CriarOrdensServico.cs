using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    public partial class CriarOrdensServico : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdensServico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CriadaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensServico", x => x.Id);
                    table.CheckConstraint("CK_OrdensServico_Numero", "[Numero] > 0");
                    table.CheckConstraint("CK_OrdensServico_Status", "[Status] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_OrdensServico_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdensServico_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensNecessariosOS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SaldoDisponivelConsultado = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensNecessariosOS", x => x.Id);
                    table.CheckConstraint("CK_ItensNecessariosOS_Disponibilidade", "[SaldoDisponivelConsultado] >= 0");
                    table.CheckConstraint("CK_ItensNecessariosOS_Quantidade", "[Quantidade] > 0");
                    table.ForeignKey(
                        name: "FK_ItensNecessariosOS_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensNecessariosOS_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarcosOrdemServico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegistradoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarcosOrdemServico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarcosOrdemServico_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicosSolicitadosOS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicoCatalogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicosSolicitadosOS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicosSolicitadosOS_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServicosSolicitadosOS_ServicosCatalogo_ServicoCatalogoId",
                        column: x => x.ServicoCatalogoId,
                        principalTable: "ServicosCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensNecessariosOS_ItemEstoqueId",
                table: "ItensNecessariosOS",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensNecessariosOS_OrdemServicoId_ItemEstoqueId",
                table: "ItensNecessariosOS",
                columns: new[] { "OrdemServicoId", "ItemEstoqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarcosOrdemServico_OrdemServicoId_RegistradoEmUtc",
                table: "MarcosOrdemServico",
                columns: new[] { "OrdemServicoId", "RegistradoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_ClienteId",
                table: "OrdensServico",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_Numero",
                table: "OrdensServico",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_Status_CriadaEmUtc",
                table: "OrdensServico",
                columns: new[] { "Status", "CriadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_VeiculoId",
                table: "OrdensServico",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicosSolicitadosOS_OrdemServicoId_ServicoCatalogoId",
                table: "ServicosSolicitadosOS",
                columns: new[] { "OrdemServicoId", "ServicoCatalogoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicosSolicitadosOS_ServicoCatalogoId",
                table: "ServicosSolicitadosOS",
                column: "ServicoCatalogoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensNecessariosOS");

            migrationBuilder.DropTable(
                name: "MarcosOrdemServico");

            migrationBuilder.DropTable(
                name: "ServicosSolicitadosOS");

            migrationBuilder.DropTable(
                name: "OrdensServico");
        }
    }
}
