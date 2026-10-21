namespace OficinaMecanicaFase1.Api.Contracts;

public sealed record AutenticacaoInternaRequest(string? Email, string? Senha);
public sealed record AutenticacaoInternaResponse(string Token, string Tipo, DateTimeOffset ExpiraEmUtc);
public sealed record CadastrarUsuarioInternoRequest(string? Email, string? Senha, string? Papel);
public sealed record UsuarioInternoResponse(Guid Id, string Email, string Papel);
public sealed record IdentidadeInternaResponse(string Nome, string[] Papeis);
