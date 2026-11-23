using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IncluirEmailCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Clientes",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Clientes");
        }
    }
}
