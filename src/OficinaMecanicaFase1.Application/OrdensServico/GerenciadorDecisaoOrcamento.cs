using System.Security.Cryptography;
using System.Text;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed record CodigoAcessoEmitido(string Codigo, int NumeroEmissao, DateTimeOffset EmitidoEmUtc);

public sealed class GerenciadorDecisaoOrcamento(
    IRepositorioOrdensServico repositorio, GerenciadorReservas reservas, TimeProvider tempo)
{
    public async Task<Orcamento?> ConsultarPropostaAsync(long numero, string? documento,
        CancellationToken cancelamento)
    {
        var normalizado = DocumentoCliente.Normalizar(documento);
        if (normalizado is null) return null;
        var ordem = await repositorio.ObterDoClienteAsync(numero, normalizado, cancelamento);
        return ordem is null ? null : await repositorio.ObterOrcamentoAsync(ordem.Id, cancelamento);
    }

    public async Task<ResultadoOrdemServico<CodigoAcessoEmitido>> EmitirCodigoAsync(
        long numero, Guid atendenteId, string? motivo, CancellationToken cancelamento)
    {
        var ordem = await repositorio.ObterAsync(numero, cancelamento);
        if (ordem is null) return NaoEncontrado<CodigoAcessoEmitido>();
        var orcamento = await repositorio.ObterOrcamentoAsync(ordem.Id, cancelamento);
        if (orcamento is null || orcamento.Decisao != DecisaoOrcamento.Pendente)
            return Conflito<CodigoAcessoEmitido>("O orçamento não está pendente de decisão.");

        var bytes = RandomNumberGenerator.GetBytes(12);
        var hex = Convert.ToHexString(bytes);
        var codigo = $"{hex[..8]}-{hex[8..16]}-{hex[16..]}";
        try
        {
            var emissao = ordem.EmitirCodigoAcesso(HashCodigo(hex), atendenteId, motivo, tempo.GetUtcNow());
            repositorio.Adicionar(emissao);
            await repositorio.SalvarAsync(cancelamento);
            return ResultadoOrdemServico<CodigoAcessoEmitido>.Sucesso(
                new CodigoAcessoEmitido(codigo, emissao.NumeroEmissao, emissao.EmitidoEmUtc));
        }
        catch (ArgumentException erro)
        {
            return ResultadoOrdemServico<CodigoAcessoEmitido>.Falha(ErroOrdemServico.Invalido,
                erro.Message, erro.ParamName);
        }
        catch (InvalidOperationException erro) { return Conflito<CodigoAcessoEmitido>(erro.Message); }
        catch (ConflitoPersistenciaException)
        {
            return Conflito<CodigoAcessoEmitido>("A OS foi alterada. Consulte e tente novamente.");
        }
    }

    public async Task<ResultadoOrdemServico<Orcamento>> DecidirAsync(long numero,
        string? documento, string? codigo, bool aprovar, string? motivo,
        CancellationToken cancelamento)
    {
        var normalizado = DocumentoCliente.Normalizar(documento);
        if (normalizado is null || !TentarHashCodigo(codigo, out var hash)) return NaoEncontrado<Orcamento>();
        var ordem = await repositorio.ObterDoClienteAsync(numero, normalizado, cancelamento);
        if (ordem is null || !ordem.ValidarCodigoAcesso(hash)) return NaoEncontrado<Orcamento>();
        var orcamento = await repositorio.ObterOrcamentoAsync(ordem.Id, cancelamento);
        if (orcamento is null) return NaoEncontrado<Orcamento>();
        if (orcamento.Decisao != DecisaoOrcamento.Pendente)
            return Conflito<Orcamento>("O orçamento já recebeu uma decisão.");

        try
        {
            var agora = tempo.GetUtcNow();
            if (aprovar)
            {
                if (!string.IsNullOrWhiteSpace(motivo))
                    return ResultadoOrdemServico<Orcamento>.Falha(ErroOrdemServico.Invalido,
                        "A aprovação não recebe motivo de rejeição.", "motivo");
                orcamento.Aprovar(agora);
                repositorio.Adicionar(ordem.RegistrarAprovacaoOrcamento(agora));
            }
            else
            {
                orcamento.Rejeitar(motivo ?? string.Empty, agora);
                var marcos = ordem.EncerrarPorRejeicao(motivo!, agora);
                repositorio.Adicionar(marcos.Rejeicao);
                repositorio.Adicionar(marcos.Encerramento);

                var reservas = await repositorio.ObterReservasAtivasAsync(ordem.Id, cancelamento);
                var ids = reservas.Select(reserva => reserva.ItemEstoqueId).Distinct().ToArray();
                var itens = await repositorio.ObterItensParaReservaAsync(ids, cancelamento);
                if (itens.Count != ids.Length)
                    return Conflito<Orcamento>("Um item reservado não está disponível para liberação.");
                foreach (var grupo in reservas.GroupBy(reserva => reserva.ItemEstoqueId))
                    itens[grupo.Key].LiberarReserva(grupo.Sum(reserva => reserva.Quantidade));
                foreach (var reserva in reservas) reserva.Liberar(agora);
            }
            await repositorio.SalvarAsync(cancelamento);
            if (aprovar) await reservas.TentarReservarAsync(numero, cancelamento);
            return ResultadoOrdemServico<Orcamento>.Sucesso(orcamento);
        }
        catch (ArgumentException erro)
        {
            return ResultadoOrdemServico<Orcamento>.Falha(ErroOrdemServico.Invalido,
                erro.Message, erro.ParamName);
        }
        catch (InvalidOperationException erro) { return Conflito<Orcamento>(erro.Message); }
        catch (ConflitoPersistenciaException)
        {
            return Conflito<Orcamento>("A proposta, o código ou uma reserva foi alterado. Consulte e tente novamente.");
        }
    }

    private static byte[] HashCodigo(string codigo) =>
        SHA256.HashData(Encoding.ASCII.GetBytes(codigo));

    private static bool TentarHashCodigo(string? codigo, out byte[] hash)
    {
        var normalizado = codigo?.Trim().Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
        if (normalizado is null || normalizado.Length != 24 || !normalizado.All(Uri.IsHexDigit))
        {
            hash = [];
            return false;
        }
        hash = HashCodigo(normalizado);
        return true;
    }

    private static ResultadoOrdemServico<T> NaoEncontrado<T>() =>
        ResultadoOrdemServico<T>.Falha(ErroOrdemServico.NaoEncontrado, "Proposta ou credenciais não encontradas.");

    private static ResultadoOrdemServico<T> Conflito<T>(string mensagem) =>
        ResultadoOrdemServico<T>.Falha(ErroOrdemServico.Conflito, mensagem);
}
