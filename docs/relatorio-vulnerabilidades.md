# Relatório de vulnerabilidades e limitações da auditoria

Auditoria executada em **02/11/2026** sobre a `main` no commit `a6c63a5`, após a migração dos endpoints para controllers. Os resultados abaixo substituem os scans de 22/10 e 28/10 como evidência da versão atual. As ferramentas consultam regras e bases de avisos que podem mudar com o tempo.

| Verificação | Procedimento | Resultado |
| --- | --- | --- |
| Dependências NuGet, diretas e transitivas, dos cinco projetos | `dotnet restore .\OficinaMecanicaFase1.slnx -m:1` e `dotnet list .\OficinaMecanicaFase1.slnx package --vulnerable --include-transitive --no-restore --format json` | Concluído com exit code 0; os cinco projetos foram listados, sem pacotes vulneráveis reportados pela fonte NuGet consultada. |
| Analisadores .NET | `dotnet format analyzers .\OficinaMecanicaFase1.slnx --verify-no-changes --no-restore --severity warn` | Concluído com exit code 0 e sem avisos exibidos. Nenhum arquivo foi alterado pelo comando. |
| Semgrep 1.182.1, seleção automática de regras | Imagem Docker atualizada; `semgrep scan --config auto` em `src`, `tests`, `scripts`, `Dockerfile` e `docker-compose.yml` | 116 arquivos rastreados analisados, 122 regras aplicáveis, **0 achados**. Houve **1 erro parcial de análise de sintaxe** no construtor primário de `AvisosEstoqueController.cs`; 10 arquivos foram excluídos pelos padrões de ignorar do Semgrep. |
| Semgrep, auditoria de segurança e segredos | `semgrep scan --config p/security-audit --config p/secrets --metrics off` nos mesmos alvos | 41 regras aplicáveis, 116 arquivos analisados, **0 achados e 0 erros de análise**. Esse conjunto é predominantemente genérico e não elimina a limitação da análise C# automática. |
| Semgrep, busca de segredos no repositório | `semgrep scan --config p/secrets --metrics off .` | 37 regras aplicáveis, 186 arquivos rastreados aceitos pelo scanner, **0 achados e 0 erros de análise**. Arquivos binários ou ignorados não são cobertos por esse resultado. |
| CVEs da imagem da API | `docker build --pull` do `Dockerfile` atual; imagem exportada e analisada com Trivy 0.76.1 e base de vulnerabilidades baixada nesta execução | **26 achados em pacotes do Ubuntu 24.04: 21 médios e 5 baixos; nenhum alto ou crítico.** Três arquivos de dependências .NET foram analisados com **0 achados**. Nenhum dos 26 registros informou versão corrigida. |
| Arquivos de segredo rastreados | Consulta aos nomes versionados no Git para `.env`, `.key`, `.pfx`, `.pem`, `.p12` e `secrets.json` | Nenhum arquivo com esses nomes/extensões foi encontrado entre os arquivos rastreados. Esta verificação não examinou novamente todo o histórico Git. |

## Análise do código e das dependências

O scan automático do Semgrep incluiu 33 regras C# aplicáveis, além de regras genéricas e para JSON, YAML e Dockerfile. Seu resultado de zero achados é limitado pelo erro de parsing em `src/OficinaMecanicaFase1.Api/Controllers/AvisosEstoqueController.cs`, linha 12, no construtor primário C#. O arquivo foi inspecionado manualmente: expõe uma consulta de avisos de reserva, exige a política `Estoquistas` e delega a leitura ao gerenciador de reservas. Essa inspeção pontual não substitui uma análise estática completa do arquivo. O scanner informou cerca de 99,9% das linhas analisadas, 10 alvos ignorados e análise restrita aos arquivos rastreados pelo Git.

A consulta NuGet não reportou vulnerabilidades conhecidas nas dependências diretas ou transitivas dos cinco projetos. O resultado depende dos avisos disponíveis nas fontes consultadas em 02/11/2026. O comando de analisadores .NET, que estava inconclusivo no relatório anterior, passou nesta execução.

## Análise da imagem

A imagem analisada foi reconstruída da `main` atual, com atualização das bases `mcr.microsoft.com/dotnet/sdk:10.0` e `mcr.microsoft.com/dotnet/aspnet:10.0` pelo `--pull`. O Trivy identificou Ubuntu 24.04 e runtime ASP.NET Core/.NET 10.0.14. Os 26 registros de CVE se concentram em pacotes do sistema como `libpcre2-8-0`, `libgnutls30t64`, `libc6`, `libc-bin`, `dash` e `tar`; 21 têm gravidade média e 5 baixa. A saída não informou versões corrigidas para eles. Não houve achados altos ou críticos, nem achados nos três arquivos de dependências .NET examinados.

O uso da imagem base oficial e de `USER $APP_UID` reduz parte da exposição, mas não elimina os avisos do sistema operacional. A equipe deve acompanhar as correções da imagem base, reconstruir a API e repetir o Trivy quando houver atualização. Não se deve interpretar “0 achados no código” como prova de ausência de vulnerabilidades.

## Configuração de segurança observada e limites

- O JWT valida emissor, público, assinatura e expiração; rotas internas exigem papéis do ASP.NET Core Identity.
- CPF/CNPJ e placa são validados no domínio. O código de decisão do cliente é armazenado como hash e a reemissão exige motivo.
- O Compose publica HTTP e SQL Server em `127.0.0.1` por padrão; a imagem executa a API sem privilégios de root.
- O ambiente local usa HTTP sem TLS. Exposição fora da máquina exige HTTPS e configuração de implantação apropriada.
- Os scans são baseados em regras e bases de avisos, não incluem teste dinâmico de invasão e têm as exclusões e o erro parcial de parser descritos acima. A busca de segredos não prova ausência absoluta de credenciais.

Os testes automatizados da versão atual e a cobertura dos domínios críticos estão documentados em `docs/cobertura-dominio.md`.
