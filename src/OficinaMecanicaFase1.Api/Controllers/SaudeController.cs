using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.Sistema;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/saude")]
[Tags("Sistema")]
[AllowAnonymous]
public sealed class SaudeController : OficinaControllerBase
{
    [HttpGet("", Name = "ConsultarSaude")]
    [ProducesResponseType(typeof(HealthResponse), 200)]
    public IActionResult Consultar() => Ok(new HealthResponse("operacional"));

    [HttpGet("banco", Name = "ConsultarSaudeBanco")]
    [ProducesResponseType(typeof(HealthResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> ConsultarBancoAsync([FromServices] IConsultaSaudeBanco consulta,
        CancellationToken cancellationToken) =>
        await consulta.PodeConectarAsync(cancellationToken)
            ? Ok(new HealthResponse("operacional"))
            : Problem("Banco de dados indisponível", statusCode: 503);
}
