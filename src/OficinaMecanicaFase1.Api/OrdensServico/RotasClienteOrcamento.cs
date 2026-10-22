using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Api.OrdensServico;

public static class RotasClienteOrcamento
{
    public static IEndpointRouteBuilder MapearClienteOrcamento(this IEndpointRouteBuilder app)
    {
        var rotas = app.MapGroup("/api/cliente/ordens-servico").WithTags("Cliente - orçamento");

        rotas.MapPost("/{numero:long}/orcamento/consulta", ConsultarAsync)
            .WithName("ConsultarOrcamentoCliente")
            .WithSummary("Cliente consulta a proposta pelo número da OS e CPF/CNPJ.")
            .Produces<OrcamentoResponse>().Produces(404);

        rotas.MapPost("/{numero:long}/orcamento/decisao", DecidirAsync)
            .WithName("DecidirOrcamentoCliente")
            .WithSummary("Cliente aprova ou rejeita com número da OS, CPF/CNPJ e código individual.")
            .Produces<OrcamentoResponse>().Produces(404).ProducesValidationProblem().ProducesProblem(409);

        return app;
    }

    private static async Task<IResult> ConsultarAsync(long numero,
        ConsultarOrcamentoClienteRequest entrada, HttpContext contexto,
        GerenciadorDecisaoOrcamento gerenciador, CancellationToken cancelamento)
    {
        contexto.Response.Headers.CacheControl = "no-store";
        var proposta = await gerenciador.ConsultarPropostaAsync(numero, entrada.Documento, cancelamento);
        return proposta is null ? Results.NotFound() : Results.Ok(OrcamentoResponse.De(proposta, numero));
    }

    private static async Task<IResult> DecidirAsync(long numero,
        DecidirOrcamentoClienteRequest entrada, HttpContext contexto,
        GerenciadorDecisaoOrcamento gerenciador, CancellationToken cancelamento)
    {
        contexto.Response.Headers.CacheControl = "no-store";
        var resultado = await gerenciador.DecidirAsync(numero, entrada.Documento, entrada.CodigoAcesso,
            entrada.Aprovar, entrada.Motivo, cancelamento);
        return resultado.Erro switch
        {
            ErroOrdemServico.Nenhum => Results.Ok(OrcamentoResponse.De(resultado.Valor!, numero)),
            ErroOrdemServico.NaoEncontrado => Results.NotFound(),
            ErroOrdemServico.Invalido => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [resultado.Campo ?? "decisao"] = [resultado.Mensagem ?? "Dados inválidos."]
            }),
            ErroOrdemServico.Conflito => Results.Problem(resultado.Mensagem, statusCode: 409),
            _ => Results.Problem("Não foi possível registrar a decisão.", statusCode: 500)
        };
    }
}
