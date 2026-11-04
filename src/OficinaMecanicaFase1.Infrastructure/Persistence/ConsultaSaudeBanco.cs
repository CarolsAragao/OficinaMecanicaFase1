using OficinaMecanicaFase1.Application.Sistema;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

internal sealed class ConsultaSaudeBanco(OficinaDbContext db) : IConsultaSaudeBanco
{
    public Task<bool> PodeConectarAsync(CancellationToken cancelamento) =>
        db.Database.CanConnectAsync(cancelamento);
}
