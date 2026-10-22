using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.Cadastros;

namespace OficinaMecanicaFase1.Api.Cadastros;

public static class RotasCadastros
{
    public static IEndpointRouteBuilder MapearCadastros(this IEndpointRouteBuilder app)
    {
        var clientes = app.MapGroup("/api/clientes")
            .WithTags("Clientes")
            .RequireAuthorization("Cadastros");

        clientes.MapGet("", ListarClientesAsync)
            .WithName("ListarClientes")
            .WithSummary("Lista clientes; documento opcional busca CPF/CNPJ exato após validação.")
            .Produces<ClienteResponse[]>()
            .ProducesValidationProblem()
            .Produces(401).Produces(403);

        clientes.MapGet("/{id:guid}", ObterClienteAsync)
            .WithName("ObterCliente")
            .Produces<ClienteResponse>()
            .Produces(404).Produces(401).Produces(403);

        clientes.MapPost("", CriarClienteAsync)
            .WithName("CriarCliente")
            .Produces<ClienteResponse>(201)
            .ProducesValidationProblem()
            .Produces(401).Produces(403).ProducesProblem(409);

        clientes.MapPut("/{id:guid}", AtualizarClienteAsync)
            .WithName("AtualizarCliente")
            .Produces<ClienteResponse>()
            .ProducesValidationProblem()
            .Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        clientes.MapDelete("/{id:guid}", RemoverClienteAsync)
            .WithName("RemoverCliente")
            .RequireAuthorization("Administradores")
            .Produces(204).Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        var veiculos = app.MapGroup("/api/veiculos")
            .WithTags("Veículos")
            .RequireAuthorization("Cadastros");

        veiculos.MapGet("", ListarVeiculosAsync)
            .WithName("ListarVeiculos")
            .WithSummary("Lista veículos; clienteId opcional filtra pelo proprietário.")
            .Produces<VeiculoResponse[]>()
            .Produces(401).Produces(403);

        veiculos.MapGet("/{id:guid}", ObterVeiculoAsync)
            .WithName("ObterVeiculo")
            .Produces<VeiculoResponse>()
            .Produces(404).Produces(401).Produces(403);

        veiculos.MapPost("", CriarVeiculoAsync)
            .WithName("CriarVeiculo")
            .Produces<VeiculoResponse>(201)
            .ProducesValidationProblem()
            .Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        veiculos.MapPut("/{id:guid}", AtualizarVeiculoAsync)
            .WithName("AtualizarVeiculo")
            .WithSummary("Atualiza os dados do veículo sem alterar o cliente proprietário.")
            .Produces<VeiculoResponse>()
            .ProducesValidationProblem()
            .Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        veiculos.MapDelete("/{id:guid}", RemoverVeiculoAsync)
            .WithName("RemoverVeiculo")
            .RequireAuthorization("Administradores")
            .Produces(204).Produces(404).Produces(401).Produces(403).ProducesProblem(409);

        return app;
    }

    private static async Task<IResult> ListarClientesAsync(
        string? documento, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var resultado = await cadastros.ListarClientesAsync(documento, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Results.Ok(resultado.Valor!.Select(ClienteResponse.De).ToArray())
            : ResponderErro(resultado);
    }

    private static async Task<IResult> ObterClienteAsync(
        Guid id, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var cliente = await cadastros.ObterClienteAsync(id, cancelamento);
        return cliente is null ? Results.NotFound() : Results.Ok(ClienteResponse.De(cliente));
    }

    private static async Task<IResult> CriarClienteAsync(
        ClienteRequest entrada, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var resultado = await cadastros.CriarClienteAsync(entrada.Nome, entrada.Documento, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Results.Created($"/api/clientes/{resultado.Valor!.Id}", ClienteResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    private static async Task<IResult> AtualizarClienteAsync(
        Guid id, ClienteRequest entrada, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var resultado = await cadastros.AtualizarClienteAsync(id, entrada.Nome, entrada.Documento, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Results.Ok(ClienteResponse.De(resultado.Valor!))
            : ResponderErro(resultado);
    }

    private static async Task<IResult> RemoverClienteAsync(
        Guid id, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var resultado = await cadastros.RemoverClienteAsync(id, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum ? Results.NoContent() : ResponderErro(resultado);
    }

    private static async Task<IResult> ListarVeiculosAsync(
        Guid? clienteId, GerenciadorCadastros cadastros, CancellationToken cancelamento) =>
        Results.Ok((await cadastros.ListarVeiculosAsync(clienteId, cancelamento))
            .Select(VeiculoResponse.De).ToArray());

    private static async Task<IResult> ObterVeiculoAsync(
        Guid id, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var veiculo = await cadastros.ObterVeiculoAsync(id, cancelamento);
        return veiculo is null ? Results.NotFound() : Results.Ok(VeiculoResponse.De(veiculo));
    }

    private static async Task<IResult> CriarVeiculoAsync(
        CriarVeiculoRequest entrada, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var resultado = await cadastros.CriarVeiculoAsync(
            entrada.ClienteId, entrada.Placa, entrada.Marca, entrada.Modelo, entrada.Ano, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Results.Created($"/api/veiculos/{resultado.Valor!.Id}", VeiculoResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    private static async Task<IResult> AtualizarVeiculoAsync(
        Guid id, AtualizarVeiculoRequest entrada, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var resultado = await cadastros.AtualizarVeiculoAsync(
            id, entrada.Placa, entrada.Marca, entrada.Modelo, entrada.Ano, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Results.Ok(VeiculoResponse.De(resultado.Valor!))
            : ResponderErro(resultado);
    }

    private static async Task<IResult> RemoverVeiculoAsync(
        Guid id, GerenciadorCadastros cadastros, CancellationToken cancelamento)
    {
        var resultado = await cadastros.RemoverVeiculoAsync(id, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum ? Results.NoContent() : ResponderErro(resultado);
    }

    private static IResult ResponderErro<T>(ResultadoCadastro<T> resultado) => resultado.Erro switch
    {
        ErroCadastro.Invalido => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [resultado.Campo ?? "cadastro"] = [resultado.Mensagem ?? "Dados inválidos."]
        }),
        ErroCadastro.NaoEncontrado => Results.NotFound(),
        ErroCadastro.Conflito => Results.Problem(resultado.Mensagem, statusCode: 409),
        _ => Results.Problem("Não foi possível processar o cadastro.", statusCode: 500)
    };
}
