# Testar notificações de status da OS — item 8 da Fase 2

O objetivo é conferir que cada mudança real de status cria um aviso, que falhas de SMTP não desfazem a OS e que o envio pode ser retomado. Os testes automatizados verificam as falhas de transação e a disputa entre processadores em banco descartável. Não provoque falhas manualmente no banco da aplicação para repetir esses cenários.

## 1. Preparar o ambiente

Abra Docker Desktop com contêineres Linux e aguarde o mecanismo iniciar. Encerre uma API aberta pelo Visual Studio se ela estiver usando a mesma porta do Compose. Use PowerShell na pasta do projeto:

```powershell
Set-Location C:\Users\Carol\Desktop\OficinaMecanicaFase1
docker version
```

O comando deve mostrar a seção do servidor Docker. Use o `.env` já configurado; não o sobrescreva. Se for um clone novo, siga o README para copiá-lo de `.env.example` e executar `scripts\Configurar-Acesso-Interno.ps1`. Não envie senhas ou tokens a outras pessoas.

## 2. Executar testes automatizados

Na raiz do projeto:

```powershell
dotnet test .\OficinaMecanicaFase1.slnx -c Release --logger "console;verbosity=normal"
```

Resultado esperado: todos aprovados, nenhuma falha e nenhum ignorado. Os testes SQL/Mailpit criam os próprios contêineres e bancos descartáveis; não usam o volume local da aplicação.

Para executar somente os testes diretamente ligados a esta correção:

```powershell
dotnet test .\OficinaMecanicaFase1.slnx -c Release --filter "FullyQualifiedName~OutboxSqlServerTests|FullyQualifiedName~ProcessadorNotificacoesStatusTests|FullyQualifiedName~NotificadorStatusSmtpTests"
```

O que eles demonstram:

| Cenário automatizado | Resultado esperado |
| --- | --- |
| Falha ao gravar aviso depois de salvar criação/mudança de status | Transação revertida; OS e histórico não ficam parcialmente alterados. |
| Falha transitória na mesma situação | Outra tentativa com contexto novo grava OS e um aviso. |
| Commit realizado, mas confirmação perdida | Marco persistido confirma sucesso; não cria outra OS. |
| Duas tentativas disputando o mesmo aviso | Somente uma reserva vence. |
| Reserva expira e outra tentativa assume | A antiga não consegue concluir nem reagendar o aviso da nova. |
| Dois processadores com um aviso reservado | Um envio enquanto a reserva está válida. |
| Envio SMTP falha | Erro/contador/próxima tentativa persistidos; OS permanece correta. |
| Nova tentativa depois da recuperação | Aviso passa para Enviada; próxima consulta não o reenvia. |
| Lote demorado | Cada aviso recebe prazo novo quando é assumido. |
| Prazo da reserva acaba | Não inicia envio vencido; cancela envio que aguarda além do prazo e reagenda. |

## 3. Subir API, banco e caixa de e-mail de teste

```powershell
docker compose -f .\docker-compose.yml -f .\docker-compose.mailpit.yml up -d --build --wait
docker compose -f .\docker-compose.yml -f .\docker-compose.mailpit.yml ps
```

A inicialização padrão aplica a migration nova, inclusive `ProcessamentoId`. Não é necessário excluir banco ou volume. Se o ambiente usa `Banco__Inicializacao=Externa`, aplique a migration no passo próprio de preparação antes de iniciar a API; no Kubernetes, isso corresponde ao Job.

Abra:

- Swagger: `http://localhost:5080/swagger`.
- Mailpit: `http://localhost:8025`.

Use as portas do seu `.env` se as tiver alterado. Se houver conflito com outro clone, defina portas livres para `API_PORT`, `SQLSERVER_PORT`, `MAILPIT_WEB_PORT` e `MAILPIT_SMTP_PORT` e use outro projeto Compose com `-p`; repita esse mesmo `-p` nos comandos seguintes. Esta configuração captura e-mails no Mailpit, sem enviá-los a pessoas reais.

Para conferir os erros de configuração:

