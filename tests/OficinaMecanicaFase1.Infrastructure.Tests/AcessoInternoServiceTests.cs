using OficinaMecanicaFase1.Application.AcessoInterno;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class AcessoInternoServiceTests
{
    [Fact]
    public async Task LoginEmiteTokenSomenteParaPapelInterno()
    {
        var identidade = new IdentidadeFalsa
        {
            UsuarioAutenticado = new UsuarioInternoDados(Guid.NewGuid(), "atendente@teste.local",
                "atendente@teste.local", [PapeisInternos.Atendente])
        };
        var emissor = new EmissorFalso();
        var acesso = new AcessoInternoService(identidade, emissor);

        Assert.NotNull(await acesso.AutenticarAsync("atendente@teste.local", "senha"));
        Assert.Equal(1, emissor.Emissoes);

        identidade.UsuarioAutenticado = identidade.UsuarioAutenticado with { Papeis = ["Cliente"] };
        Assert.Null(await acesso.AutenticarAsync("atendente@teste.local", "senha"));
        Assert.Equal(1, emissor.Emissoes);
    }

    [Fact]
    public async Task CadastroRecusaPapelInvalidoEDuplicidadeAntesDeCriarUsuario()
    {
        var identidade = new IdentidadeFalsa();
        var acesso = new AcessoInternoService(identidade, new EmissorFalso());

        var papelInvalido = await acesso.CadastrarAsync("cliente@teste.local", "senha", "Cliente");
        Assert.Equal(FalhaCadastroInterno.DadosInvalidos, papelInvalido.Falha);
        Assert.Equal(0, identidade.Cadastros);

        identidade.EmailEmUso = true;
        var duplicado = await acesso.CadastrarAsync("atendente@teste.local", "senha", PapeisInternos.Atendente);
        Assert.Equal(FalhaCadastroInterno.EmailDuplicado, duplicado.Falha);
        Assert.Equal(0, identidade.Cadastros);
    }

    private sealed class IdentidadeFalsa : IIdentidadeInterna
    {
        public UsuarioInternoDados? UsuarioAutenticado { get; set; }
        public bool EmailEmUso { get; set; }
        public int Cadastros { get; private set; }

        public Task<UsuarioInternoDados?> AutenticarAsync(string email, string senha) =>
            Task.FromResult(UsuarioAutenticado);

        public Task<bool> EmailEmUsoAsync(string email) => Task.FromResult(EmailEmUso);

        public Task<ResultadoCadastroInterno> CadastrarAsync(string email, string senha, string papel)
        {
            Cadastros++;
            return Task.FromResult(new ResultadoCadastroInterno(
                new UsuarioInternoDados(Guid.NewGuid(), email, email, [papel]),
                FalhaCadastroInterno.Nenhuma));
        }
    }

    private sealed class EmissorFalso : IEmissorTokenInterno
    {
        public int Emissoes { get; private set; }

        public TokenInterno Emitir(UsuarioInternoDados usuario)
        {
            Emissoes++;
            return new TokenInterno("token-de-teste", DateTimeOffset.UtcNow.AddMinutes(15));
        }
    }
}
