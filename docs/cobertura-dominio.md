# Cobertura dos domínios críticos

Medição executada em 02/11/2026, com .NET 10, xUnit e Coverlet na versão atual do projeto. A suíte completa terminou com **43 testes aprovados, 0 falhas e 0 ignorados**: 32 testes sem Docker e 11 testes de integração com SQL Server descartável via Testcontainers. O comando executado foi:

```powershell
dotnet test .\OficinaMecanicaFase1.slnx -c Release -m:1 --settings .\coverage.runsettings --collect:"XPlat Code Coverage" --results-directory .\TestResults --logger "trx;LogFileName=auditoria-entrega-2026-11-02.trx"
```

| Área do domínio | Linhas cobertas | Cobertura de linhas |
| --- | ---: | ---: |
| Cadastros | 58 / 58 | 100,00% |
| Catálogo | 17 / 17 | 100,00% |
| Estoque | 102 / 108 | 94,44% |
| Orçamentos | 78 / 82 | 95,12% |
| Ordens de serviço | 219 / 233 | 93,99% |
| **Domínio completo** | **474 / 498** | **95,18%** |

Cobertura de ramificações: **389 / 458 (84,93%)**. O XML Cobertura desta execução está em `docs/cobertura-auditoria.cobertura.xml`.

O arquivo `coverage.runsettings` inclui apenas o assembly `OficinaMecanicaFase1.Domain`; API, aplicação, infraestrutura, migrations, testes e código gerado ficam fora do denominador. A suíte de integração usa contêineres descartáveis próprios e não depende dos dados do banco local da aplicação.
