# 💳 Private Banking – Sistema de Transações Financeiras (API + Worker + Frontend Angular)

## 📌 Visão Geral

Este repositório contém uma solução completa para simular um **sistema financeiro / private banking**, com:

- **Web API** (ASP.NET Core) para autenticação, contas e transações
- **Worker de Transações** (BackgroundService) para processamento assíncrono via RabbitMQ
- **Frontend Angular** (painel web) com autenticação, proteção de rotas e interceptor de 401
- **Infra local via Docker Compose** (SQL Server, RabbitMQ Management, Redis)

A arquitetura
- **DDD + Clean Architecture**
- **Event-Driven / Consistência eventual**
- **Sharding em filas RabbitMQ** (roteamento determinístico + single-active-consumer por shard)
- **Cache Redis com invalidação orientada a evento**
- **Health checks / readiness e liveness**
- **Testes (domínio e aplicação) como documentação executável**

---

## 🧱 Arquitetura

### Camadas / Projetos
- **Domain**
  - Entidades (`Cliente`, `Conta`, `Transacao`)
  - Enums
  - `ProcessadorTransacaoDomainService` (Serviço de domínio)
- **Application**
  - Services (ex.: `AuthService`, `ContaService`, `TransacaoService`)
  - Orquestração e publicação de eventos
  - Padrão de retorno `ResultPattern`
- **Infraestrutura**
  - EF Core + SQL Server
  - RabbitMQ (publisher/consumer, topology sharded, retry/DLQ)
  - Redis (cache)
- **WebApi**
  - Controllers
  - JWT + Identity
  - Swagger
  - Middlewares (exception handling)
  - Health checks
- **WorkerTransacao**
  - Consumer sharded (`TransacaoCriadaConsumer`)
  - Processamento assíncrono e invalidação de cache
- **Frontend (Angular)**
  - Login / Cadastro / Registro de Contas Bancarias e Transações
  - Proteção de rotas (Auth Guard)
  - Interceptor: tratamento de **401** (ex.: logout/redirecionamento)

---

## 🧩 Componentes

### 1) Web API (ASP.NET Core)
Responsável por:
- Registro e login de usuário (Identity + JWT)
- Registro e consulta de contas
- Criação e consulta de transações
- Health checks e Swagger

Recursos importantes:
- **JWT** com claim `clienteId`
- `IUserContext` para obter `ClienteId` via claims
- **Swagger** com Bearer Token
- **API Behavior** customizado (422 para ModelState inválido)
- **Exception Middleware** padronizando erro interno

---

### 2) WorkerTransacao (.NET Worker Service)
Responsável por:
- Consumir evento `TransacaoCriadaEvent`
- Carregar transação pendente + relacionamentos (origem/destino/cliente/transacao estornada)
- Executar domínio (`ProcessadorTransacaoDomainService.Processar`)
- Persistir alterações
- **Invalidar cache** de contas após sucesso

Resiliência:
- **Retry com TTL** + **DLQ** por shard
- Controle de tentativas via header `x-attempts`

---

### 3) Frontend Angular
Responsável por:
- UI de painel (contas, saldos, transações)
- Registro de conta e operações
- Autenticação e sessão
- Proteção de rotas
- Interceptor para tratar **401** retornado pela API (ex.: redireciona para login / limpa token)

> Observação: o frontend é servido via container na porta `4200` (mapeado para Nginx/HTTP interno do container).

---

## 🔐 Segurança (JWT + Identity)

- Login gera token JWT com:
  - `sub`: email
  - `jti`: identificador único do token
  - `clienteId`: identificação do cliente
- Endpoints de conta/transação exigem `[Authorize]`
- A API configura `ClockSkew` e valida issuer/audience/key

---

## 🔁 Fluxo de Transação (consistência eventual)

```text
Usuário (Frontend)
  ↓ (HTTP)
Web API cria Transação (Status = PENDENTE) + salva no SQL Server
  ↓ (RabbitMQ)
Publica TransacaoCriadaEvent em transacoes.exchange (routingKey sharded)
  ↓ (WorkerTransacao)
Consumer lê mensagem do shard, processa regras, atualiza contas e transação
  ↓ (Redis)
Invalida cache da conta origem/destino após sucesso
```

---

## 📬 Mensageria (RabbitMQ Sharded)

### Topologia (por shard)
- Exchange principal: `transacoes.exchange` (Direct)
- Exchange retry: `transacoes.exchange.retry`
- Exchange DLX: `transacoes.exchange.dlx`

Para cada shard:
- `transacoes.shard-{n}.queue` (principal)
- `transacoes.shard-{n}.queue.retry` (TTL + dead-letter para principal)
- `transacoes.shard-{n}.queue.dlq` (dead-letter final)

Configurações relevantes:
- `x-single-active-consumer` habilitado nas filas (evita consumo concorrente no mesmo shard)
- `prefetchCount` configurável
- Tentativas controladas por header `x-attempts`

Roteamento:
- O shard é calculado de forma determinística (SHA256 → int → mod `ShardCount`)

---

## 🔒 Controle de Concorrência (RowVersion / Optimistic Lock)

