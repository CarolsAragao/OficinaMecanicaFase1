using System.Security.Claims;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;
using OficinaMecanicaFase1.Infrastructure.Persistence;

namespace OficinaMecanicaFase1.Api.OrdensServico;

public sealed record CriarAditivoRequest(TipoAditivo Tipo, string Descricao, Guid? ServicoCatalogoId,
    Guid? ItemEstoqueId, decimal Quantidade, bool OriginalPodeProsseguir);
public sealed record ConsultarAditivoRequest(string Documento);
public sealed record DecidirAditivoRequest(string Documento, string CodigoAcesso, bool Aprovar, string? Motivo);
public sealed record AditivoResponse(Guid Id, long NumeroOrdemServico, string Tipo, string Descricao,
    Guid? ServicoCatalogoId, string? NomeServico, Guid? ItemEstoqueId, string? NomeItem,
    decimal Quantidade, decimal PrecoServico,
    decimal PrecoItem, decimal Total, bool OriginalPodeProsseguir, string Situacao,
    string SituacaoReserva, DateTimeOffset CriadoEmUtc, DateTimeOffset? DecididoEmUtc,
    string? MotivoRejeicao)
{
    public static AditivoResponse De(Aditivo a, long numero) => new(a.Id, numero, a.Tipo.ToString(),
        a.Descricao, a.ServicoCatalogoId, a.NomeServico, a.ItemEstoqueId, a.NomeItem,
        a.Quantidade, a.PrecoServico,
        a.PrecoItem, a.Total, a.OriginalPodeProsseguir, a.Situacao.ToString(),
        a.SituacaoReserva.ToString(), a.CriadoEmUtc, a.DecididoEmUtc, a.MotivoRejeicao);
}

public static class RotasAditivos
{
    public static IEndpointRouteBuilder MapearAditivos(this IEndpointRouteBuilder app)
    {
        var internas = app.MapGroup("/api/ordens-servico/{numero:long}/aditivos")
            .WithTags("Aditivos").RequireAuthorization("EquipeInterna");
        internas.MapPost("", CriarAsync).RequireAuthorization("Mecanicos")
            .Produces<AditivoResponse>(201).ProducesValidationProblem().Produces(401).Produces(403).ProducesProblem(409);
        internas.MapGet("/{aditivoId:guid}", ObterInternoAsync)
            .Produces<AditivoResponse>().Produces(404);
        internas.MapPost("/{aditivoId:guid}/reservas/tentativa", TentarReservaAsync)
            .RequireAuthorization("Estoquistas").Produces<AditivoResponse>().ProducesProblem(409);

        var cliente = app.MapGroup("/api/cliente/ordens-servico/{numero:long}/aditivos")
            .WithTags("Cliente - aditivos");
        cliente.MapPost("/{aditivoId:guid}/consulta", ConsultarClienteAsync)
            .Produces<AditivoResponse>().Produces(404);
        cliente.MapPost("/{aditivoId:guid}/decisao", DecidirAsync)
            .Produces<AditivoResponse>().ProducesValidationProblem().Produces(404).ProducesProblem(409);
        return app;
    }

    private static async Task<IResult> CriarAsync(long numero, CriarAditivoRequest entrada,
        ClaimsPrincipal usuario, GerenciadorAditivos gerenciador, CancellationToken ct)
    {
        if (!Guid.TryParse(usuario.FindFirst("sub")?.Value, out var mecanicoId)) return Results.Forbid();
        var resultado = await gerenciador.CriarAsync(numero, mecanicoId, entrada.Tipo, entrada.Descricao,
            entrada.ServicoCatalogoId, entrada.ItemEstoqueId, entrada.Quantidade,
            entrada.OriginalPodeProsseguir, ct);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Created($"/api/ordens-servico/{numero}/aditivos/{resultado.Valor!.Id}",
                AditivoResponse.De(resultado.Valor, numero)) : Erro(resultado);
    }

    private static async Task<IResult> ObterInternoAsync(long numero, Guid aditivoId,
        GerenciadorAditivos gerenciador, CancellationToken ct)
    {
        var aditivo = await gerenciador.ConsultarInternoAsync(numero, aditivoId, ct);
        return aditivo is null ? Results.NotFound() : Results.Ok(AditivoResponse.De(aditivo, numero));
    }

    private static async Task<IResult> ConsultarClienteAsync(long numero, Guid aditivoId,
        ConsultarAditivoRequest entrada, HttpContext contexto, GerenciadorAditivos gerenciador,
        CancellationToken ct)
    {
        contexto.Response.Headers.CacheControl = "no-store";
        var aditivo = await gerenciador.ConsultarClienteAsync(numero, aditivoId, entrada.Documento, ct);
        return aditivo is null ? Results.NotFound() : Results.Ok(AditivoResponse.De(aditivo, numero));
    }

    private static async Task<IResult> DecidirAsync(long numero, Guid aditivoId,
        DecidirAditivoRequest entrada, HttpContext contexto, GerenciadorAditivos gerenciador,
        CancellationToken ct)
    {
        contexto.Response.Headers.CacheControl = "no-store";
        var resultado = await gerenciador.DecidirAsync(numero, aditivoId, entrada.Documento,
            entrada.CodigoAcesso, entrada.Aprovar, entrada.Motivo, ct);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Ok(AditivoResponse.De((await gerenciador.ConsultarInternoAsync(numero, aditivoId, ct))!, numero))
            : Erro(resultado);
    }

    private static async Task<IResult> TentarReservaAsync(long numero, Guid aditivoId,
        GerenciadorAditivos gerenciador, CancellationToken ct)
    {
        var resultado = await gerenciador.TentarReservaAsync(numero, aditivoId, ct);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Results.Ok(AditivoResponse.De(resultado.Valor!, numero)) : Erro(resultado);
    }

    private static IResult Erro(ResultadoOrdemServico<Aditivo> resultado) => resultado.Erro switch
    {
        ErroOrdemServico.NaoEncontrado => Results.NotFound(),
        ErroOrdemServico.Invalido => Results.ValidationProblem(new Dictionary<string, string[]>
            { [resultado.Campo ?? "aditivo"] = [resultado.Mensagem ?? "Dados inválidos."] }),
        ErroOrdemServico.Conflito => Results.Problem(resultado.Mensagem, statusCode: 409),
        _ => Results.Problem(statusCode: 500)
    };
}