```powershell
docker compose -f .\docker-compose.yml -f .\docker-compose.mailpit.yml logs --tail 80 api
```

## 4. Autenticar e preparar cadastros pelo Swagger

Em `POST /api/interno/autenticacao`, entre com o administrador local. Copie o token e use **Authorize** no Swagger. Se o campo pedir Bearer, siga o formato exibido pela interface. Tokens expiram; refaça o login se receber 401.

Com administrador, crie ou reutilize os cadastros:

1. Cliente em `POST /api/clientes`: `{"nome":"Cliente de teste","documento":"52998224725","email":"cliente@exemplo.com"}`.
2. Veículo em `POST /api/veiculos`, usando o ID desse cliente: `{"clienteId":"ID_CLIENTE","placa":"ABC1234","marca":"Fiat","modelo":"Uno","ano":2020}`.
3. Serviço em `POST /api/servicos`: `{"nome":"Revisão de teste","descricao":null,"precoBase":100}`.

Guarde os IDs retornados. Se documento/placa já existir, reutilize o cadastro em vez de tentar duplicá-lo; confira se o cliente tem e-mail.

Crie ou reutilize uma conta `Mecânico` e uma `Atendente`, em `POST /api/interno/usuarios`, como administrador. Use e-mails de teste e senhas próprias que atendam às regras. O campo `papel` aceita esses nomes exatos. O administrador não possui automaticamente as permissões exclusivas desses papéis; autentique com a conta correspondente e substitua o token em Authorize ao mudar de tarefa.

## 5. Conferir os seis status e as ações sem aviso

Para simplificar o teste, abra uma OS com **um serviço e sem itens de estoque**; assim não precisa preparar consumo de peças.

Como Administrador ou Atendente, use `POST /api/ordens-servico`:

```json
{
  "clienteId": "ID_CLIENTE",
  "veiculoId": "ID_VEICULO",
  "servicosIds": ["ID_SERVICO"],
  "itensNecessarios": []
}
```

Espere `201 Created`, anote `numero` e confira `Recebida`. No Mailpit deve chegar um aviso com esse número em alguns segundos. Cada requisição abaixo usa o número anotado e o token do papel indicado:

| Ação | Papel | Status/aviso esperado |
| --- | --- | --- |
| `POST /api/ordens-servico/{numero}/diagnostico/inicio` | Mecânico | EmDiagnostico; um novo e-mail. |
| `POST /api/ordens-servico/{numero}/diagnostico`, corpo `{"descricao":"Revisão registrada para teste"}` | Mecânico | Continua EmDiagnostico; nenhum e-mail extra. |
| `POST /api/ordens-servico/{numero}/orcamento` | Mecânico | AguardandoAprovacao; um novo e-mail. |
| `POST /api/ordens-servico/{numero}/codigo-acesso/emissao`, corpo `{"motivo":null}` na primeira emissão | Atendente | Continua AguardandoAprovacao; retorna código, sem e-mail extra de status. |
| `POST /api/cliente/ordens-servico/{numero}/orcamento/decisao`, corpo com documento, código recebido, `aprovar:true`, `motivo:null` | Cliente; rota pública | Aprovação registrada; continua AguardandoAprovacao; sem e-mail extra de status. |
| `POST /api/ordens-servico/{numero}/execucao/inicio` | Mecânico | EmExecucao; um novo e-mail. |
| `POST /api/ordens-servico/{numero}/servicos/{servicoId}/inicio` | Mecânico | Continua EmExecucao; nenhum e-mail extra. |
| `POST /api/ordens-servico/{numero}/servicos/{servicoId}/conclusao` | Mecânico | Finalizada ao concluir o único serviço; um novo e-mail. |
| `POST /api/ordens-servico/{numero}/entrega` | Atendente | Entregue; um novo e-mail. |

O caminho pelo formulário n8n continua disponível: acrescente `-f .\docker-compose.n8n.yml` ao comando de subida, importe/publique o workflow conforme README e faça a aprovação por ele, em vez de chamar diretamente a rota pública.