O sistema utiliza **controle de concorrência otimista** através do campo **RowVersion** na entidade Conta

### Como funciona
- A entidade `Conta` possui a propriedade `RowVersion`
- No Entity Framework Core, ela é configurada como:
  - `IsRowVersion()`
  - `IsConcurrencyToken()`
- A cada atualização da linha, o banco altera automaticamente o valor do `RowVersion`

### Benefícios
- Evita **lost updates** em cenários concorrentes
- Garante integridade de saldo em operações financeiras
- Não exige bloqueios pessimistas no banco
- Escala melhor em ambientes de alta concorrência

---

## 💾 Cache (Redis)

- Cache aplicado na consulta de conta (cache-first)
- TTL padrão: **1 dia**
- Invalidação de cache ao final do processamento assíncrono (após sucesso da transação)

---

## ❤️ Health Checks

A API expõe:
- `GET /health/live`  
  Liveness (sempre OK quando o processo está de pé)
- `GET /health/ready`  
  Readiness baseado em tags:
  - `database` (SQL Server)
  - `messaging` (RabbitMQ)

---

## 📚 Swagger

- UI em `/swagger`
- Suporte a Bearer Token
- Útil para testes manuais dos endpoints

---

## 🐳 Docker Compose (ambiente local)

Este repositório inclui `docker-compose.yml` com os serviços:

- `webapi` (API) — portas `8080` e `8081`
- `workertransacao` (worker)
- `rabbitmq` (RabbitMQ + Management) — portas `5672` e `15672`
- `sqlserver` (SQL Server 2022) — porta `1433`
- `redis` (Redis) — porta `6379`
- `frontend` (Angular) — porta `4200`

### Subir tudo
```bash
docker compose up --build
```

### Acessos
- Frontend: `http://localhost:4200`
- API (HTTP): `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- RabbitMQ Management: `http://localhost:15672`
- SQL Server: `localhost,1433`
- Redis: `localhost:6379`

### Credenciais padrão (docker-compose)
RabbitMQ:
- Usuário: `user`
- Senha: `password`

SQL Server:
- `SA_PASSWORD`: `YourStrong!Passw0rd`

---

## ✅ Testes

A solução contém testes cobrindo:
- Regras de domínio (ProcessadorTransacao)
- Services de aplicação (Auth/Conta/Transacao)

Exemplo (na raiz da solução):
```bash
dotnet test
```

---

## 📦 Observabilidade (correlationId)

- A API passa `HttpContext.TraceIdentifier` como `correlationId` na publicação do evento
- O worker registra logs com `CorrelationId`
- Isso permite rastrear ponta-a-ponta: request → evento → processamento

---

## 📈 Teste de Carga (k6)

### Cenário: **5.000 requisições** (shared-iterations)
- **Executor:** `shared-iterations`
- **VUs:** 20
- **Total de requisições:** 5.000
- **Endpoint:** `POST /Transacao`
- **Payload:** `quantia = 1` (mesma conta para estressar concorrência — *hot account*)
- **Falhas HTTP:** 0%

### Resultados (HTTP)
- **Throughput:** **238.88 req/s** (`http_reqs`)
- **Latência média (avg):** **83.3 ms**
- **p90:** **80.59 ms**
- **p95:** **90.98 ms**
- **Máximo:** **2.99 s**
- **Erros:** **0.00%** (0/5000)

## ⚙️ Capacidade de Processamento do Worker

Durante os testes, a capacidade real do worker foi medida diretamente no RabbitMQ através da taxa de **ACK/s** (mensagens processadas com sucesso).

### Resultado observado
- **Throughput do worker:** **~100 transações por segundo (TPS)**
- Medido como:
  - ~50 ACK/s em `transacoes.shard-0.queue`
  - ~50 ACK/s em `transacoes.shard-1.queue`

Esse valor representa **processamento end-to-end real**, incluindo:
- consumo da mensagem
- execução da regra de negócio
- controle de concorrência (`rowVersion`)
- persistência no banco
- invalidação de cache
- `BasicAck` no RabbitMQ

> ⚠️ Observação: o teste foi executado em cenário de **alta contenção** (hot account), com poucas contas ativas e somente 1 replica, o que reduz o throughput máximo teórico. Em cenários com mais contas, o TPS tende a aumentar.

---

## 📈 Teste de Carga Inicial (k6 – Ramp-up)

### Cenário
- **Executor:** `ramping-vus`
- **VUs máximos:** 50
- **Duração total:** ~1 minuto
- **Endpoint:** `POST /Transacao`
- **Objetivo:** avaliar latência e capacidade de ingestão da API sob aumento progressivo de carga

### Resultados (HTTP)
- **Total de requisições:** **12.753**
- **Throughput médio:** **~212.5 req/s**
- **Latência média:** **94.72 ms**
- **p90:** **101.16 ms**
- **p95:** **127.39 ms**
- **Falhas HTTP:** **0.00%**

---

## 🔭 Próximos Passos (Evoluções Planejadas)
- Logs estruturados com Serilog
- Métricas com Prometheus + Grafana
- Tracing distribuído com OpenTelemetry + Jaeger
