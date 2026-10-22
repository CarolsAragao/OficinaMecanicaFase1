# Oficina Mecânica — Fase 1

Este diretório é a raiz do **repositório do back-end** da oficina mecânica. Os diagramas e demais artefatos DDD estão em `Docs-tech-challenge/oficina_mecanica`, fora deste repositório, e orientam as regras de negócio.

## Estrutura

- `OficinaMecanicaFase1.slnx`: solução .NET 10.
- `src/OficinaMecanicaFase1.Api`: interface HTTP, contratos de entrada/saída e OpenAPI.
- `src/OficinaMecanicaFase1.Application`: coordenação dos casos de uso.
- `src/OficinaMecanicaFase1.Domain`: regras e conceitos do domínio.
- `src/OficinaMecanicaFase1.Infrastructure`: adaptações técnicas e persistência.
- `tests/OficinaMecanicaFase1.Infrastructure.Tests`: integração com um SQL Server isolado por teste.
- `compose.yaml`: API e SQL Server local com volume persistente.
- `scripts/Configurar-Depuracao-Local.ps1`: conexão local do Visual Studio armazenada nos User Secrets.
- `scripts/Configurar-Acesso-Interno.ps1`: chave JWT local e primeiro administrador fora do Git.

Todos os projetos pertencem ao mesmo back-end monolítico. As referências seguem `Api → Application`, `Api → Infrastructure`, `Infrastructure → Application` e `Application → Domain`. Os contextos DDD são limites de linguagem e responsabilidade dentro do monólito.

## Executar com Docker Compose

Copie `.env.example` para `.env` e substitua a senha de exemplo por uma senha forte sua. `.env` está ignorado pelo Git. Em seguida, gere a chave JWT e informe, quando solicitado, o e-mail e uma senha própria para o primeiro administrador. Não use a senha do SQL Server como senha da conta. Use o PowerShell na raiz do projeto:

```powershell
Copy-Item .env.example .env
notepad .env
powershell -ExecutionPolicy Bypass -File .\scripts\Configurar-Acesso-Interno.ps1
docker compose config --quiet
docker compose up --build -d
docker compose ps
Invoke-RestMethod http://localhost:5080/api/saude
Invoke-RestMethod http://localhost:5080/api/saude/banco
```

A API aguarda o SQL Server ficar saudável e aplica as migrations ao iniciar. A primeira inicialização pode levar alguns minutos. O Swagger fica em `http://localhost:5080/swagger`. Para inspecionar erros, use `docker compose logs api sqlserver`. Para parar sem apagar os dados, use `docker compose down`; o volume `dados_sqlserver` guarda os arquivos do SQL Server. `docker compose down -v` apaga esse volume e seus dados, portanto só o execute quando quiser descartá-los. A porta 1433 do SQL Server fica acessível apenas por `127.0.0.1` para depuração local. O SQL Server Developer serve para desenvolvimento e testes, não é uma licença de produção.

## Depurar a API pelo Visual Studio

Na raiz do repositório, execute o primeiro script para salvar a conexão nos User Secrets da API, fora do repositório. Ele usa a senha do `.env` existente; se o arquivo ainda não existir, solicita uma senha e o cria. Alterar a senha no arquivo não muda a senha de um banco que já usa o volume persistente. O segundo script gera uma chave JWT e solicita os dados do administrador ausentes, ou reaproveita os dados locais, e grava os User Secrets correspondentes.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Configurar-Depuracao-Local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\Configurar-Acesso-Interno.ps1
docker compose stop api
docker compose up -d sqlserver
docker compose ps
```

No Visual Studio, selecione `OficinaMecanicaFase1.Api` como projeto de inicialização, o perfil `http` e pressione F5. O perfil abre `http://localhost:5080/swagger`. A API rodando pelo Visual Studio usa o banco `OficinaMecanica` do SQL Server no Compose e aplica migrations pendentes ao iniciar. Para voltar a executar a API em contêiner, encerre a depuração no Visual Studio e use `docker compose up --build -d api`. Não execute as duas APIs simultaneamente na mesma porta 5080.

## Contas internas e JWT

