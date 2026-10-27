# Auditoria final da Fase 1 — Oficina Mecânica

Data da auditoria: 27/10/2026. Fonte normativa: SOAT - Fase 1 - Tech Challenge.pdf, páginas 2–5. Projeto analisado: OficinaMecanicaFase1. Remoto: https://github.com/CarolsAragao/OficinaMecanicaFase1.git.

## Critério e percentuais

A matriz separa evidência de implementação de prova de funcionamento. Uma linha só conta no numerador se estiver marcada como cumprida; parcial e não verificável contam zero. Para funcionamento, foi exigido teste executado com sucesso da interface ou regra inteira, ou verificação direta de artefato estrutural/de entrega. Inspeção de rota sem execução não prova a API. O denominador são os 46 requisitos aplicáveis: dos 47 itens atômicos originalmente identificados, a exigência de repositório privado foi dispensada pela orientação posterior do professor informada pela equipe. A opção de arquitetura em camadas foi tratada como escolha permitida, não como obrigação adicional.

- Implementados: **43/46 = 93,48%**.
- Comprovadamente funcionais: **13/46 = 28,26%**.

## Matriz de requisitos

| ID | Requisito | Evidência concreta | Teste/procedimento executado | Implementação | Funcionamento | Correção necessária |
| --- | --- | --- | --- | --- | --- | --- |
| F01 | Identificar cliente por CPF/CNPJ | RotasCadastros GET /api/clientes?documento; DocumentoCliente.Normalizar | Inspeção; testes unitários de CPF/CNPJ | cumprido | parcial | Executar consulta HTTP em SQL descartável |
| F02 | Cadastrar veículo com placa, marca, modelo e ano | Veiculo.cs; RotasCadastros POST/PUT /api/veiculos | Inspeção; teste de domínio de proprietário/placa | cumprido | parcial | Executar cadastro HTTP e vínculo |
| F03 | Incluir serviços solicitados na OS | OrdemServico.Criar/AdicionarServicoSolicitado; POST /api/ordens-servico | Testes de domínio de criação e ampliação | cumprido | parcial | Executar fluxo HTTP |
| F04 | Incluir peças e insumos necessários na OS | ItemNecessario; AdicionarItemNecessario; rotas de OS | Testes de quantidade e inclusão no domínio | cumprido | parcial | Executar fluxo HTTP |
| F05 | Gerar orçamento automaticamente pelos serviços e itens | Orcamento.Gerar; rota POST /api/ordens-servico/{numero}/orcamento | Teste de total e arredondamento no domínio | cumprido | parcial | Executar geração HTTP com persistência |
| F06 | Disponibilizar orçamento ao cliente | RotasClienteOrcamento; consulta pública por OS e documento | Inspeção de rota e contrato; integração bloqueada | cumprido | não verificável | Testar acesso de cliente à proposta |
| F07 | Aprovar ou rejeitar orçamento | Orcamento.Aprovar/Rejeitar; RotasClienteOrcamento | Testes unitários de decisão e rejeição | cumprido | parcial | Executar decisão HTTP e efeitos na OS |
| F08 | Autorizar reparos adicionais | Aditivo.cs; RotasAditivos; reserva adicional | Teste unitário de rejeição, aprovação e reserva | cumprido | parcial | Executar fluxo HTTP completo do aditivo |
| F09 | Representar os seis status oficiais | StatusOrdemServico.cs; OrdemServico.cs | Testes de transição até Entregue; enum inspecionado | cumprido | cumprido | Nenhuma |
| F10 | Alterar status conforme as ações | Métodos de diagnóstico, orçamento, execução, finalização e entrega em OrdemServico.cs | Transições de domínio testadas; HTTP bloqueado | cumprido | parcial | Executar ações via API |
| F11 | Permitir acompanhamento da OS pelo cliente | POST /api/cliente/ordens-servico/{numero}/andamento/consulta | Inspeção de rota; integração bloqueada | cumprido | não verificável | Testar consulta e não vazamento de dados |
| F12 | CRUD de clientes | RotasCadastros.cs; GerenciadorCadastros.cs | Inspeção; integração bloqueada | cumprido | não verificável | Executar criar, ler, atualizar, excluir |
| F13 | CRUD de veículos | RotasCadastros.cs; Veiculo.cs | Inspeção; integração bloqueada | cumprido | não verificável | Executar CRUD e restrições de vínculo |
| F14 | CRUD de serviços | RotasCatalogoEstoque.cs; ServicoCatalogo.cs | Inspeção e teste de regra; integração bloqueada | cumprido | não verificável | Executar CRUD HTTP |
| F15 | CRUD de peças e insumos | RotasCatalogoEstoque.cs; ItemEstoque.cs | Inspeção e testes de regra; integração bloqueada | cumprido | não verificável | Executar CRUD HTTP |
| F16 | Controlar estoque | ItemEstoque.Reservar/LiberarReserva/RegistrarConsumoReservado; RepositorioReservas.cs | Testes unitários de saldo e consumo; integração bloqueada | cumprido | parcial | Testar concorrência e persistência |
| F17 | Listar OS na administração | GET /api/administracao/ordens-servico em RotasGestaoOrdensServico.cs | Inspeção; integração bloqueada | cumprido | não verificável | Executar filtro e paginação HTTP |
| F18 | Detalhar OS na administração | GET /api/administracao/ordens-servico/{numero} | Inspeção; integração bloqueada | cumprido | não verificável | Executar detalhe HTTP |
| F19 | Monitorar tempo médio de execução | ConsultaGestaoOrdensServico.cs; rota indicadores/tempo-medio | IndicadorTempoMedioTests passou; HTTP bloqueado | cumprido | parcial | Validar resposta HTTP e dados persistidos |
| S01 | JWT nas APIs administrativas | Program.cs AddJwtBearer e políticas; EmissorJwt.cs | Inspeção; teste HTTP bloqueado | cumprido | não verificável | Testar 401, 403, expiração e papéis |
| S02 | Validar CPF/CNPJ | DocumentoCliente.Normalizar | Teorias de documentos válidos/inválidos passaram | cumprido | cumprido | Nenhuma |
| S03 | Validar placa | PlacaVeiculo.Normalizar | Teorias de placas válidas/inválidas passaram | cumprido | cumprido | Nenhuma |
| Q01 | Testes unitários dos fluxos principais | InvariantesDominioTests, RegrasDominioTests e demais testes de domínio | 31 testes sem Docker passaram | cumprido | cumprido | Nenhuma para o escopo unitário |
| Q02 | Testes de integração dos fluxos principais | 11 testes HTTP/SQL com WebApplicationFactory e Testcontainers | 11 falharam antes do teste por DockerUnavailableException | cumprido | não verificável | Executar suíte completa com Docker Linux |
| Q03 | Cobertura mínima de 80% nos domínios críticos | coverage.runsettings; docs/cobertura-auditoria.cobertura.xml | 459/498 linhas e 388/458 ramos medidos | cumprido | cumprido | Repetir com suíte completa quando Docker funcionar |
| T01 | Back-end monolítico | OficinaMecanicaFase1.slnx; uma API e quatro projetos em camadas | Inspeção de referências e build Release | cumprido | cumprido | Nenhuma |
| T02 | Justificar banco escolhido | README: SQL Server, relações, restrições e transações | Justificativa inspecionada | cumprido | cumprido | Nenhuma |
| T03 | APIs RESTful | RotasCadastros, RotasCatalogoEstoque e RotasOrdensServico | Rotas e verbos inspecionados; HTTP bloqueado | cumprido | não verificável | Executar contratos HTTP |
| T04 | Documentar APIs via Swagger/OpenAPI | Program.cs AddOpenApi, MapOpenApi, UseSwaggerUI | Código inspecionado; servidor não iniciado | cumprido | não verificável | Abrir /swagger e /openapi/v1.json em ambiente limpo |
| T05 | Dockerfile de build da aplicação | Dockerfile multiestágio .NET 10 com APP_UID | Arquivo inspecionado; build Docker bloqueado | cumprido | não verificável | Executar docker build |
| T06 | docker-compose.yml para ambiente completo | docker-compose.yml com API, SQL Server, healthcheck e volume | docker compose config --quiet passou; up bloqueado | cumprido | parcial | Subir projeto isolado e testar endpoints |
| T07 | README para execução local simples | README; .env.example; scripts; migrations/bootstrap | Exportação limpa: restore/build e 31 testes passaram; runtime bloqueado | cumprido | parcial | Validar up e login de administrador em clone |
| D01 | Documentação DDD em Miro ou equivalente | docs/index.html e nove HTMLs; cinco Excalidraw originais | Navegação local testada; Pages publicou índice e 14 links DDD com HTTP 200 | cumprido | parcial | Enviar documentação revisada ao remoto e retestar Pages |
| D02 | Event Storming completo da OS | docs/ddd/04-event-storming-os.html e diagrama editável | HTML/SVG local inspecionado; URL pública respondeu HTTP 200 | cumprido | parcial | Enviar versão revisada ao remoto e inspecionar página publicada |
| D03 | Event Storming completo de peças e insumos | docs/ddd/05-event-storming-estoque.html e diagrama editável | HTML/SVG local inspecionado; URL pública respondeu HTTP 200 | cumprido | parcial | Enviar versão revisada ao remoto e inspecionar página publicada |
| D04 | Diagramas pertinentes da disciplina DDD | Domain Storytelling, modelo tático e Context Map em docs/ddd | Modelo e mapa locais abertos; todos os links DDD publicados responderam HTTP 200 | cumprido | parcial | Enviar versão revisada e inspecionar diagramas publicados |
| D05 | Linguagem ubíqua aplicada | docs/ddd/09-linguagem-ubiqua-revisao.html | Glosário local inspecionado; URL pública respondeu HTTP 200 | cumprido | parcial | Enviar versão revisada; revisar após integração |
| E01 | Repositório privado | Orientação posterior do professor informada pela equipe permite repositório público | GET anônimo respondeu HTTP 200 | dispensado | dispensado | Registrar a orientação posterior junto à entrega, se houver mensagem escrita |
| E02 | Acesso de leitura de soat-architecture ao repositório | Repositório público abre sem autenticação | GET anônimo respondeu HTTP 200 em 27/10/2026 | cumprido | cumprido | Reconfirmar acesso público na entrega |
| E03 | Vídeo de até 15 minutos | docs/roteiro-video.md; vídeo ausente | Nenhum vídeo ou duração verificados | ausente | ausente | Gravar, disponibilizar e verificar duração |
| E04 | Código-fonte no repositório de entrega | Remoto origin e arquivos rastreados | Repositório público abre; commit local desta auditoria; push falhou antes da autenticação | parcial | parcial | Enviar commit auditado ao remoto e confirmar SHA publicado |
| E05 | Relatório com análise do scan de código | docs/relatorio-vulnerabilidades.md | NuGet atualizado; Semgrep inconclusivo e Docker Scout não executado | parcial | parcial | Reexecutar scanner de código/imagem e analisar achados |
| E06 | PDF informa nome do grupo | docs/entrega-fase-1.pdf | Extração e renderização visual: nome correto | cumprido | cumprido | Nenhuma |
| E07 | PDF informa participantes e Discord | docs/entrega-fase-1.pdf | Extração e renderização visual: nomes, RMs e Discord corretos | cumprido | cumprido | Nenhuma |
| E08 | PDF contém link da documentação | docs/entrega-fase-1.pdf; URL do GitHub Pages | Índice e 14 links DDD responderam HTTP 200; PDF regenerado e link clicável | cumprido | cumprido | Revalidar após envio das alterações ao remoto |
| E09 | PDF contém link do repositório | docs/entrega-fase-1.pdf | Link clicável extráido e GET anônimo HTTP 200 | cumprido | cumprido | Reconfirmar acesso público ao repositório |
| E10 | PDF contém análise de vulnerabilidades | docs/entrega-fase-1.pdf | Texto extraído e página renderizada, com limitações explícitas | cumprido | cumprido | Atualizar após scans pendentes |

