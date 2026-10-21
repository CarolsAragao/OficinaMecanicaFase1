namespace OficinaMecanicaFase1.Api.Security;

public sealed class ConfiguracaoJwt
{
    public required string Emissor { get; init; }
    public required string Publico { get; init; }
    public required byte[] Chave { get; init; }

    public static ConfiguracaoJwt Ler(IConfiguration configuration)
    {
        var emissor = configuration["Jwt:Issuer"];
        var publico = configuration["Jwt:Audience"];
        var chaveBase64 = configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(emissor) || string.IsNullOrWhiteSpace(publico) || string.IsNullOrWhiteSpace(chaveBase64))
        {
            throw new InvalidOperationException("Configure Jwt:Issuer, Jwt:Audience e Jwt:SigningKey antes de iniciar a API.");
        }

        byte[] chave;
        try
        {
            chave = Convert.FromBase64String(chaveBase64);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Jwt:SigningKey deve ser uma chave aleatória em Base64.", exception);
        }

        if (chave.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey precisa conter ao menos 32 bytes aleatórios.");
        }

        return new ConfiguracaoJwt { Emissor = emissor, Publico = publico, Chave = chave };
    }
}
