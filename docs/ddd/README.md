# Documentação DDD — Oficina Mecânica

Estes são os artefatos de modelagem que orientaram a implementação da
Fase 1. O índice navegável fica em `docs/index.html`. Os nove HTMLs
contêm diagramas SVG legíveis no navegador e os cinco arquivos `.excalidraw`
permitem edição. A hospedagem pública é separada do repositório de código
para que este possa atender ao requisito de acesso privado.

| Ordem | Documento | Conteúdo |
| --- | --- | --- |
| 01 | [Escopo e rastreabilidade](01-escopo-rastreabilidade.html) | Requisitos do Tech Challenge e cobertura documental. |
| 02 | [Decisões aprovadas](02-decisoes-aprovadas.html) | Políticas e regras D-00 a D-16. |
| 03 | [Domain Storytelling](03-domain-storytelling.html) | Jornadas e variações do domínio. |
| 04 | [Event Storming da OS](04-event-storming-os.html) | Eventos, comandos, políticas e fluxos da ordem de serviço. |
| 05 | [Event Storming de estoque](05-event-storming-estoque.html) | Peças, insumos, disponibilidade, reserva e consumo. |
| 06 | [Catálogos, regras e atores](06-catalogos-regras-atores.html) | Vocabulário e rastreabilidade entre fluxos. |
| 07 | [Modelo tático DDD](07-modelo-tatico-ddd.html) | Agregados, entidades e responsabilidades. |
| 08 | [Mapa de contextos](08-context-map.html) | Relações entre os limites do domínio. |
| 09 | [Linguagem ubíqua](09-linguagem-ubiqua-revisao.html) | Termos oficiais e equivalências. |

Na API, autenticação e papéis usam ASP.NET Core Identity com JWT; o mapa de contextos trata esse acesso como preocupação transversal, separada da identificação cadastral do cliente. O evento de estoque mínimo é opcional na decisão D-13 e não integra o MVP implementado.

Os cinco [diagramas editáveis](diagramas) acompanham os documentos. O [relatório de vulnerabilidades](../relatorio-vulnerabilidades.md) e o [roteiro do vídeo](../roteiro-video.md) estão na pasta `docs`.