No primeiro início, a API aplica a migration da Identity e cria os papéis `Administrador`, `Atendente`, `Mecânico` e `Estoquista`. Se ainda não existir um administrador, cria a conta informada em `Bootstrap:AdminEmail` e `Bootstrap:AdminPassword`. Sem essas configurações, a inicialização é interrompida com uma mensagem específica; um e-mail já pertencente a outra conta não é elevado automaticamente. A senha é armazenada pelo ASP.NET Core Identity como hash. Após o primeiro início bem-sucedido, a senha de bootstrap pode ser removida do `.env` e dos User Secrets; reinicializações usam a conta existente.

| Papel | Responsabilidade inicial |
| --- | --- |
| Administrador | Cria contas internas, administra clientes e veículos e mantém serviços, peças e insumos do catálogo. |
| Atendente | Identifica clientes e veículos, abre ordens de serviço, entrega o código de decisão do orçamento e registra a entrega nos fluxos correspondentes. |
| Mecânico | Inicia e registra o diagnóstico, solicita a geração do orçamento e, nos fluxos seguintes, executa a OS e registra o consumo. |
| Estoquista | Registra entradas de estoque; avisos de reserva recusada nos fluxos seguintes. |

`POST /api/interno/autenticacao` recebe `email` e `senha` e devolve um JWT padrão válido por 15 minutos. Cinco tentativas de senha incorreta bloqueiam a conta por 15 minutos. `GET /api/interno/identidade` exige um dos quatro papéis internos; `POST /api/interno/usuarios` exige `Administrador` e permite criar contas com um dos quatro papéis. O documento OpenAPI indica Bearer nas rotas protegidas, e o botão **Authorize** do Swagger aceita o token devolvido pelo login. Rotas `/api/saude` e `/api/saude/banco` continuam públicas. O cliente da oficina não é uma conta interna. A consulta da proposta e a decisão usam número da OS, CPF/CNPJ e, para decidir, um código individual emitido pelo Atendente.

Se a senha inicial do Administrador precisar ser redefinida, pare a API e execute `powershell -ExecutionPolicy Bypass -File .\scripts\Redefinir-Administrador.ps1`. O comando solicita o e-mail da conta e a nova senha duas vezes, sem exibir os caracteres digitados. A senha precisa atender às regras do Identity.

Se contas de teste aparecerem no banco local, execute `powershell -ExecutionPolicy Bypass -File .\scripts\Limpar-Contas-Teste.ps1`. O comando remove somente `admin@teste.local` e `atendente@teste.local`, recria o Administrador configurado se necessário e solicita a redefinição segura da senha.

Com Docker Desktop ligado, execute `dotnet test .\OficinaMecanicaFase1.slnx -c Release` para rodar os testes em SQL Server descartável. Eles verificam ausência de token, token inválido ou expirado, login, acesso permitido e papel sem permissão. A aplicação precisa de uma chave de 32 bytes aleatórios em Base64; o script a gera e a guarda fora do repositório.

## Executar a API no host

Para executar sem contêiner da API, mantenha o SQL Server em execução e configure a conexão pelos User Secrets como indicado acima. Também é possível usar uma variável de ambiente local no PowerShell:

```powershell
$env:ConnectionStrings__Oficina = 'Server=localhost,1433;Database=OficinaMecanica;User Id=sa;Password=SENHA_LOCAL;Encrypt=True;TrustServerCertificate=True'
dotnet restore .\OficinaMecanicaFase1.slnx
dotnet build .\OficinaMecanicaFase1.slnx -c Release
dotnet run --project .\src\OficinaMecanicaFase1.Api\OficinaMecanicaFase1.Api.csproj --launch-profile http
```

Substitua `SENHA_LOCAL` no terminal, sem salvar a senha no código ou em arquivo versionado. Limpe a variável quando terminar: `Remove-Item Env:ConnectionStrings__Oficina`.

Com a aplicação em `http://localhost:5080`, confira `GET /api/saude/banco` para a conectividade com SQL Server. `GET /api/saude` confirma apenas o processo HTTP. `/openapi/v1.json` e `/swagger` ficam disponíveis em `Development`.

## Migrações e teste de persistência

