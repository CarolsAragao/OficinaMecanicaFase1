namespace OficinaMecanicaFase1.Domain.Catalogo;

public sealed class ServicoCatalogo
{
    private ServicoCatalogo() { }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public decimal PrecoBase { get; private set; }
    public bool Ativo { get; private set; }

    public ServicoCatalogo(string nome, string? descricao, decimal precoBase)
    {
        Id = Guid.NewGuid();
        Ativo = true;
        Atualizar(nome, descricao, precoBase);
    }

    public void Atualizar(string nome, string? descricao, decimal precoBase)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 200)
            throw new ArgumentException("Informe o nome do serviço com até 200 caracteres.", nameof(nome));
        if (descricao?.Trim().Length > 500)
            throw new ArgumentException("A descrição deve ter até 500 caracteres.", nameof(descricao));
        if (precoBase < 0 || precoBase > 999_999_999.99m || decimal.Round(precoBase, 2) != precoBase)
            throw new ArgumentException("Informe um preço base não negativo com até duas casas decimais.", nameof(precoBase));

        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        PrecoBase = precoBase;
    }

    public void DefinirAtivo(bool ativo) => Ativo = ativo;
}
