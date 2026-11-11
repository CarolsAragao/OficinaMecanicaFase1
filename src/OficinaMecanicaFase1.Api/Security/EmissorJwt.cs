using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanicaFase1.Application.AcessoInterno;

namespace OficinaMecanicaFase1.Api.Security;

public sealed class EmissorJwt(ConfiguracaoJwt configuracao, TimeProvider tempo) : IEmissorTokenInterno
{
    public TokenInterno Emitir(UsuarioInternoDados usuario)
    {
        var agora = tempo.GetUtcNow();
        var vencimento = agora.AddMinutes(15);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.Nome),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(usuario.Papeis.Select(papel => new Claim("role", papel)));

        var token = new JwtSecurityToken(
            issuer: configuracao.Emissor,
            audience: configuracao.Publico,
            claims: claims,
            notBefore: agora.UtcDateTime,
            expires: vencimento.UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(configuracao.Chave), SecurityAlgorithms.HmacSha256));

        return new TokenInterno(new JwtSecurityTokenHandler().WriteToken(token), vencimento);
    }
}