O modelo EF Core inicial tem a tabela técnica `RegistrosPersistencia`, criada pela migration `CriarPersistenciaInicial`; ela permite verificar gravação e leitura sem antecipar as entidades do negócio. A migration está versionada em `src/OficinaMecanicaFase1.Infrastructure/Persistence/Migrations`. Para mudanças futuras no modelo, instale a ferramenta de migrations do EF Core 10 e gere uma nova migration apontando para o projeto de infraestrutura e o projeto de inicialização da API. Uma migration deve ser revisada antes de aplicar em outros ambientes.

```powershell
dotnet test .\OficinaMecanicaFase1.slnx -c Release
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -No -Q 'SELECT MigrationId FROM OficinaMecanica.dbo.__EFMigrationsHistory'
```

O teste de integração cria um SQL Server descartável próprio por meio do Testcontainers, aplica a migration, grava um registro e lê o mesmo registro em outro `DbContext`. Docker Desktop deve estar ligado para `dotnet test`. O comando `sqlcmd` solicita a senha interativamente. Os testes não usam o volume da aplicação.

## Clientes e veículos

As rotas abaixo exigem o JWT obtido em `POST /api/interno/autenticacao`. `Administrador` e `Atendente` podem cadastrar, consultar e atualizar. Só `Administrador` pode excluir. Os campos de cliente são `nome` e `documento`; os de veículo são `clienteId`, `placa`, `marca`, `modelo` e `ano`. Na atualização de veículo, envie apenas `placa`, `marca`, `modelo` e `ano`: o proprietário permanece o mesmo.

| Método | Rota | Uso |
| --- | --- | --- |
| GET | `/api/clientes?documento=529.982.247-25` | Identificar cliente por CPF/CNPJ; retorna lista vazia se o documento válido não estiver cadastrado. Sem filtro, lista todos. |
| GET | `/api/clientes/{id}` | Consultar cliente. |
| POST | `/api/clientes` | Cadastrar cliente. |
| PUT | `/api/clientes/{id}` | Atualizar nome e documento. |
| DELETE | `/api/clientes/{id}` | Excluir cliente sem veículos ou outras referências. |
| GET | `/api/veiculos?clienteId={id}` | Listar veículos do cliente; sem filtro, lista todos. |
| GET | `/api/veiculos/{id}` | Consultar veículo. |
| POST | `/api/veiculos` | Cadastrar veículo de cliente existente. |
| PUT | `/api/veiculos/{id}` | Atualizar dados do veículo sem trocar o proprietário. |
| DELETE | `/api/veiculos/{id}` | Excluir veículo sem referências. |

Exemplo de `POST /api/clientes`: `{"nome":"Ana Maria","documento":"529.982.247-25"}`. Exemplo de `POST /api/veiculos`: `{"clienteId":"ID_DO_CLIENTE","placa":"ABC1D23","marca":"Fiat","modelo":"Uno","ano":2020}`. Substitua `ID_DO_CLIENTE` pelo `id` retornado no cadastro do cliente.

O CPF/CNPJ é validado pelos dígitos verificadores e armazenado só com números. A placa aceita o formato brasileiro antigo (`ABC1234` ou `ABC-1234`) e Mercosul (`ABC1D23`), e é armazenada em maiúsculas sem hífen. Documento e placa são únicos também no banco. O vínculo `Veiculo.ClienteId` tem chave estrangeira para cliente e é usado para conferir se o veículo selecionado na abertura da OS pertence ao cliente informado. As tabelas `Clientes` e `Veiculos` são criadas pela migration `CadastrarClientesEVeiculos` quando a API inicia.

Entradas inválidas retornam `400`; ausência de cadastro, `404`; documento ou placa duplicados e exclusão de cadastro em uso, `409`; falta de autenticação, `401`; papel sem permissão, `403`. O Swagger em `/swagger` apresenta os contratos e indica Bearer nas rotas protegidas.

## Serviços, peças e insumos

As leituras abaixo exigem JWT de qualquer papel interno. `Administrador` cria, atualiza, desativa e reativa serviços e itens. `Estoquista` registra a entrada física de peças e insumos. A desativação preserva o cadastro e suas entradas; `somenteAtivos=true` mostra o catálogo selecionável para uma nova OS.

