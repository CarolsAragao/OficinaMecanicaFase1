using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class PersistenciaSqlServerTests
{
    [Fact]
    public async Task MigrationERegistroPersistemEmBancoIsolado()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();

        var connection = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaPersistencia_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        var id = Guid.NewGuid();

        await using (var db = new OficinaDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.RegistrosPersistencia.Add(new RegistroPersistencia
            {
                Id = id,
                Descricao = "Conexão verificada",
                CriadoEm = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using (var db = new OficinaDbContext(options))
        {
            var registro = await db.RegistrosPersistencia.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("Conexão verificada", registro.Descricao);
            Assert.Contains("20261021000000_CriarPersistenciaInicial", await db.Database.GetAppliedMigrationsAsync());
        }
    }
}
