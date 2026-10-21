using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanicaFase1.Infrastructure.Identity;

namespace OficinaMecanicaFase1.Api.Security;

public sealed class EmissorJwt(ConfiguracaoJwt configuracao, TimeProvider tempo)
{
    public TokenEmitido Emitir(UsuarioInterno usuario, IEnumerable<string> papeis)
    {
        var agora = tempo.GetUtcNow();
        var vencimento = agora.AddMinutes(15);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(papeis.Select(papel => new Claim("role", papel)));

        var token = new JwtSecurityToken(
            issuer: configuracao.Emissor,
            audience: configuracao.Publico,
            claims: claims,
            notBefore: agora.UtcDateTime,
            expires: vencimento.UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(configuracao.Chave), SecurityAlgorithms.HmacSha256));

        return new TokenEmitido(new JwtSecurityTokenHandler().WriteToken(token), vencimento);
    }
}

public sealed record TokenEmitido(string Valor, DateTimeOffset ExpiraEmUtc);
