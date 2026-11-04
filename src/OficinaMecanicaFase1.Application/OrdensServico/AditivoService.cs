using System.Security.Cryptography;
using System.Text;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed class AditivoService(IAditivoRepository repositorio, TimeProvider tempo)
{
    public async Task<ResultadoOrdemServico<Aditivo>> CriarAsync(long numero, Guid mecanicoId,
        TipoAditivo tipo, string descricao, Guid? servicoId, Guid? itemId, decimal quantidade,
        bool originalPodeProsseguir, CancellationToken ct) => await repositorio.ExecutarAsync(numero,
        async (operacoes, ordem) =>
        {
            if (ordem.Status != StatusOrdemServico.EmExecucao)
                return Invalido("A OS precisa estar em execução.");
            if (await operacoes.TemPendenteAsync(ordem.Id, ct))
                return Conflito("Decida o aditivo pendente antes de registrar outro.");
            if (tipo == TipoAditivo.ConsumoExcedente &&
                (itemId is null || !ordem.AutorizaItem(itemId.Value)))
                return Invalido("Consumo excedente exige um item já autorizado na OS.");
            if (tipo == TipoAditivo.ReparoAdicional && (servicoId is null ||
                ordem.ServicosSolicitados.Any(s => s.ServicoCatalogoId == servicoId)))
                return Invalido("Informe um serviço adicional ainda não incluído na OS.");
            var item = itemId is { } estoqueId ? await operacoes.ObterItemAsync(estoqueId, ct) : null;
            if (itemId is not null && (item is null || !item.Ativo || !item.QuantidadeValida(quantidade)))
                return Invalido("Item inativo, inexistente ou quantidade inválida.");
            if (itemId is null && (tipo != TipoAditivo.ReparoAdicional || quantidade != 0))
                return Invalido("Reparo sem item deve ter quantidade zero.");
            var servico = servicoId is { } id ? await operacoes.ObterServicoAsync(id, ct) : null;
            if (servicoId is not null && (servico is null || !servico.Ativo))
                return Invalido("Serviço adicional inexistente ou inativo.");
            var aditivo = Aditivo.Criar(ordem.Id, tipo, descricao, servicoId, servico?.Nome,
                itemId, item?.Nome, quantidade, servico?.PrecoBase ?? 0, item?.PrecoUnitario ?? 0,
                originalPodeProsseguir,
                mecanicoId, tempo.GetUtcNow());
            operacoes.Adicionar(aditivo);
            operacoes.Adicionar(ordem.RegistrarMarcoAditivo(
                tipo == TipoAditivo.ConsumoExcedente ? "Consumo excedente identificado; aprovação adicional solicitada" :
                "Reparo adicional identificado; aditivo disponibilizado", tempo.GetUtcNow()));
            return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
        }, ct);

    public async Task<Aditivo?> ConsultarClienteAsync(long numero, Guid aditivoId, string? documento,
        CancellationToken ct)
    {
        var normalizado = DocumentoCliente.Normalizar(documento);
        if (normalizado is null) return null;
        return await repositorio.ConsultarClienteAsync(numero, aditivoId, normalizado, ct);
    }

    public Task<Aditivo?> ConsultarInternoAsync(long numero, Guid aditivoId, CancellationToken ct) =>
        repositorio.ConsultarInternoAsync(numero, aditivoId, ct);

    public async Task<ResultadoOrdemServico<Aditivo>> DecidirAsync(long numero, Guid aditivoId,
        string? documento, string? codigo, bool aprovar, string? motivo, CancellationToken ct)
    {
        var normalizado = DocumentoCliente.Normalizar(documento);
        var limpo = codigo?.Trim().Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
        if (normalizado is null || limpo is null || limpo.Length != 24 || !limpo.All(Uri.IsHexDigit))
            return NaoEncontrado();
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(limpo));
        var resultado = await repositorio.ExecutarAsync(numero, async (operacoes, ordem) =>
        {
            var cliente = await operacoes.ObterClienteAsync(ordem.ClienteId, ct);
            if (cliente?.Documento != normalizado || !ordem.ValidarCodigoAcesso(hash)) return NaoEncontrado();
            var aditivo = await operacoes.ObterAditivoAsync(ordem.Id, aditivoId, ct);
            if (aditivo is null) return NaoEncontrado();
            if (ordem.Status != StatusOrdemServico.EmExecucao || aditivo.Situacao != SituacaoAditivo.Pendente)
                return Conflito("O aditivo não aguarda decisão.");
            aditivo.Decidir(aprovar, motivo, tempo.GetUtcNow());
            operacoes.Adicionar(ordem.RegistrarMarcoAditivo(aprovar ?
                "Aditivo aprovado; reserva adicional solicitada" :
                aditivo.OriginalPodeProsseguir ? "Aditivo rejeitado; escopo original pode prosseguir" :
                "Aditivo rejeitado; impedimento do escopo original registrado", tempo.GetUtcNow()));
            return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
        }, ct);
        if (aprovar && resultado.Erro == ErroOrdemServico.Nenhum)
            await TentarReservaAsync(numero, aditivoId, ct);
        return resultado;
    }

    public async Task<ResultadoOrdemServico<Aditivo>> TentarReservaAsync(long numero, Guid aditivoId,
        CancellationToken ct) => await repositorio.ExecutarAsync(numero, async (operacoes, ordem) =>
    {
        var aditivo = await operacoes.ObterAditivoAsync(ordem.Id, aditivoId, ct);
        if (aditivo is null) return NaoEncontrado();
        if (ordem.Status != StatusOrdemServico.EmExecucao || aditivo.Situacao != SituacaoAditivo.Aprovado ||
            aditivo.SituacaoReserva == SituacaoReservaAditivo.Confirmada)
            return Conflito("O aditivo não aguarda reserva.");
        if (aditivo.ItemEstoqueId is null)
        {
            var novos = ordem.AutorizarAditivo(aditivo.ServicoCatalogoId, null, 0);
            if (novos.Servico is not null) operacoes.Adicionar(novos.Servico);
            aditivo.RegistrarReserva(true);
            operacoes.Adicionar(ordem.RegistrarMarcoAditivo(
                "Reparo adicional sem item autorizado", tempo.GetUtcNow()));
            return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
        }
        var item = await operacoes.BloquearItemAsync(aditivo.ItemEstoqueId.Value, ct);
        if (item is null) return Conflito("Item do aditivo não encontrado.");
        if (item.SaldoDisponivel < aditivo.Quantidade)
        {
            aditivo.RegistrarReserva(false);
            operacoes.Adicionar(ordem.RegistrarMarcoAditivo(
                "Reserva do aditivo recusada; aguarda reposição", tempo.GetUtcNow()));
            return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
        }
        item.Reservar(aditivo.Quantidade);
        var reserva = await operacoes.ObterReservaAtivaAsync(ordem.Id, item.Id, ct);
        if (reserva is null)
            operacoes.Adicionar(ReservaItemOrdemServico.Criar(
                ordem.Id, item.Id, aditivo.Quantidade, tempo.GetUtcNow()));
        else reserva.Acrescentar(aditivo.Quantidade);
        var autorizados = ordem.AutorizarAditivo(aditivo.ServicoCatalogoId, item.Id, aditivo.Quantidade);
        if (autorizados.Servico is not null) operacoes.Adicionar(autorizados.Servico);
        if (autorizados.Item is not null) operacoes.Adicionar(autorizados.Item);
        aditivo.RegistrarReserva(true);
        operacoes.Adicionar(ordem.RegistrarMarcoAditivo(
            "Itens adicionais reservados; aditivo autorizado", tempo.GetUtcNow()));
        return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
    }, ct);

    private static ResultadoOrdemServico<Aditivo> NaoEncontrado() =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.NaoEncontrado, "Aditivo ou credenciais não encontrados.");
    private static ResultadoOrdemServico<Aditivo> Invalido(string mensagem) =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.Invalido, mensagem);
    private static ResultadoOrdemServico<Aditivo> Conflito(string mensagem) =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.Conflito, mensagem);
}
