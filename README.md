Core Financeiro — API + Worker (Event-Driven)
📌 Visão Geral

Este projeto implementa um core financeiro simplificado, orientado a eventos, com separação clara entre orquestração, regras de negócio e infraestrutura.

A solução foi desenhada com foco em boas práticas de mercado, incluindo:

ASP.NET Core moderno

Domain-Driven Design (DDD)

Processamento assíncrono via mensageria

Consistência eventual

Cache Redis

Controle de concorrência otimista

Observabilidade básica (health checks + correlation id)

Execução local via Docker Compose

🧱 Arquitetura
Componentes
Componente	Responsabilidade
WebApi	Expor endpoints REST, autenticação, criação de transações
WorkerTransacao	Processar transações pendentes
RabbitMQ	Transporte de eventos
SQL Server	Persistência relacional
Redis	Cache de leitura
Frontend (Angular)	Interface do usuário
Estilo Arquitetural

Arquitetura em camadas

Event-Driven Architecture

Consistência eventual

Sharding por cliente no processamento de eventos

🗂 Estrutura do Projeto
/WebApi
  ├── Controllers
  ├── Extensions
  ├── Middleware
  └── Program.cs

/WorkerTransacao
  └── Consumers

/Application
  ├── Services
  ├── Dtos
  └── Interfaces

/Domain
  ├── Entities
  ├── Enums
  ├── Events
  └── Services

/Infraestrutura
  ├── EntityFramework
  ├── Messaging
  └── Caching

/Frontend
/docker-compose.yml

🔐 Segurança
Autenticação

JWT Bearer Token

ASP.NET Identity

Claims relevantes:

clienteId → escopo do tenant

sub → email

Autorização

Endpoints sensíveis usam [Authorize]

Escopo por cliente garantido via IUserContext

🌐 API — Endpoints
Auth
Método	Endpoint	Descrição
POST	/Auth/Registrar	Registra usuário e cliente
POST	/Auth/Logar	Autentica e retorna JWT
Conta (JWT obrigatório)
Método	Endpoint
POST	/Conta/Registrar
GET	/Conta
GET	/Conta/{contaId}
GET	/Conta/contasParaTransferencia/{id}
Transação (JWT obrigatório)
Método	Endpoint
POST	/Transacao
GET	/Transacao/conta/{contaId}
GET	/Transacao/passiveisDeEstorno/conta/{contaId}
📄 Swagger

Disponível na raiz da aplicação

Autenticação via Bearer JWT

Fluxo:

Faça login

Copie o token

Clique em Authorize

Informe:

Bearer {seu_token}

Controle de Concorrência

RowVersion habilita concorrência otimista

EF Core gera:

UPDATE ... WHERE RowVersion = @OriginalRowVersion


Em caso de conflito:

DbUpdateConcurrencyException

Base pronta para:

retry

serialização por cliente

escalabilidade segura

🔄 Fluxo de Transação
1️⃣ Criação (WebApi)

Valida request

Garante conta pertence ao cliente logado

Cria Transacao com status PENDENTE

Persiste no banco

Publica TransacaoCriadaEvent

Retorna resposta imediatamente

➡️ Baixa latência no request

2️⃣ Processamento (Worker)

Consome evento RabbitMQ

Carrega transação pendente (AsTracking)

Executa regras no ProcessadorTransacaoDomainService

Atualiza saldos e status

Persiste alterações

Invalida cache Redis (origem e destino)

🧠 Regras de Negócio (Domain Service)

Implementadas em ProcessadorTransacaoDomainService:

Crédito

Débito (respeita limite)

Reserva

Captura

Transferência

Estorno (compensação)

Características:

Domínio sem dependência de infraestrutura

Regras explícitas

Mensagens de erro controladas

Mutação de estado clara

🧊 Cache (Redis)
Estratégia

Cache-aside

Apenas leitura

TTL: 1 dia

Invalidação

Executada pelo worker apenas em transações SUCESSO

➡️ Garante consistência eventual

📬 Mensageria (RabbitMQ)

Exchange: transacoes.exchange

Routing key: transacoes.shard-{n}

Shard calculado por ClienteId

Benefícios

Paralelismo controlado

Redução de contenção

Escalabilidade horizontal

🩺 Health Checks
Liveness
GET /health/live

Readiness
GET /health/ready


Verifica:

Banco de dados

Mensageria

🧯 Tratamento de Erros
Validação

DTO inválido → 422

Retorno padronizado (ResultPattern)

Exceções

Middleware global

HTTP 500

Mensagem genérica ao cliente

Log detalhado internamente

🐳 Execução Local (Docker)
Subir ambiente
docker compose up --build

Serviços
Serviço	Porta
WebApi	8080 / 8081
Frontend	4200
RabbitMQ UI	15672
SQL Server	1433
Redis	6379
📈 Observabilidade
Implementado

CorrelationId (TraceIdentifier)

Logs no worker

Health checks

Evoluções naturais

OpenTelemetry

Métricas (Prometheus)

Tracing distribuído

DLQ + retries

🧠 Notas de Arquitetura (Senior Notes)

✔ Event-Driven
✔ Separação Application / Domain / Infra
✔ Cache consciente
✔ Concorrência otimista
✔ Worker dedicado
✔ Sharding por cliente
