using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.OrdensServico;
using System.Security.Claims;

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

        ordens.MapPost("/{numero:long}/diagnostico/inicio", IniciarDiagnosticoAsync)
            .WithName("IniciarDiagnostico")
            .WithSummary("Mecânico inicia o diagnóstico de uma OS recebida.")
            .RequireAuthorization("Mecanicos")
            .Produces<OrdemServicoResponse>()
            .ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        ordens.MapPost("/{numero:long}/diagnostico", RegistrarDiagnosticoAsync)
            .WithName("RegistrarDiagnostico")
            .WithSummary("Mecânico registra o diagnóstico iniciado.")
            .RequireAuthorization("Mecanicos")
            .Produces<OrdemServicoResponse>()
            .ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        ordens.MapPost("/{numero:long}/orcamento", GerarOrcamentoAsync)
            .WithName("GerarOrcamento")
            .WithSummary("Sistema calcula serviços e itens após o diagnóstico e disponibiliza a proposta pela API.")
            .RequireAuthorization("Mecanicos")
            .Produces<OrcamentoResponse>()
            .ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        ordens.MapGet("/{numero:long}/orcamento", ObterOrcamentoAsync)
            .WithName("ObterOrcamento")
            .Produces<OrcamentoResponse>()
            .Produces(404).Produces(401).Produces(403);

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

    private static async Task<IResult> IniciarDiagnosticoAsync(long numero, ClaimsPrincipal usuario,
        GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(usuario.FindFirst("sub")?.Value, out var mecanicoId)) return Results.Forbid();
        var resultado = await gerenciador.IniciarDiagnosticoAsync(numero, mecanicoId, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private static async Task<IResult> RegistrarDiagnosticoAsync(long numero, RegistrarDiagnosticoRequest entrada,
        ClaimsPrincipal usuario, GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(usuario.FindFirst("sub")?.Value, out var mecanicoId)) return Results.Forbid();
        var resultado = await gerenciador.RegistrarDiagnosticoAsync(numero, mecanicoId, entrada.Descricao, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private static async Task<IResult> GerarOrcamentoAsync(long numero,
        GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        var resultado = await gerenciador.GerarOrcamentoAsync(numero, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Ok(OrcamentoResponse.De(resultado.Valor!, numero)) : ResponderErro(resultado);
    }

    private static async Task<IResult> ObterOrcamentoAsync(long numero,
        GerenciadorOrdensServico gerenciador, CancellationToken cancelamento)
    {
        var orcamento = await gerenciador.ObterOrcamentoAsync(numero, cancelamento);
        return orcamento is null ? Results.NotFound() : Results.Ok(OrcamentoResponse.De(orcamento, numero));
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