| Método | Rota | Uso |
| --- | --- | --- |
| GET | `/api/servicos?somenteAtivos=true` | Listar serviços ativos; sem o filtro, inclui desativados. |
| GET | `/api/servicos/{id}` | Consultar serviço. |
| POST | `/api/servicos` | Cadastrar serviço. |
| PUT | `/api/servicos/{id}` | Atualizar nome, descrição e preço base. |
| DELETE | `/api/servicos/{id}` | Desativar sem excluir o histórico. |
| PATCH | `/api/servicos/{id}/ativacao` | Reativar ou desativar com `{"ativo":true}` ou `{"ativo":false}`. |
| GET | `/api/itens-estoque?somenteAtivos=true` | Listar peças e insumos ativos; sem o filtro, inclui desativados. |
| GET | `/api/itens-estoque/{id}` | Consultar item e saldos físico, reservado e disponível. |
| POST | `/api/itens-estoque` | Cadastrar peça ou insumo. |
| PUT | `/api/itens-estoque/{id}` | Atualizar nome, descrição e preço unitário. |
| DELETE | `/api/itens-estoque/{id}` | Desativar sem excluir o histórico. |
| PATCH | `/api/itens-estoque/{id}/ativacao` | Reativar ou desativar. |
| POST | `/api/itens-estoque/{id}/entradas` | Estoquista registra entrada de quantidade. |
| GET | `/api/itens-estoque/{id}/entradas` | Consultar o histórico de entradas. |

Exemplos de corpo JSON: serviço `{"nome":"Alinhamento","descricao":"Eixos dianteiros","precoBase":120.00}`; peça `{"nome":"Filtro de óleo","tipo":"Peca","unidadeMedida":"un","precoUnitario":20.00}`; insumo `{"nome":"Óleo","tipo":"Insumo","unidadeMedida":"L","precoUnitario":15.50}`; entrada `{"quantidade":1.250}`. Os cadastros começam com saldo físico e reservado em zero. O preço aceita duas casas decimais; a quantidade aceita até três. Para peças, a quantidade deve ser inteira e a unidade deve ser `un`. Insumos aceitam quantidades fracionárias. Tipo e unidade de medida são definidos no cadastro inicial e não mudam na atualização.

O saldo disponível é o físico menos o reservado. Nesta versão, uma entrada aumenta somente o saldo físico e grava um registro com instante UTC e usuário responsável. Cada atualização de saldo usa controle de concorrência; se outra operação alterou o item, a API responde `409` para que o saldo seja consultado antes de tentar novamente. Valor inválido retorna `400`, cadastro inexistente `404`, ausência de JWT `401` e papel sem permissão `403`. A migration `CriarCatalogoEEntradasEstoque` cria as tabelas e restrições ao iniciar a API.

## Ordens de serviço

`POST /api/ordens-servico` abre uma ordem com cliente, veículo e ao menos um serviço ativo do catálogo. Só aceita veículo associado ao cliente informado. Os itens necessários são opcionais na abertura; a API aceita peça em quantidade inteira e insumo fracionário com até três casas decimais. Um número sequencial é gerado pelo SQL Server, a OS começa em `Recebida` e grava o marco `OS recebida` com instante UTC. A rota exige JWT de Administrador ou Atendente.

Exemplo de corpo:

```json
{
  "clienteId": "GUID-DO-CLIENTE",
  "veiculoId": "GUID-DO-VEICULO-DO-CLIENTE",
  "servicosIds": ["GUID-DO-SERVICO-ATIVO"],
  "itensNecessarios": [
    { "itemEstoqueId": "GUID-DA-PECA-ATIVA", "quantidade": 1 },
    { "itemEstoqueId": "GUID-DO-INSUMO-ATIVO", "quantidade": 0.5 }
  ]
}
```

