# Cobertura dos domínios críticos

Medição executada em 27/10/2026, com .NET 10, xUnit e Coverlet. Foram
executados **31 testes sem dependência de Docker**, todos aprovados. O
comando foi:

```powershell
dotnet test .\OficinaMecanicaFase1.slnx -c Release --no-build -m:1 --filter "Category!=Integration" --settings .\coverage.runsettings --collect:"XPlat Code Coverage" --results-directory .\TestResults
```

| Área do domínio | Linhas cobertas | Cobertura de linhas |
| --- | ---: | ---: |
| Cadastros | 56 / 58 | 96,55% |
| Catálogo | 16 / 17 | 94,12% |
| Estoque | 99 / 108 | 91,67% |
| Orçamentos | 73 / 82 | 89,02% |
| Ordens de serviço | 215 / 233 | 92,27% |
| **Domínio completo** | **459 / 498** | **92,16%** |

Cobertura de ramificações: **388 / 458 (84,71%)**. O XML Cobertura está em
`docs/cobertura-auditoria.cobertura.xml`.
O arquivo `coverage.runsettings` inclui apenas o assembly
`OficinaMecanicaFase1.Domain`; API, aplicação, infraestrutura, migrations,
testes e código gerado ficam fora do denominador.

Na medição de 27/10, a suíte tinha **42 testes**. A migração da API para
controllers acrescentou um teste de contrato HTTP, totalizando **43 testes**
na branch `adicionando-controllers`. Nessa branch, os **32 testes sem Docker**
passaram; os 11 de integração com SQL Server do Testcontainers precisam ser
repetidos após a migração. Neste ambiente, o acesso ao pipe do Docker foi
negado, então a execução integral terminou com `DockerUnavailableException`
antes de testar esses fluxos. A cobertura acima continua sendo a medição das
regras de domínio de 27/10 e não representa uma nova medição da branch.
