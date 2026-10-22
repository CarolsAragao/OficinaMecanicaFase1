using System.Security.Claims;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.Catalogo;

namespace OficinaMecanicaFase1.Api.Catalogo;

public static class RotasCatalogoEstoque
{
    public static IEndpointRouteBuilder MapearCatalogoEstoque(this IEndpointRouteBuilder app)
    {
        var servicos = app.MapGroup("/api/servicos")
            .WithTags("Serviços de catálogo")
            .RequireAuthorization("EquipeInterna");

        servicos.MapGet("", ListarServicosAsync)
            .WithName("ListarServicosCatalogo")
            .WithSummary("Lista serviços; somenteAtivos=true mostra os selecionáveis para uma nova OS.")
            .Produces<ServicoCatalogoResponse[]>()
            .Produces(401).Produces(403);
        servicos.MapGet("/{id:guid}", ObterServicoAsync)
            .WithName("ObterServicoCatalogo")
            .Produces<ServicoCatalogoResponse>()
            .Produces(404).Produces(401).Produces(403);
        servicos.MapPost("", CriarServicoAsync)
            .WithName("CriarServicoCatalogo")
            .RequireAuthorization("Administradores")
            .Produces<ServicoCatalogoResponse>(201)
            .ProducesValidationProblem().Produces(401).Produces(403).ProducesProblem(409);
        servicos.MapPut("/{id:guid}", AtualizarServicoAsync)
            .WithName("AtualizarServicoCatalogo")
            .RequireAuthorization("Administradores")
            .Produces<ServicoCatalogoResponse>()
            .ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(409);
        servicos.MapPatch("/{id:guid}/ativacao", DefinirServicoAtivoAsync)
            .WithName("DefinirAtivacaoServicoCatalogo")
            .RequireAuthorization("Administradores")
            .Produces<ServicoCatalogoResponse>()
            .Produces(404).Produces(401).Produces(403).ProducesProblem(409);
        servicos.MapDelete("/{id:guid}", DesativarServicoAsync)
            .WithName("DesativarServicoCatalogo")
            .WithSummary("Desativa o serviço sem apagar seu cadastro ou referências históricas.")
            .RequireAuthorization("Administradores")
            .Produces(204).Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        var itens = app.MapGroup("/api/itens-estoque")
            .WithTags("Peças e insumos")
            .RequireAuthorization("EquipeInterna");

        itens.MapGet("", ListarItensAsync)
            .WithName("ListarItensEstoque")
            .WithSummary("Lista peças e insumos; somenteAtivos=true mostra os selecionáveis para uma nova OS.")
            .Produces<ItemEstoqueResponse[]>()
            .Produces(401).Produces(403);
        itens.MapGet("/{id:guid}", ObterItemAsync)
            .WithName("ObterItemEstoque")
            .Produces<ItemEstoqueResponse>()
            .Produces(404).Produces(401).Produces(403);
        itens.MapPost("", CriarItemAsync)
            .WithName("CriarItemEstoque")
            .RequireAuthorization("Administradores")
            .Produces<ItemEstoqueResponse>(201)
            .ProducesValidationProblem().Produces(401).Produces(403).ProducesProblem(409);
        itens.MapPut("/{id:guid}", AtualizarItemAsync)
            .WithName("AtualizarItemEstoque")
            .RequireAuthorization("Administradores")
            .Produces<ItemEstoqueResponse>()
            .ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(409);
        itens.MapPatch("/{id:guid}/ativacao", DefinirItemAtivoAsync)
            .WithName("DefinirAtivacaoItemEstoque")
            .RequireAuthorization("Administradores")
            .Produces<ItemEstoqueResponse>()
            .Produces(404).Produces(401).Produces(403).ProducesProblem(409);
        itens.MapDelete("/{id:guid}", DesativarItemAsync)
            .WithName("DesativarItemEstoque")
            .WithSummary("Desativa o item sem apagar seu cadastro nem as entradas registradas.")
            .RequireAuthorization("Administradores")
            .Produces(204).Produces(404).Produces(401).Produces(403).ProducesProblem(409);
        itens.MapPost("/{id:guid}/entradas", RegistrarEntradaAsync)
            .WithName("RegistrarEntradaEstoque")
            .WithSummary("Estoquista registra entrada de peça inteira ou insumo fracionário.")
            .RequireAuthorization("Estoquistas")
            .Produces<EntradaEstoqueResponse>(201)
            .ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(409);
        itens.MapGet("/{id:guid}/entradas", ListarEntradasAsync)
            .WithName("ListarEntradasEstoque")
            .Produces<EntradaEstoqueResponse[]>()
            .Produces(404).Produces(401).Produces(403);

        return app;
    }

