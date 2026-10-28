# Relatório de vulnerabilidades e limitações da auditoria

Auditoria atualizada em **28/10/2026**. Resultados de scans anteriores, datados
de 22/10/2026, não substituem a verificação atual.

| Verificação | Comando/procedimento | Resultado |
| --- | --- | --- |
| Dependências NuGet, diretas e transitivas, dos cinco projetos | `dotnet list .\OficinaMecanicaFase1.slnx package --vulnerable --include-transitive --no-restore --format json`, após `dotnet restore -m:1`, com cache NuGet gravável | Exit code 0; JSON com cinco projetos, sem lista de pacotes vulneráveis. A consulta vale para os avisos disponíveis na fonte NuGet na data. |
| Analisadores .NET | `dotnet format analyzers .\OficinaMecanicaFase1.slnx --verify-no-changes --no-restore --severity warn` | **Não concluído**: o processo auxiliar falhou ao conectar ao named pipe por acesso negado. Não se declara zero avisos. |
| Semgrep para código-fonte | Docker: `semgrep scan --config auto src tests` | **Concluído em 28/10/2026**: 101 arquivos rastreados pelo Git, 83 regras executadas, 0 achados (0 bloqueantes). Nove arquivos foram ignorados conforme `.semgrepignore`. A tentativa anterior no terminal isolado havia falhado. |
| CVEs da imagem da API | Trivy 0.76.1; scan do arquivo `oficina-api.tar` exportado da imagem `oficina-validacao-api:latest` | **Concluído em 28/10/2026**: Ubuntu 24.04 com 26 achados (21 médios, 5 baixos, 0 altos/críticos); três entradas de dependências .NET com 0 achados. |
| Arquivos rastreados e histórico Git | Busca por arquivos de segredo, marcadores de chave privada, atribuições de credenciais e caminhos pessoais, sem imprimir valores | Nenhum arquivo `.env`, `.key`, `.pfx` ou User Secrets aparece nos 439 objetos históricos inspecionados; nenhum marcador de chave privada foi encontrado. O histórico contém placeholders de configuração em `.env.example`/`compose.yaml` e caminhos locais antigos de documentação. Os caminhos locais foram removidos da versão de trabalho. |

A busca de segredos é heurística e não prova ausência absoluta de credenciais.
Exemplos de CPF/CNPJ e senhas de teste aparecem nos testes como dados
sintéticos; nenhuma credencial de execução foi identificada nos arquivos
rastreados pela busca realizada. Não foram exibidos valores sensíveis.

## Análise do código-fonte

Em 28/10/2026, a execução em Docker terminou com sucesso: 101 arquivos rastreados pelo Git analisados, 83 regras executadas e 0 achados (0 bloqueantes). Nove arquivos compatíveis com .semgrepignore foram ignorados. O resultado vale apenas para os arquivos e regras efetivamente analisados; não prova ausência absoluta de falhas.

## Análise dos achados da imagem

Em 28/10/2026, o Trivy 0.76.1 analisou a imagem da API exportada como oficina-api.tar. Foram detectadas 26 vulnerabilidades em pacotes do Ubuntu 24.04: 21 médias e 5 baixas, nenhuma alta ou crítica. As três entradas de dependências .NET tiveram 0 achados. A saída enviada não informa versões corrigidas para esses 26 achados. Eles devem ser acompanhados; após atualizar a imagem base, é preciso reconstruir a API e repetir o scan. Docker Scout solicitou login, mas o Trivy produziu um resultado válido sem login.

## Configuração de segurança observada

- O JWT valida emissor, público, assinatura e expiração; as rotas internas
  exigem papéis do ASP.NET Core Identity.
- O cadastro valida CPF/CNPJ e placa no domínio. O código de decisão do
  cliente é comparado por hash e exige motivo para reemissão.
- O Compose vincula HTTP e SQL Server a `127.0.0.1` por padrão, com portas
  configuráveis. O Dockerfile usa o usuário `APP_UID` sem privilégios.
- O ambiente local usa HTTP sem TLS. Para exposição externa, é necessário
  configurar HTTPS e uma implantação adequada; a configuração local não
  demonstra segurança de produção.

## Próximas verificações

Com Docker Linux funcional, executar a suíte completa, a inicialização limpa
via Compose. Repetir Trivy após atualização da imagem base e confirmar `dotnet format analyzers` onde
o named pipe do processo auxiliar seja permitido. Analisar e corrigir achados concretos antes
da submissão. O resultado histórico de 22/10/2026 permanece apenas como
registro, não como prova atual.