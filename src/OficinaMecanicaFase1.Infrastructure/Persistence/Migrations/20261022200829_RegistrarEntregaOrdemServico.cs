using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarEntregaOrdemServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EntregueEmUtc",
                table: "OrdensServico",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntreguePorId",
                table: "OrdensServico",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntregueEmUtc",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "EntreguePorId",
                table: "OrdensServico");
        }
    }
}