## Testes, cobertura e execução limpa

- Build Release no projeto original: aprovado, sem avisos. Suíte completa: **42 testes, 31 passaram e 11 falharam**; as 11 falhas foram DockerUnavailableException antes de iniciar SQL Server. Não houve falha de asserção de negócio nos testes que puderam executar.
- Subconjunto sem Docker: **31/31 aprovados**. Coverlet mediu somente OficinaMecanicaFase1.Domain: **459/498 linhas (92,16%)** e **388/458 ramos (84,71%)**. Por área: Cadastros 56/58 (96,55%); Catálogo 16/17 (94,12%); Estoque 99/108 (91,67%); Orçamentos 73/82 (89,02%); Ordens de serviço 215/233 (92,27%). O XML auditado está em docs/cobertura-auditoria.cobertura.xml.
- A exportação limpa de 146 arquivos, sem .env, User Secrets, bin/obj nem TestResults, concluiu restore, build e 31 testes de domínio. O Dockerfile e a inicialização via Compose não foram executados porque o mecanismo Docker Linux permaneceu inacessível. docker compose config --quiet passou com variáveis temporárias.
- Os testes de integração instanciam MsSqlBuilder/Testcontainers por teste, com banco de nome único e descarte por await using. Não usam SQL Server local, porta fixa, User Secrets, dados existentes nem caminhos da usuária. Essa independência foi inspecionada no código; sua execução limpa segue não verificável.

