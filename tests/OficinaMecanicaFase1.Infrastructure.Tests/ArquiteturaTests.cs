using System.Reflection;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class ArquiteturaTests
{
    [Fact]
    public void CamadasInternasNaoDependemDeApiInfraestruturaOuFrameworksExternos()
    {
        var camadasInternas = new[]
        {
            typeof(OrdemServico).Assembly,
            typeof(OrdemServicoService).Assembly
        };
        string[] dependenciasProibidas =
        [
            "OficinaMecanicaFase1.Api",
            "OficinaMecanicaFase1.Infrastructure",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore"
        ];

        foreach (var camada in camadasInternas)
        {
            var referenciaIndevida = camada.GetReferencedAssemblies()
                .Select(referencia => referencia.Name)
                .FirstOrDefault(nome => nome is not null && dependenciasProibidas.Any(
                    proibida => nome.StartsWith(proibida, StringComparison.Ordinal)));

            Assert.True(referenciaIndevida is null,
                $"{camada.GetName().Name} não deve depender de {referenciaIndevida}.");
        }

        Assert.Contains(typeof(OrdemServico).Assembly.GetName().Name,
            typeof(OrdemServicoService).Assembly.GetReferencedAssemblies().Select(referencia => referencia.Name));
    }
}
