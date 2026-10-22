# Roteiro para o vídeo de demonstração — Fase 1

**Grupo:** Torque & Domínio — Tech Challenge  
**Duração-alvo:** 12 a 14 minutos (limite da entrega: 15 minutos)  
**Ambiente:** API e SQL Server locais, Swagger em `http://localhost:5080/swagger`.

Este é um guia de gravação, não um vídeo já entregue. Grave em uma base de demonstração com dados fictícios. Antes de iniciar, deixe a aplicação funcionando, prepare contas de **Administrador**, **Atendente**, **Mecânico** e **Estoquista** e separe em um bloco local os IDs do cliente, veículo, serviço e item, além do número da OS. Nunca mostre senhas, a chave JWT, a connection string ou o conteúdo do `.env` na gravação; o token pode ser inserido no Swagger com o campo fora da área capturada ou editado na pós-produção.

| Tempo | Tela e ação | Explicação sugerida |
| --- | --- | --- |
| 0:00–0:50 | Mostrar o repositório e a página da documentação. Abrir os dois Event Stormings, o modelo tático, mapa de contextos e glossário. | "A implementação parte das decisões DDD documentadas. Os dois fluxos principais são ordem de serviço e estoque de peças e insumos." |
| 0:50–1:40 | Abrir `/api/saude` e `/api/saude/banco`. No Swagger, localizar autenticação, cadastros, catálogo, OS e gestão. | "A API é um monólito .NET com limites de domínio explícitos e persistência em SQL Server. Estes endpoints comprovam disponibilidade da API e do banco." |
| 1:40–2:30 | Fazer login em `POST /api/interno/autenticacao` com uma conta de demonstração e autorizar o Swagger. Mostrar `GET /api/interno/identidade` sem expor o token. | "As rotas internas usam JWT e papéis; as ações sensíveis exigem o papel correspondente." |
| 2:30–3:50 | Com o perfil adequado, criar ou consultar cliente em `/api/clientes`, veículo em `/api/veiculos`, serviço em `/api/servicos` e item em `/api/itens-estoque`. Registrar uma entrada em `/api/itens-estoque/{id}/entradas`. | "O cadastro relaciona veículo ao cliente e o catálogo mantém serviços e itens. A entrada é registrada separadamente do consumo." |
| 3:50–4:50 | Abrir `POST /api/ordens-servico` com cliente, veículo, serviço e item. Consultar `GET /api/ordens-servico/{numero}`. | "A OS nasce Recebida. A disponibilidade é consultada na abertura, mas o estoque ainda não é reservado nem baixado." |
| 4:50–6:00 | Entrar como Mecânico. Executar `diagnostico/inicio`, `diagnostico` e `orcamento`; consultar o orçamento. | "O diagnóstico antecede o cálculo do orçamento. Serviços e itens formam a proposta para o cliente." |
| 6:00–7:20 | Entrar como Atendente e emitir código em `codigo-acesso/emissao`. Usar as rotas públicas `POST /api/cliente/ordens-servico/{numero}/orcamento/consulta` e `/decisao` para aprovação. Ocultar o código na edição. | "A decisão usa documento e código individual; a rota do cliente não exige uma conta interna. A aprovação leva à tentativa de reserva." |
| 7:20–8:30 | Mostrar a reserva confirmada. Se houver saldo insuficiente, mostrar aviso de estoque, registrar entrada como Estoquista e executar `reservas/tentativa`. | "A reserva integral impede iniciar o reparo sem disponibilidade. Após reposição, o estoquista pode tentar novamente." |
| 8:30–10:00 | Entrar como Mecânico. Executar `execucao/inicio`, iniciar um serviço, registrar consumo real em `consumos` e concluir o serviço. | "O consumo baixa estoque e reserva; a conclusão do último serviço finaliza a OS e libera saldo reservado não utilizado." |
| 10:00–10:50 | Entrar como Atendente e registrar `entrega`. Consultar andamento pela rota pública. | "A entrega é um marco distinto da finalização. O cliente acompanha o status sem acessar APIs administrativas." |
| 10:50–12:10 | Abrir listagem/detalhamento em `/api/administracao/ordens-servico` e indicador `/indicadores/tempo-medio`. | "A gestão filtra as ordens e calcula o tempo médio conforme a definição documentada na resposta." |
| 12:10–13:20 | Se houver dados preparados, mostrar uma OS rejeitada ou um aditivo de reparo adicional, incluindo decisão do cliente. | "A rejeição encerra a proposta sem forçar um novo status oficial; reparos adicionais seguem aprovação própria." |
| 13:20–14:00 | Voltar à página da documentação e abrir o relatório de vulnerabilidades. | "O relatório registra os resultados e a limitação do scan da imagem Docker, que exigiu autenticação." |

## Preparação para uma gravação fluida

1. Rode o projeto conforme o [README](../README.md) e confirme `/api/saude/banco` antes de gravar.
2. Use dados fictícios e uma OS nova. Prepare uma segunda OS já rejeitada ou com aditivo se quiser demonstrar o ramo opcional sem ultrapassar 15 minutos.
3. Deixe os valores de entrada em um arquivo local fora do enquadramento. Use os exemplos de esquema do Swagger para montar os corpos das requisições, pois os IDs e os contratos dependem dos dados criados.
4. Capture a tela em 1080p com zoom de navegador suficiente para leitura. Faça cortes nos intervalos de troca de papel e autenticação.
5. Antes de compartilhar, revise o vídeo em busca de segredos, confirme duração máxima de 15 minutos e teste o link em uma janela anônima.

Ao publicar, acrescente o link final do vídeo ao PDF de entrega ou à página da documentação e confirme que o avaliador consegue abri-lo sem autenticação pessoal.
