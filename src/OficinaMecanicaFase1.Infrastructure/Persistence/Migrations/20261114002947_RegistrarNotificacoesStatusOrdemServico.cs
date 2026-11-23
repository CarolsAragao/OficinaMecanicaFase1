using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarNotificacoesStatusOrdemServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificacoesStatusOrdensServico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarcoHistoricoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroOrdemServico = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Assunto = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Corpo = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Situacao = table.Column<int>(type: "int", nullable: false),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    CriadaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProximaTentativaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessandoAteUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnviadaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimoErro = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacoesStatusOrdensServico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacoesStatusOrdensServico_MarcosOrdemServico_MarcoHistoricoId",
                        column: x => x.MarcoHistoricoId,
                        principalTable: "MarcosOrdemServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificacoesStatusOrdensServico_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesStatusOrdensServico_MarcoHistoricoId",
                table: "NotificacoesStatusOrdensServico",
                column: "MarcoHistoricoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesStatusOrdensServico_OrdemServicoId",
                table: "NotificacoesStatusOrdensServico",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesStatusOrdensServico_Situacao_ProximaTentativaEmUtc",
                table: "NotificacoesStatusOrdensServico",
                columns: new[] { "Situacao", "ProximaTentativaEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificacoesStatusOrdensServico");
        }
    }
}
