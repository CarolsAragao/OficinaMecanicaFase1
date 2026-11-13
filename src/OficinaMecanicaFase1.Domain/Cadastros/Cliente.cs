using System.Net.Mail;

namespace OficinaMecanicaFase1.Domain.Cadastros;

public sealed class Cliente
{
    private Cliente() { }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Documento { get; private set; } = null!;
    public string? Email { get; private set; }

    public Cliente(string nome, string documento, string? email = null)
    {
        Id = Guid.NewGuid();
        Atualizar(nome, documento, email);
    }

    public void Atualizar(string nome, string documento, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 200)
            throw new ArgumentException("Informe um nome com até 200 caracteres.", nameof(nome));
        var normalizado = DocumentoCliente.Normalizar(documento);
        if (normalizado is null)
            throw new ArgumentException("Informe um CPF ou CNPJ válido.", nameof(documento));
        var emailNormalizado = email?.Trim();
        if (!string.IsNullOrEmpty(emailNormalizado) &&
            (emailNormalizado.Length > 254 ||
             !MailAddress.TryCreate(emailNormalizado, out var endereco) ||
             !string.Equals(endereco.Address, emailNormalizado, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Informe um e-mail válido com até 254 caracteres.", nameof(email));
        Nome = nome.Trim();
        Documento = normalizado;
        if (email is not null)
            Email = string.IsNullOrEmpty(emailNormalizado) ? null : emailNormalizado;
    }
}
