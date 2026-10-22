using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Api.OrdensServico;

public static class RotasGestaoOrdensServico
{
    public static IEndpointRouteBuilder MapearGestaoOrdensServico(this IEndpointRouteBuilder app)
    {
        var gestao = app.MapGroup("/api/administracao/ordens-servico")
            .WithTags("Gestão de ordens de serviço")
            .RequireAuthorization("Administradores");

        gestao.MapGet("", ListarAsync)
            .WithName("ListarOrdensServicoAdministrativas")
            .WithSummary("Lista OS por status, da mais recente para a mais antiga, com paginação.")
            .Produces<PaginaOrdensServicoResponse>()
            .ProducesValidationProblem().Produces(401).Produces(403);

        gestao.MapGet("/indicadores/tempo-medio", ObterTempoMedioAsync)
            .WithName("ObterTempoMedioPorServico")
            .WithSummary("Média do tempo decorrido por serviço concluído, incluindo pausas e impedimentos.")
            .Produces<IndicadorTempoMedioResponse>()
            .Produces(401).Produces(403);

        gestao.MapGet("/{numero:long}", ObterAsync)
            .WithName("ObterOrdemServicoAdministrativa")
            .WithSummary("Detalha uma OS para o Administrador.")
            .Produces<OrdemServicoResponse>()
            .Produces(404).Produces(401).Produces(403);

        return app;
    }

    private static async Task<IResult> ListarAsync(string? status, int? pagina,
        int? tamanhoPagina, ConsultaGestaoOrdensServico consulta, CancellationToken cancelamento)
    {
        StatusOrdemServico? filtro = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusOrdemServico>(status, true, out var convertido) ||
                !Enum.IsDefined(convertido))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Informe um dos seis status oficiais da OS."]
                });
            filtro = convertido;
        }

        if (pagina is < 1 || pagina is > 21474836 || tamanhoPagina is < 1 or > 100)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["paginacao"] = ["Página deve estar entre 1 e 21474836; tamanhoPagina entre 1 e 100."]
            });

        var resultado = await consulta.ListarAsync(filtro, pagina ?? 1, tamanhoPagina ?? 20, cancelamento);
        return Results.Ok(PaginaOrdensServicoResponse.De(resultado));
    }

    private static async Task<IResult> ObterAsync(long numero,
        GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        var ordem = await gerenciador.ObterAsync(numero, cancelamento);
        return ordem is null ? Results.NotFound() : Results.Ok(OrdemServicoResponse.De(ordem));
    }

    private static async Task<IResult> ObterTempoMedioAsync(
        ConsultaGestaoOrdensServico consulta, CancellationToken cancelamento) =>
        Results.Ok(IndicadorTempoMedioResponse.De(
            await consulta.ObterIndicadorAsync(cancelamento)));
}
