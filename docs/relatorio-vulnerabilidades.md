# Relatório de vulnerabilidades e operação local

Verificação realizada em 22/10/2026 sobre o código e as dependências da
solução .NET 10.

| Verificação | Resultado | Análise |
| --- | --- | --- |
| `dotnet list OficinaMecanicaFase1.slnx package --vulnerable --include-transitive --format json` | Nenhum pacote vulnerável informado pelo NuGet | A consulta incluiu dependências diretas e transitivas dos cinco projetos. |
| Semgrep `p/csharp` | 0 achados em 107 arquivos C#, 27 regras | Nenhum padrão coberto por esse conjunto de regras foi identificado. |
| Semgrep `p/security-audit` | 0 achados em 147 arquivos, 3 regras aplicáveis | Verificação complementar; esse conjunto teve cobertura limitada para C#. |
| `dotnet format analyzers --verify-no-changes --no-restore --severity warn` | Nenhum aviso | Analisadores .NET configurados na solução passaram. |
| Arquivos rastreados pelo Git | Nenhum `.env`, arquivo de User Secrets ou chave privada rastreado | A busca foi feita pelos nomes dos arquivos, sem exibir valores. |
| Docker Scout, imagem base `mcr.microsoft.com/dotnet/aspnet:10.0` | Scan de CVEs não concluído | A ferramenta exigiu login Docker. Este relatório não afirma ausência de CVEs na imagem. |

O scan estático não substitui uma auditoria de segurança. A consulta do NuGet
reflete os avisos disponíveis na data da execução; uma nova execução pode
encontrar avisos publicados depois.

## Ajustes aplicados

- O template `.env.example` passou a exigir senha própria, sem senha pronta.
- Os scripts locais recusam a antiga senha de exemplo.
- A porta HTTP 5080 da API, assim como a 1433 do SQL Server, fica vinculada a
  `127.0.0.1` no Compose. O Dockerfile já executa a API como usuário sem
  privilégios; a imagem construída informou UID `1654`.

## Validação de operação

Uma instância isolada foi iniciada com credenciais temporárias, volume Docker
próprio e portas alternativas. O SQL Server ficou saudável, a API iniciou e
`/api/saude`, `/api/saude/banco`, `/openapi/v1.json` e `/swagger/index.html`
responderam HTTP `200`. Nenhum dado ou segredo dessa validação pertence ao
repositório.

Para repetir o scan de dependências e o scan estático na raiz do projeto:

```powershell
dotnet list .\OficinaMecanicaFase1.slnx package --vulnerable --include-transitive --format json
dotnet format analyzers .\OficinaMecanicaFase1.slnx --verify-no-changes --no-restore --severity warn
docker run --rm -v "${PWD}:/src" semgrep/semgrep semgrep scan --config p/csharp /src/src
```

Para completar a análise de CVEs da imagem, faça login no Docker Scout e
execute `docker scout cves mcr.microsoft.com/dotnet/aspnet:10.0 --only-severity critical,high`.
