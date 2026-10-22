# Cobertura dos testes do domínio

Medição em 22/10/2026, com .NET 10, xUnit, WebApplicationFactory e SQL Server
isolado pelo Testcontainers. A suíte passou com **32 testes**, sem falhas ou
testes ignorados.

| Área do domínio | Linhas cobertas | Cobertura de linhas |
| --- | ---: | ---: |
| Cadastros | 54 / 58 | 93,10% |
| Catálogo | 16 / 17 | 94,12% |
| Estoque | 94 / 108 | 87,04% |
| Orçamentos | 74 / 82 | 90,24% |
| Ordens de serviço | 217 / 233 | 93,13% |
| **Domínio completo** | **455 / 498** | **91,36%** |

A cobertura de ramos foi 325 / 458 (70,96%). O critério de 80% do plano foi
avaliado por **linhas** das classes do projeto `OficinaMecanicaFase1.Domain`.
Todas as áreas do domínio superaram esse percentual. A métrica de ramos fica
registrada separadamente e não foi usada como substituto da cobertura de linhas.

A suíte cobre abertura, diagnóstico, orçamento, aprovação, reserva, consumo,
finalização e entrega; rejeição do orçamento; aditivos aprovados e rejeitados;
recusa de reserva, reposição e nova tentativa; e concorrência na última unidade
de estoque. Os testes unitários isolam as regras de transição, cronologia,
quantidade e saldo, além do cálculo do tempo médio.

Para reproduzir a medição na raiz do repositório, com Docker Desktop em
execução:

```powershell
dotnet test .\OficinaMecanicaFase1.slnx -c Release --settings .\coverage.runsettings --collect:"XPlat Code Coverage" --results-directory .\TestResults
```

O XML Cobertura fica em `TestResults/<id>/coverage.cobertura.xml`. Essa pasta é
ignorada pelo Git. `coverage.runsettings` inclui apenas o assembly de domínio;
testes, API, infraestrutura, migrations e código gerado não entram no percentual.