    private static async Task<IResult> ListarServicosAsync(
        bool? somenteAtivos, GerenciadorCatalogo catalogo, CancellationToken cancelamento) =>
        Results.Ok((await catalogo.ListarServicosAsync(somenteAtivos ?? false, cancelamento))
            .Select(ServicoCatalogoResponse.De).ToArray());

    private static async Task<IResult> ObterServicoAsync(
        Guid id, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var servico = await catalogo.ObterServicoAsync(id, cancelamento);
        return servico is null ? Results.NotFound() : Results.Ok(ServicoCatalogoResponse.De(servico));
    }

    private static async Task<IResult> CriarServicoAsync(
        ServicoCatalogoRequest entrada, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var resultado = await catalogo.CriarServicoAsync(entrada.Nome, entrada.Descricao, entrada.PrecoBase, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Results.Created($"/api/servicos/{resultado.Valor!.Id}", ServicoCatalogoResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    private static async Task<IResult> AtualizarServicoAsync(
        Guid id, ServicoCatalogoRequest entrada, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var resultado = await catalogo.AtualizarServicoAsync(id, entrada.Nome, entrada.Descricao, entrada.PrecoBase, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Results.Ok(ServicoCatalogoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private static async Task<IResult> DefinirServicoAtivoAsync(
        Guid id, DefinirAtivacaoRequest entrada, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var resultado = await catalogo.DefinirServicoAtivoAsync(id, entrada.Ativo, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Results.Ok(ServicoCatalogoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private static async Task<IResult> DesativarServicoAsync(
        Guid id, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var resultado = await catalogo.DefinirServicoAtivoAsync(id, false, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum ? Results.NoContent() : ResponderErro(resultado);
    }

    private static async Task<IResult> ListarItensAsync(
        bool? somenteAtivos, GerenciadorCatalogo catalogo, CancellationToken cancelamento) =>
        Results.Ok((await catalogo.ListarItensAsync(somenteAtivos ?? false, cancelamento))
            .Select(ItemEstoqueResponse.De).ToArray());

    private static async Task<IResult> ObterItemAsync(
        Guid id, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var item = await catalogo.ObterItemAsync(id, cancelamento);
        return item is null ? Results.NotFound() : Results.Ok(ItemEstoqueResponse.De(item));
    }

    private static async Task<IResult> CriarItemAsync(
        CriarItemEstoqueRequest entrada, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var resultado = await catalogo.CriarItemAsync(entrada.Nome, entrada.Descricao, entrada.Tipo,
            entrada.UnidadeMedida, entrada.PrecoUnitario, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Results.Created($"/api/itens-estoque/{resultado.Valor!.Id}", ItemEstoqueResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    private static async Task<IResult> AtualizarItemAsync(
        Guid id, AtualizarItemEstoqueRequest entrada, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var resultado = await catalogo.AtualizarItemAsync(id, entrada.Nome, entrada.Descricao,
            entrada.PrecoUnitario, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Results.Ok(ItemEstoqueResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private static async Task<IResult> DefinirItemAtivoAsync(
        Guid id, DefinirAtivacaoRequest entrada, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var resultado = await catalogo.DefinirItemAtivoAsync(id, entrada.Ativo, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Results.Ok(ItemEstoqueResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private static async Task<IResult> DesativarItemAsync(
        Guid id, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        var resultado = await catalogo.DefinirItemAtivoAsync(id, false, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum ? Results.NoContent() : ResponderErro(resultado);
    }

    private static async Task<IResult> RegistrarEntradaAsync(
        Guid id, RegistrarEntradaEstoqueRequest entrada, ClaimsPrincipal usuario,
        GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(usuario.FindFirst("sub")?.Value, out var usuarioId)) return Results.Forbid();
        var resultado = await catalogo.RegistrarEntradaAsync(id, entrada.Quantidade, usuarioId, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Results.Created($"/api/itens-estoque/{id}/entradas", EntradaEstoqueResponse.De(resultado.Valor!))
            : ResponderErro(resultado);
    }

    private static async Task<IResult> ListarEntradasAsync(
        Guid id, GerenciadorCatalogo catalogo, CancellationToken cancelamento)
    {
        if (await catalogo.ObterItemAsync(id, cancelamento) is null) return Results.NotFound();
        return Results.Ok((await catalogo.ListarEntradasAsync(id, cancelamento))
            .Select(EntradaEstoqueResponse.De).ToArray());
    }

    private static IResult ResponderErro<T>(ResultadoCatalogo<T> resultado) => resultado.Erro switch
    {
        ErroCatalogo.Invalido => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [resultado.Campo ?? "catalogo"] = [resultado.Mensagem ?? "Dados inválidos."]
        }),
        ErroCatalogo.NaoEncontrado => Results.NotFound(),
        ErroCatalogo.Conflito => Results.Problem(resultado.Mensagem, statusCode: 409),
        _ => Results.Problem("Não foi possível processar a operação.", statusCode: 500)
    };
}