## Documentação DDD

Os cinco diagramas Excalidraw do projeto são idênticos por SHA-256 aos originais. O índice e os nove HTMLs foram conferidos localmente: links relativos existentes, dois Event Stormings com SVG legível, modelo tático, Context Map e linguagem ubíqua. O GitHub Pages do projeto foi habilitado com fonte `main` e pasta `/docs`. Em 27/10/2026, o índice abriu no navegador e seus 14 links DDD responderam HTTP 200, incluindo ambos os Event Stormings. A URL definitiva é `https://carolsaragao.github.io/OficinaMecanicaFase1/`. A versão revisada dos documentos continua local até o envio dos commits; após o push, é necessário conferir a nova implantação como visitante.

| Documento | Conteúdo e propósito |
| --- | --- |
| 01 Escopo e rastreabilidade | Liga requisitos, decisões DDD e componentes da API. |
| 02 Decisões aprovadas | Registra D-00 a D-16 e resolve ambiguidades do fluxo. |
| 03 Domain Storytelling | Descreve jornadas, atores e alternativas do atendimento. |
| 04 Event Storming da OS | Mostra comandos, eventos, políticas, status e ramos de rejeição/aditivos. |
| 05 Event Storming de estoque | Mostra catálogo, reserva, recusa, consumo, liberação e reposição. |
| 06 Catálogos, regras e atores | Consolida operações, responsáveis e regras transversais. |
| 07 Modelo tático DDD | Propõe agregados, entidades, objetos de valor e invariantes. |
| 08 Context Map | Explica limites conceituais e relações entre cadastros, OS, orçamento e estoque. |
| 09 Linguagem ubíqua | Define termos por contexto e corrige sinônimos ambíguos. |
| Cinco Excalidraw | Permitem edição das histórias, dos dois Event Stormings, do modelo tático e do mapa. |

