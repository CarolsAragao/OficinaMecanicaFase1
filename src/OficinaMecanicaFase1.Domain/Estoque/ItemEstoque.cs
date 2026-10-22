namespace OficinaMecanicaFase1.Domain.Estoque;

public sealed class ItemEstoque
{
    private const decimal SaldoMaximo = 999_999_999_999_999.999m;
    private ItemEstoque() { }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public TipoItemEstoque Tipo { get; private set; }
    public string UnidadeMedida { get; private set; } = null!;
    public decimal PrecoUnitario { get; private set; }
    public decimal SaldoFisico { get; private set; }
    public decimal SaldoReservado { get; private set; }
    public decimal SaldoDisponivel => SaldoFisico - SaldoReservado;
    public bool Ativo { get; private set; }
    public byte[] Versao { get; private set; } = [];

    public ItemEstoque(string nome, string? descricao, TipoItemEstoque tipo, string unidadeMedida, decimal precoUnitario)
    {
        if (!Enum.IsDefined(tipo)) throw new ArgumentException("Tipo de item inválido.", nameof(tipo));
        if (string.IsNullOrWhiteSpace(unidadeMedida) || unidadeMedida.Trim().Length > 20)
            throw new ArgumentException("Informe uma unidade de medida com até 20 caracteres.", nameof(unidadeMedida));
        if (tipo == TipoItemEstoque.Peca && !unidadeMedida.Trim().Equals("un", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Peças devem usar a unidade 'un'.", nameof(unidadeMedida));

        Id = Guid.NewGuid();
        Tipo = tipo;
        UnidadeMedida = tipo == TipoItemEstoque.Peca ? "un" : unidadeMedida.Trim();
        Ativo = true;
        Atualizar(nome, descricao, precoUnitario);
    }

    public void Atualizar(string nome, string? descricao, decimal precoUnitario)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 200)
            throw new ArgumentException("Informe o nome do item com até 200 caracteres.", nameof(nome));
        if (descricao?.Trim().Length > 500)
            throw new ArgumentException("A descrição deve ter até 500 caracteres.", nameof(descricao));
        if (precoUnitario < 0 || precoUnitario > 999_999_999.99m || decimal.Round(precoUnitario, 2) != precoUnitario)
            throw new ArgumentException("Informe um preço unitário não negativo com até duas casas decimais.", nameof(precoUnitario));

        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        PrecoUnitario = precoUnitario;
    }

    public void DefinirAtivo(bool ativo) => Ativo = ativo;

    public void Reservar(decimal quantidade)
    {
        if (!QuantidadeValida(quantidade) || quantidade > SaldoDisponivel)
            throw new InvalidOperationException("Saldo disponível insuficiente para a reserva.");
        SaldoReservado += quantidade;
    }

    public void LiberarReserva(decimal quantidade)
    {
        if (!QuantidadeValida(quantidade) || quantidade > SaldoReservado)
            throw new InvalidOperationException("A quantidade a liberar excede a reserva do item.");
        SaldoReservado -= quantidade;
    }

    public EntradaEstoque RegistrarEntrada(decimal quantidade, DateTimeOffset registradaEmUtc, Guid usuarioId)
    {
        if (!QuantidadeValida(quantidade))
            throw new ArgumentException(Tipo == TipoItemEstoque.Peca
                ? "Peças exigem quantidade inteira e positiva."
                : "Insumos exigem quantidade positiva com até três casas decimais.", nameof(quantidade));
        if (SaldoFisico > SaldoMaximo - quantidade)
            throw new ArgumentException("A entrada excede a capacidade do saldo.", nameof(quantidade));
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("Informe o responsável pela entrada.", nameof(usuarioId));

        SaldoFisico += quantidade;
        return new EntradaEstoque(Id, quantidade, registradaEmUtc, usuarioId);
    }

    public bool QuantidadeValida(decimal quantidade) =>
        quantidade > 0 && quantidade <= SaldoMaximo && decimal.Round(quantidade, 3) == quantidade &&
        (Tipo != TipoItemEstoque.Peca || decimal.Truncate(quantidade) == quantidade);
}
