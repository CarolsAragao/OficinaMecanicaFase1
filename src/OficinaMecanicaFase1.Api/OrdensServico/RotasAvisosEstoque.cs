using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Api.OrdensServico;

public static class RotasAvisosEstoque
{
    public static IEndpointRouteBuilder MapearAvisosEstoque(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/avisos-estoque/reservas", async (
                bool? somentePendentes, GerenciadorReservas reservas, CancellationToken cancelamento) =>
                Results.Ok(await reservas.ListarAvisosAsync(somentePendentes ?? true, cancelamento)))
            .WithName("ListarAvisosReservaEstoque")
            .WithTags("Avisos de estoque")
            .WithSummary("Estoquista consulta recusas de reserva pendentes ou o histórico de avisos.")
            .RequireAuthorization("Estoquistas")
            .Produces<IReadOnlyCollection<AvisoReservaEstoque>>()
            .Produces(401).Produces(403);
        return app;
    }
}
