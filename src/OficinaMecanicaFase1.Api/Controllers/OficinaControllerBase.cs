using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Api.Controllers;

public abstract class OficinaControllerBase : ControllerBase
{
    protected IActionResult RespostaValidacao(IDictionary<string, string[]> erros) =>
        ValidationProblem(new ValidationProblemDetails(erros));

    protected IActionResult ResponderErroOrdemServico<T>(
        ResultadoOrdemServico<T> resultado,
        string campoPadrao = "ordemServico",
        string? mensagemErroInterno = "Não foi possível processar a ordem de serviço.") => resultado.Erro switch
    {
        ErroOrdemServico.Invalido => RespostaValidacao(new Dictionary<string, string[]>
        {
            [resultado.Campo ?? campoPadrao] = [resultado.Mensagem ?? "Dados inválidos."]
        }),
        ErroOrdemServico.NaoEncontrado => NotFound(),
        ErroOrdemServico.Conflito => Problem(resultado.Mensagem, statusCode: 409),
        _ => Problem(mensagemErroInterno, statusCode: 500)
    };
}
