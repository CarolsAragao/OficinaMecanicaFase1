using Microsoft.AspNetCore.Mvc;

namespace OficinaMecanicaFase1.Api.Controllers;

public abstract class OficinaControllerBase : ControllerBase
{
    protected IActionResult RespostaValidacao(IDictionary<string, string[]> erros) =>
        ValidationProblem(new ValidationProblemDetails(erros));
}
