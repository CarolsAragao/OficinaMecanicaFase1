using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Api.OrdensServico;

public static class RotasOrdensServico
{
    public static IEndpointRouteBuilder MapearOrdensServico(this IEndpointRouteBuilder app)
    {
        var ordens = app.MapGroup("/api/ordens-servico")
            .WithTags("Ordens de serviço")
            .RequireAuthorization("EquipeInterna");

        ordens.MapPost("", CriarAsync)
            .WithName("AbrirOrdemServico")
            .WithSummary("Abre OS Recebida com veículo próprio, serviços ativos e itens necessários; consulta disponibilidade sem reservar.")
            .RequireAuthorization("Cadastros")
            .Produces<OrdemServicoResponse>(201)
            .ProducesValidationProblem().Produces(401).Produces(403).ProducesProblem(409);

        ordens.MapGet("/{numero:long}", ObterAsync)
            .WithName("ObterOrdemServico")
            .Produces<OrdemServicoResponse>()
            .Produces(404).Produces(401).Produces(403);

        ordens.MapPost("/{numero:long}/servicos", AdicionarServicoAsync)
            .WithName("AdicionarServicoSolicitado")
            .WithSummary("Adiciona um serviço ativo antes da aprovação do orçamento.")
            .RequireAuthorization("AtendentesEMecanicos")
            .Produces<OrdemServicoResponse>()
            .ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        ordens.MapPost("/{numero:long}/itens", AdicionarItemAsync)
            .WithName("AdicionarItemNecessario")
            .WithSummary("Adiciona item ativo e consulta saldo disponível, sem reservar nem baixar estoque.")
            .RequireAuthorization("AtendentesEMecanicos")
            .Produces<OrdemServicoResponse>()
            .ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        return app;
    }

    private static async Task<IResult> CriarAsync(
        CriarOrdemServicoRequest entrada, GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        var servicos = entrada.ServicosIds ?? [];
        var itens = entrada.ItensNecessarios ?? [];
        var resultado = await gerenciador.CriarAsync(
            entrada.ClienteId, entrada.VeiculoId, servicos,
            itens.Select(item => (item.ItemEstoqueId, item.Quantidade)).ToArray(), cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Created($"/api/ordens-servico/{resultado.Valor!.Numero}", OrdemServicoResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    private static async Task<IResult> ObterAsync(
        long numero, GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        var ordem = await gerenciador.ObterAsync(numero, cancelamento);
        return ordem is null ? Results.NotFound() : Results.Ok(OrdemServicoResponse.De(ordem));
    }

    private static async Task<IResult> AdicionarServicoAsync(
        long numero, AdicionarServicoSolicitadoRequest entrada,
        GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        var resultado = await gerenciador.AdicionarServicoAsync(numero, entrada.ServicoCatalogoId, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private static async Task<IResult> AdicionarItemAsync(
        long numero, AdicionarItemNecessarioRequest entrada,
        GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        var resultado = await gerenciador.AdicionarItemAsync(
            numero, entrada.ItemEstoqueId, entrada.Quantidade, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private static IResult ResponderErro<T>(ResultadoOrdemServico<T> resultado) => resultado.Erro switch
    {
        ErroOrdemServico.Invalido => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [resultado.Campo ?? "ordemServico"] = [resultado.Mensagem ?? "Dados inválidos."]
        }),
        ErroOrdemServico.NaoEncontrado => Results.NotFound(),
        ErroOrdemServico.Conflito => Results.Problem(resultado.Mensagem, statusCode: 409),
        _ => Results.Problem("Não foi possível processar a ordem de serviço.", statusCode: 500)
    };
}