Consulte `GET /api/ordens-servico/{numero}` com token interno para conferir status e histórico. Ao terminar, devem existir seis avisos de status dessa OS. Assunto/corpo precisam identificar a OS, explicar o status e não expor documento/código/senha.

Repita a entrega já concluída: espere 400 e nenhum novo aviso. Repita uma decisão de orçamento já registrada: espere 409 e nenhum aviso de mudança de status. Essas respostas são esperadas.

## 6. Conferir cliente sem e-mail

Cadastre outro cliente com e-mail omitido ou `null`, por exemplo documento de teste `11144477735`, e outro veículo, por exemplo placa `DEF5678`. Abra uma OS com o serviço de teste.

Espere criação normal e status Recebida, mas nenhum aviso dessa OS no Mailpit nem na tabela de notificações. O comportamento definido é não enviar quando não há endereço.

## 7. Simular indisponibilidade do SMTP e recuperação

Use **uma nova OS**, ou uma OS que ainda permita iniciar o diagnóstico. Pare somente o Mailpit:

```powershell
docker compose -f .\docker-compose.yml -f .\docker-compose.mailpit.yml stop mailpit
```

Crie a nova OS ou inicie o diagnóstico com o papel adequado. Confira:

- A API conclui a operação e retorna o status correto, mesmo com o SMTP indisponível.
- O histórico está atualizado.
- O log registra falha no envio; a notificação fica pendente para nova tentativa.

Consulte o log usando o comando da etapa 3. Para comprovar o registro no SQL, use SSMS ou outro cliente com sua conexão local e execute apenas esta consulta (o número da OS identifica o caso que está testando):

```sql
SELECT NumeroOrdemServico, Status, Situacao, Tentativas,
       ProximaTentativaEmUtc, ProcessamentoId, ProcessandoAteUtc,
       EnviadaEmUtc, UltimoErro
FROM dbo.NotificacoesStatusOrdensServico
ORDER BY CriadaEmUtc DESC;
```

`Situacao`: 1 = Pendente, 2 = EmProcessamento, 3 = Enviada. Durante a tentativa pode aparecer 2; depois da falha deve voltar para 1, com `Tentativas` incrementado e próxima tentativa agendada. Essas tabelas são internas; não foi criada uma rota pública para expor os registros de envio.

Religue o Mailpit:

```powershell
docker compose -f .\docker-compose.yml -f .\docker-compose.mailpit.yml start mailpit
```

Aguarde a próxima tentativa (por padrão, 30 segundos mais o intervalo de consulta). Confira o e-mail e a mesma linha no SQL: `Situacao = 3`, data de envio preenchida e erro limpo. O contador preserva as falhas anteriores. Observe por mais uma rodada de consulta para confirmar que o aviso enviado não é selecionado novamente.

## 8. Conferir concorrência e rollback sem alterar o banco da aplicação

Use os testes automatizados da etapa 2. Eles exercitam, em SQL Server isolado, falha depois de salvar a OS, commit com confirmação perdida, duas reservas simultâneas e tentativa antiga após reassunção. Não é necessário escalar manualmente o Compose nem derrubar o SQL da aplicação para provocar esses casos.

Antes de validar no Kubernetes, integre os trabalhos, use a mesma versão atualizada em todas as réplicas e execute a migration nova no Job. O teste local não substitui o teste da imagem/configuração do cluster final. SMTP continua sem garantia de entrega exatamente uma vez em todo cenário de falha de rede.

## 9. Encerrar a validação

Conferir: suíte aprovada; seis avisos; ações sem mudança e requisições repetidas sem aviso extra; cliente sem e-mail; falha SMTP com status preservado e reenvio após recuperação. Registre o resultado na análise da Fase 2.

Para apenas parar os contêineres de teste:

```powershell
docker compose -f .\docker-compose.yml -f .\docker-compose.mailpit.yml stop
```

Não use `down -v` para este teste: ele remove os volumes. A gravação do vídeo continua sendo a etapa final da fase, depois da integração das branches, infraestrutura e CI/CD.
