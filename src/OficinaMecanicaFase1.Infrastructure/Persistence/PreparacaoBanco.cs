using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanicaFase1.Infrastructure.Identity;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public static class PreparacaoBanco
{
    public static async Task MigrarAsync(IServiceProvider servicos)
    {
        var oficina = servicos.GetRequiredService<OficinaDbContext>();
        await oficina.Database.MigrateAsync();

        var identidade = servicos.GetRequiredService<IdentidadeDbContext>();
        await identidade.Database.MigrateAsync();
    }
}