| Método | Rota | Uso |
| --- | --- | --- |
| POST | `/api/ordens-servico` | Abrir OS como `Recebida`; exige Administrador ou Atendente. |
| GET | `/api/ordens-servico/{numero}` | Consultar serviços, itens, status e marcos da OS. |
| POST | `/api/ordens-servico/{numero}/servicos` | Acrescentar serviço ativo antes da aprovação do orçamento; exige Atendente ou Mecânico. Corpo: `{"servicoCatalogoId":"GUID"}`. |
| POST | `/api/ordens-servico/{numero}/itens` | Acrescentar item ativo e consultar sua disponibilidade; exige Atendente ou Mecânico. Corpo: `{"itemEstoqueId":"GUID","quantidade":1}`. |
| POST | `/api/ordens-servico/{numero}/diagnostico/inicio` | Mecânico inicia o diagnóstico e muda a OS para `EmDiagnostico`. |
| POST | `/api/ordens-servico/{numero}/diagnostico` | Mecânico registra a avaliação. Corpo: `{"descricao":"Pastilhas desgastadas"}`. |
| POST | `/api/ordens-servico/{numero}/orcamento` | Após o diagnóstico, o sistema calcula a proposta e muda a OS para `AguardandoAprovacao`; exige Mecânico. |
| GET | `/api/ordens-servico/{numero}/orcamento` | Qualquer papel interno consulta a proposta, com serviços, itens, quantidades e totais. |
| POST | `/api/ordens-servico/{numero}/codigo-acesso/emissao` | Atendente emite o código individual; na reemissão, exige motivo e invalida o anterior. Corpo: `{"motivo":null}` na primeira emissão. |

Ao incluir peça ou insumo, a OS registra o saldo disponível consultado naquele momento. Isso não reserva nem baixa estoque; reserva e consumo pertencem aos fluxos posteriores de aprovação e execução. Serviço ou item inativo, veículo de outro cliente, serviço ausente, repetição ou quantidade inválida retorna `400`; número de OS inexistente retorna `404`. A migration `CriarOrdensServico` cria a OS, os serviços e itens associados e os marcos. Os seis status oficiais estão definidos no domínio; a abertura usa somente `Recebida`.

O orçamento só pode ser gerado depois de o Mecânico iniciar e registrar o diagnóstico. O sistema usa os preços atuais dos serviços e itens associados à OS e grava uma cópia dos nomes, unidades, preços e valores calculados; mudanças posteriores no catálogo não alteram a proposta. Cada serviço tem quantidade 1, e itens usam a quantidade necessária registrada na OS. O valor dos itens é arredondado para duas casas decimais. A geração salva orçamento, linhas, status e marco na mesma transação; uma OS tem no máximo um orçamento. A proposta fica disponível pela API, sem envio de e-mail. A consulta do cliente usa as rotas próprias abaixo. Gerar antes do diagnóstico, repetir o diagnóstico ou gerar novamente retorna `400`. A migration `CriarDiagnosticosEOrcamentos` cria as novas colunas e tabelas.

## Decisão do cliente sobre o orçamento

Depois de gerar o orçamento, o Atendente chama `POST /api/ordens-servico/{numero}/codigo-acesso/emissao` com seu JWT. A resposta traz o código individual **uma única vez** para que o Atendente o entregue ao cliente. Apenas o hash fica armazenado. Para reemitir, envie `{"motivo":"Cliente perdeu o código"}`; a emissão fica registrada e o código anterior deixa de funcionar. A rota exige o papel `Atendente`, e a resposta não pode ser armazenada em cache.

As duas rotas abaixo são públicas e recebem o CPF/CNPJ no corpo da requisição, sem colocá-lo na URL:

| Método | Rota | Corpo | Resultado |
| --- | --- | --- | --- |
| POST | `/api/cliente/ordens-servico/{numero}/orcamento/consulta` | `{"documento":"529.982.247-25"}` | Proposta e decisão atual, quando número e documento pertencem ao mesmo cliente. |
| POST | `/api/cliente/ordens-servico/{numero}/orcamento/decisao` | `{"documento":"529.982.247-25","codigoAcesso":"CODIGO_ENTREGUE","aprovar":true,"motivo":null}` | Aprova a proposta. Para rejeitar, use `"aprovar":false` e informe o motivo. |

