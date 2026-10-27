# Relatório de vulnerabilidades e limitações da auditoria

Auditoria atualizada em **27/10/2026**. Resultados de scans anteriores, datados
de 22/10/2026, não substituem a verificação atual.

| Verificação | Comando/procedimento | Resultado de 27/10/2026 |
| --- | --- | --- |
| Dependências NuGet, diretas e transitivas, dos cinco projetos | `dotnet list .\OficinaMecanicaFase1.slnx package --vulnerable --include-transitive --no-restore --format json`, após `dotnet restore -m:1`, com cache NuGet gravável | Exit code 0; JSON com cinco projetos, sem lista de pacotes vulneráveis. A consulta vale para os avisos disponíveis na fonte NuGet na data. |
| Analisadores .NET | `dotnet format analyzers .\OficinaMecanicaFase1.slnx --verify-no-changes --no-restore --severity warn` | **Não concluído**: o processo auxiliar falhou ao conectar ao named pipe por acesso negado. Não se declara zero avisos. |
| Semgrep para C# e segurança | Comandos do relatório de 22/10/2026 | **Não repetido**: executável Semgrep ausente e mecanismo Docker indisponível. Os números anteriores de zero achados são históricos, sem validação atual. |
| Docker Scout / CVEs da imagem | `docker scout cves mcr.microsoft.com/dotnet/aspnet:10.0` | **Não executado**: mecanismo Docker indisponível. Não há conclusão sobre CVEs da imagem base. |
| Arquivos rastreados e histórico Git | Busca por arquivos de segredo, marcadores de chave privada, atribuições de credenciais e caminhos pessoais, sem imprimir valores | Nenhum arquivo `.env`, `.key`, `.pfx` ou User Secrets aparece nos 439 objetos históricos inspecionados; nenhum marcador de chave privada foi encontrado. O histórico contém placeholders de configuração em `.env.example`/`compose.yaml` e caminhos locais antigos de documentação. Os caminhos locais foram removidos da versão de trabalho. |

A busca de segredos é heurística e não prova ausência absoluta de credenciais.
Exemplos de CPF/CNPJ e senhas de teste aparecem nos testes como dados
sintéticos; nenhuma credencial de execução foi identificada nos arquivos
rastreados pela busca realizada. Não foram exibidos valores sensíveis.

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
via Compose e o scan de CVEs da imagem. Repetir Semgrep em ambiente com o
executável disponível e `dotnet format analyzers` onde o named pipe do
processo auxiliar seja permitido. Analisar e corrigir achados concretos antes
da submissão. O resultado histórico de 22/10/2026 permanece apenas como
registro, não como prova atual.