## PDF de entrega

Caminho no projeto: docs/entrega-fase-1.pdf. **Uma página A4**, renderizada e conferida visualmente. Contém grupo, dois participantes com RMs/Discord, link do repositório, link público do GitHub Pages para a documentação, análise atualizada de vulnerabilidades, resultados dos testes e pendências. O link do repositório respondeu HTTP 200 sem login; o índice e 14 links DDD responderam HTTP 200 sem aviso intermediário. O PDF ainda exige atualização após os testes e scans pendentes e confirmação do envio ao remoto.

## Vulnerabilidades, segredos e histórico

Em 27/10/2026, dotnet list da solução com --vulnerable --include-transitive --no-restore --format json terminou com código 0 e sem pacotes vulneráveis nos cinco projetos. Semgrep 1.182.1 foi instalado isoladamente e o scan foi tentado, mas o semgrep-core falhou em socketpair; não há resultado de achados. Docker Scout não foi executado porque o terminal recebeu acesso negado ao mecanismo Docker, embora o aplicativo mostrasse Engine running. dotnet format analyzers falhou por acesso negado ao named pipe. A busca heurística em arquivos rastreados e 439 objetos históricos não encontrou arquivos .env, chaves privadas ou User Secrets; achou placeholders antigos e caminhos locais, removidos da versão de trabalho. Nenhum valor sensível foi reproduzido neste relatório.

## Alterações realizadas

- Acrescentados dez testes úteis de domínio e marcados onze testes dependentes de Docker como Integration; cobertura e relatório versionados.
- Compose renomeado para docker-compose.yml com API_PORT e SQLSERVER_PORT configuráveis; .env.example e script de depuração ajustados; README esclarece Linux/macOS e a orientação posterior que permite repositório público.
- Índice e documentação DDD revisados, com nomes de domínio, diagramas ampliados, referências temporárias e marcadores de data removidos; escopo reescrito para refletir a API.
- Relatório de vulnerabilidades e PDF atualizados para distinguir scans executados de verificações bloqueadas.

## Pendências para entrega

1. GitHub Pages está ativo em `main/docs` e o PDF já contém sua URL. Enviar a documentação revisada ao remoto e retestar o índice, os dois Event Stormings, demais diagramas e linguagem como visitante.
2. Com Docker Linux acessível, executar em clone limpo docker build, docker compose up, migrations/bootstrap, saúde, Swagger, login e fluxos HTTP; rodar os 11 testes de integração e repetir a cobertura. Não tocar em volumes existentes.
3. Reexecutar Semgrep e scan de CVEs da imagem, analisar achados e atualizar relatório/PDF. Repetir dotnet format analyzers em ambiente que permita seu processo auxiliar.
4. Enviar o commit local desta auditoria ao remoto e verificar o SHA publicado. O push pelo Git CLI falhou porque o Git Credential Manager não iniciou no ambiente; o Visual Studio estava minimizado e indisponível ao controle de interface. Após o push, reconfirmar que o repositório público e o GitHub Pages abrem sem login.
5. Gravar e disponibilizar o vídeo de até 15 minutos, com duração e acesso verificados.

**Conclusão:** ainda há pendências além do vídeo; a entrega não está pronta.