A consulta pública retorna `404` para OS ou documento sem correspondência e nunca inclui o código. A decisão exige o código vigente; credenciais incorretas também retornam `404`. Rejeição sem motivo retorna `400`; proposta já decidida retorna `409`, inclusive se tentarem inverter a decisão. Aprovação registra data e marco, mas a OS só avança à execução após o fluxo de reservas. Rejeição registra motivo, data e marcos próprios de encerramento, libera eventuais reservas na mesma transação e mantém o último status oficial `AguardandoAprovacao`. Ela não é apresentada como `Finalizada` ou `Entregue`. A migration `RegistrarDecisaoOrcamentoEAcessoCliente` adiciona decisão, emissão de código e registro de reservas.

O SQL Server é apropriado aqui porque o domínio tem relacionamentos entre cliente, veículo, OS, orçamento e estoque; chaves, restrições e transações ajudam a preservar consistência, especialmente em futuras reservas e baixas. A equipe também já conhece esse banco. EF Core fornece mapeamento e migrations versionadas, e a imagem Developer permite desenvolvimento local reproduzível.

## Consultar a API

No PowerShell, dentro da raiz do repositório:

```powershell
Invoke-RestMethod http://localhost:5080/api/saude
Invoke-RestMethod http://localhost:5080/api/saude/banco
Invoke-RestMethod http://localhost:5080/openapi/v1.json
Start-Process http://localhost:5080/swagger
```

`GET /api/saude` retorna `{"status":"operacional"}`. A interface Swagger usa os arquivos do pacote instalado, sem dependência de CDN.

As configurações comuns ficam em `src/OficinaMecanicaFase1.Api/appsettings.json`. A conexão é obrigatória via variável `ConnectionStrings__Oficina` ou User Secrets `ConnectionStrings:Oficina`; não grave senhas nem a chave JWT em arquivos versionados. Falhas inesperadas e respostas HTTP sem corpo usam `application/problem+json` quando o cliente aceita JSON, com um identificador `traceId` para suporte.

## Local previsto

`C:\Users\Carol\Desktop\OficinaMecanicaFase1`

## Pré-requisitos

- SDK .NET **10** (verificar com `dotnet --list-sdks`).
- Docker Desktop iniciado, usando contêineres **Linux** com arquitetura **x86-64/amd64** e memória suficiente para SQL Server (ao menos 2 GB destinados ao mecanismo; recomenda-se mais para API e demais processos).
- Docker Compose disponível (`docker compose version`).
- Git configurado e acesso a uma conta capaz de criar o repositório.

No PowerShell, dentro desta pasta, execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verificar-Ambiente.ps1
```

O script só lê o ambiente; não instala programas nem expõe senhas. Se não houver SDK .NET 10 ou Docker, instale pelas páginas oficiais abaixo e execute novamente. O Docker será usado para executar SQL Server e a API em contêineres quando o ambiente completo estiver configurado.

## Repositório GitHub

Depois de confirmar que esta é a única raiz do projeto, crie **um repositório chamado `oficina-mecanica-fase-1`** na conta GitHub da equipe e associe-o a esta pasta. Ele pode ser público. Se esta pasta ainda não tiver `.git`, os comandos básicos são:

```powershell
git init
git branch -M main
git add .
git commit -m "Configurar repositório da oficina mecânica"
git remote add origin https://github.com/SEU_USUARIO/oficina-mecanica-fase-1.git
git push -u origin main
```

Se a pasta **já tiver** `.git`, não rode `git init` nem sobrescreva o `origin`: confira `git status` e `git remote -v` antes de integrar os arquivos. Não coloque credenciais nem senhas no Git.

Substitua `SEU_USUARIO` pelo usuário ou organização proprietária. Se o repositório for público, confira que outra pessoa consegue abrir e clonar a URL sem autenticação. Se for privado, convide `soat-architecture` e confira o acesso após o aceite. Registre a URL efetiva do repositório.

## Referências

- PDF oficial **SOAT — Fase 1 — Tech Challenge**: objetivos e requisitos técnicos da aplicação.
- [Instalar .NET no Windows](https://learn.microsoft.com/pt-br/dotnet/core/install/windows).
- [SQL Server 2022 em contêiner](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver16).
- [Convidar colaboradores em repositório pessoal](https://docs.github.com/pt/repositories/managing-your-repositorys-settings-and-features/repository-access-and-collaboration/inviting-collaborators-to-a-personal-repository).

