using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public interface IAditivoRepository
{
    Task<Aditivo?> ConsultarClienteAsync(long numero, Guid aditivoId, string documento, CancellationToken ct);
    Task<Aditivo?> ConsultarInternoAsync(long numero, Guid aditivoId, CancellationToken ct);

    // Cada tentativa recebe operações vinculadas à mesma transação e à OS bloqueada.
    Task<ResultadoOrdemServico<Aditivo>> ExecutarAsync(long numero,
        Func<IOperacoesAditivos, OrdemServico, Task<ResultadoOrdemServico<Aditivo>>> acao, CancellationToken ct);
}

public interface IOperacoesAditivos
{
    Task<bool> TemPendenteAsync(Guid ordemId, CancellationToken ct);
    Task<Aditivo?> ObterAditivoAsync(Guid ordemId, Guid aditivoId, CancellationToken ct);
    ValueTask<Cliente?> ObterClienteAsync(Guid id, CancellationToken ct);
    ValueTask<ItemEstoque?> ObterItemAsync(Guid id, CancellationToken ct);
    ValueTask<ServicoCatalogo?> ObterServicoAsync(Guid id, CancellationToken ct);
    Task<ItemEstoque?> BloquearItemAsync(Guid id, CancellationToken ct);
    Task<ReservaItemOrdemServico?> ObterReservaAtivaAsync(Guid ordemId, Guid itemId, CancellationToken ct);
    void Adicionar(Aditivo aditivo);
    void Adicionar(RegistroHistoricoOrdemServico marco);
    void Adicionar(ServicoSolicitado servico);
    void Adicionar(ItemNecessario item);
    void Adicionar(ReservaItemOrdemServico reserva);
}
