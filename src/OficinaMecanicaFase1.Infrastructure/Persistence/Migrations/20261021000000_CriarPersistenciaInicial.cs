using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OficinaDbContext))]
[Migration("20261021000000_CriarPersistenciaInicial")]
public sealed class CriarPersistenciaInicial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RegistrosPersistencia",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RegistrosPersistencia", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RegistrosPersistencia");
    }
}
