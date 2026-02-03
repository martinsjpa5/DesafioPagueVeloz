# Core Financeiro — API + Worker (Event-Driven)

## 📌 Visão Geral

Este projeto implementa um **core financeiro simplificado**, orientado a eventos, com separação clara entre **orquestração**, **regras de negócio** e **infraestrutura**.

A solução foi desenhada com foco em **boas práticas de mercado**, incluindo:

- ASP.NET Core moderno
- Domain-Driven Design (DDD)
- Processamento assíncrono via mensageria
- Consistência eventual
- Cache Redis
- Controle de concorrência otimista
- Observabilidade básica (health checks + correlation id)
- Execução local via Docker Compose

---

## 🧱 Arquitetura

### Componentes

| Componente          | Responsabilidade |
|--------------------|------------------|
| **WebApi**          | Expor endpoints REST, autenticação, criação de transações |
| **WorkerTransacao** | Processar transações pendentes |
| **RabbitMQ**        | Transporte de eventos |
| **SQL Server**      | Persistência relacional |
| **Redis**           | Cache de leitura |
| **Frontend (Angular)** | Interface do usuário |

### Estilo Arquitetural

- Arquitetura em **camadas**
- **Event-Driven Architecture**
- **Consistência eventual**
- **Sharding por cliente** no processamento de eventos

---

## 🗂 Estrutura do Projeto
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

---

## 🔐 Segurança

### Autenticação

- JWT Bearer Token
- ASP.NET Identity
- Claims relevantes:
  - `clienteId` → escopo do tenant
  - `sub` → email do usuário

### Autorização

- Endpoints sensíveis utilizam `[Authorize]`
- Escopo por cliente garantido via `IUserContext`

---

## 🌐 API — Endpoints

### Auth

| Método | Endpoint            | Descrição |
|-------:|---------------------|-----------|
| POST   | `/Auth/Registrar`   | Registra usuário e cliente |
| POST   | `/Auth/Logar`       | Autentica e retorna JWT |

### Conta (JWT obrigatório)

| Método | Endpoint |
|-------:|----------|
| POST   | `/Conta/Registrar` |
| GET    | `/Conta` |
| GET    | `/Conta/{contaId}` |
| GET    | `/Conta/contasParaTransferencia/{id}` |

### Transação (JWT obrigatório)

| Método | Endpoint |
|-------:|----------|
| POST   | `/Transacao` |
| GET    | `/Transacao/conta/{contaId}` |
| GET    | `/Transacao/passiveisDeEstorno/conta/{contaId}` |

---

## 📄 Swagger

- Disponível na **raiz da aplicação**
- Autenticação via **Bearer JWT**

### Como usar

1. Faça login em `/Auth/Logar`
2. Copie o token JWT retornado
3. Clique em **Authorize** no Swagger
4. Informe: Bearer {seu_token}

Controle de Concorrência

RowVersion habilita concorrência otimista

O EF Core gera:

UPDATE Conta
SET ...
WHERE Id = @Id AND RowVersion = @OriginalRowVersion


Conflitos resultam em DbUpdateConcurrencyException

Base pronta para retry e serialização por cliente

🔄 Fluxo de Transação
1️⃣ Criação (WebApi)

Valida request

Garante que a conta pertence ao cliente logado

Cria Transacao com status PENDENTE

Persiste no banco

Publica TransacaoCriadaEvent

Retorna resposta imediatamente

➡️ Baixa latência no request HTTP

2️⃣ Processamento (Worker)

Consome evento do RabbitMQ

Carrega transação pendente (AsTracking)

Executa regras no ProcessadorTransacaoDomainService

Atualiza saldos e status

Persiste alterações

Invalida cache Redis (origem e destino)

🧠 Regras de Negócio (Domain)

Implementadas em ProcessadorTransacaoDomainService:

Crédito

Débito (respeita limite)

Reserva

Captura

Transferência

Estorno (operação compensatória)

Características:

Domínio isolado de infraestrutura

Mutação de estado explícita

Erros controlados

Lógica centralizada

🧊 Cache (Redis)
Estratégia

Cache-aside

Apenas leitura

TTL: 1 dia

Invalidação

Executada pelo worker somente em transações SUCESSO

➡️ Garante consistência eventual

📬 Mensageria (RabbitMQ)

Exchange: transacoes.exchange

Routing key: transacoes.shard-{n}

Shard calculado a partir do ClienteId

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

DTO inválido → 422 Unprocessable Entity

Retorno padronizado (ResultPattern)

Exceções

Middleware global

HTTP 500

Mensagem genérica ao cliente

Log detalhado internamente

🐳 Execução Local (Docker)
Subir ambiente completo
docker compose up --build

Serviços disponíveis
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

DLQ + retry

🧠 Notas de Arquitetura (Senior Notes)

✔ Event-driven
✔ Separação Application / Domain / Infrastructure
✔ Cache consciente
✔ Concorrência otimista
✔ Worker dedicado
✔ Sharding por cliente

Próximos passos

Retry para DbUpdateConcurrencyException

Idempotência explícita no consumer

Rate limiting

Secrets Manager

RBAC

Outbox Pattern

🎯 Pitch para entrevista

“Esse projeto simula um core financeiro real. A API cria transações pendentes e publica eventos. Um worker processa as regras do domínio e atualiza saldo e status, usando sharding por cliente e concorrência otimista. O sistema é consistente de forma eventual e escalável.”

✅ Conclusão

Este projeto demonstra:

maturidade técnica

domínio de .NET moderno

entendimento real de sistemas distribuídos

preocupação com produção e escala

📌 Projeto totalmente válido como portfólio sênior.


---

Se quiser, no próximo passo eu posso:
- **converter isso para Confluence**
- **gerar diagramas C4**
- **criar ADRs**
- **revisar o README como se fosse um tech lead exigente**

Só falar 👍
