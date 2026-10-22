using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Application.Catalogo;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Infrastructure.Identity;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Oficina");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Defina ConnectionStrings__Oficina para conectar ao SQL Server.");
        }

        services.AddDbContext<OficinaDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddScoped<IRepositorioCadastros, RepositorioCadastros>();
        services.AddScoped<GerenciadorCadastros>();
        services.AddScoped<IRepositorioCatalogo, RepositorioCatalogo>();
        services.AddScoped<GerenciadorCatalogo>();
        services.AddScoped<IRepositorioOrdensServico, RepositorioOrdensServico>();
        services.AddScoped<GerenciadorOrdensServico>();
        services.AddScoped<GerenciadorDecisaoOrcamento>();
        services.AddScoped<IRepositorioReservas, RepositorioReservas>();
        services.AddScoped<GerenciadorReservas>();

        services.AddDbContext<IdentidadeDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql
                .MigrationsHistoryTable("__HistoricoMigrationsIdentidade")
                .EnableRetryOnFailure()));

        return services;
    }
}
