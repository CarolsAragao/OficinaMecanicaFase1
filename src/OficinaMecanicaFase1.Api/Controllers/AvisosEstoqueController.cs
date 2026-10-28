using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/avisos-estoque")]
[Tags("Avisos de estoque")]
[Authorize(Policy = "Estoquistas")]
public sealed class AvisosEstoqueController(GerenciadorReservas reservas) : OficinaControllerBase
{
    [HttpGet("reservas", Name = "ListarAvisosReservaEstoque")]
    [EndpointSummary("Estoquista consulta recusas de reserva pendentes ou o histórico de avisos.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvisoReservaEstoque>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListarReservasAsync([FromQuery] bool? somentePendentes,
        CancellationToken cancelamento) =>
        Ok(await reservas.ListarAvisosAsync(somentePendentes ?? true, cancelamento));
}
