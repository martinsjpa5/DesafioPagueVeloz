# Core Financeiro — WebApi + Worker + Mensageria (RabbitMQ) + Cache (Redis)

## 📌 Visão Geral

Este projeto implementa um **core financeiro orientado a eventos**, com processamento assíncrono de transações, separação clara de responsabilidades e foco em **consistência eventual**, **escalabilidade** e **qualidade de código**.

Ele simula cenários reais encontrados em sistemas financeiros, como:
- processamento assíncrono de operações
- controle de concorrência
- estorno como operação compensatória
- cache de leitura
- sharding de filas
- observabilidade e testes automatizados

O projeto é totalmente executável via **Docker Compose**.

---

## 🧱 Arquitetura

### Componentes

| Componente | Responsabilidade |
|----------|------------------|
| **WebApi** | Exposição de endpoints REST, autenticação JWT, criação de transações |
| **WorkerTransacao** | Processamento assíncrono das transações |
| **RabbitMQ** | Transporte de eventos e desacoplamento |
| **SQL Server** | Persistência relacional |
| **Redis** | Cache de leitura de contas |
| **Frontend (Angular)** | Interface do usuário |

### Estilo Arquitetural
- Arquitetura em camadas
- Event-driven
- Consistência eventual
- Escalabilidade horizontal via sharding

---

## 🧠 Modelo de Domínio

### Entidades Principais

#### Cliente
- Representa o tenant/dono das contas

#### Conta
- `SaldoDisponivel`
- `SaldoReservado`
- `LimiteDeCredito`
- `Status`
- `RowVersion` (controle de concorrência otimista)
- `ClienteId`

#### Transacao
- Criada com `Status = PENDENTE`
- Processada pelo worker
- Estados possíveis:
  - `PENDENTE`
  - `SUCESSO`
  - `FALHA`
- Tipos:
  - Crédito
  - Débito
  - Reserva
  - Captura
  - Transferência
  - Estorno

---

## 🔄 Fluxo de Transação (End-to-End)

### 1️⃣ Criação (WebApi)
1. Cliente chama `POST /Transacao`
2. API valida regras de entrada
3. Cria transação com status `PENDENTE`
4. Persiste no banco
5. Publica evento `TransacaoCriadaEvent` no RabbitMQ
6. Retorna resposta imediatamente

### 2️⃣ Processamento (Worker)
1. Worker consome evento da fila
2. Carrega transação pendente
3. Executa regras do domínio
4. Atualiza saldos e status
5. Persiste alterações
6. Invalida cache Redis (se sucesso)

---

## 📬 Mensageria e Sharding

- Exchange: `transacoes.exchange`
- Routing key base: `transacoes`
- Routing final: `transacoes.shard-{n}`

O shard é calculado com base no `ClienteId`, permitindo:
- paralelismo controlado
- redução de contenção
- escalabilidade previsível

---

## ⚡ Cache (Redis)

- Estratégia: **Cache-Aside**
- Cache aplicado apenas para leitura de contas
- TTL: **1 dia**
- Invalidação automática após transação processada com sucesso

➡️ Garante leitura rápida com **consistência eventual**.

---

## 🔐 Segurança

### Autenticação
- JWT Bearer Token
- Claims:
  - `sub` (email)
  - `jti`
  - `clienteId`

### Autorização
- Endpoints protegidos com `[Authorize]`
- Escopo garantido por `clienteId` via `IUserContext`

---

## 📡 API — Endpoints

### Auth
- `POST /Auth/Registrar`
- `POST /Auth/Logar`

### Conta (JWT obrigatório)
- `POST /Conta/Registrar`
- `GET /Conta`
- `GET /Conta/{contaId}`
- `GET /Conta/contasParaTransferencia/{id}`

### Transação (JWT obrigatório)
- `POST /Transacao`
- `GET /Transacao/conta/{contaId}`
- `GET /Transacao/passiveisDeEstorno/conta/{contaId}`

---

## 🧾 Swagger / OpenAPI

- Swagger habilitado
- Suporte a JWT Bearer Token
- Acessível na raiz da aplicação:
  ```
  http://localhost:8080
  ```

---

## 🩺 Health Checks

### Liveness
```
GET /health/live
```

### Readiness
```
GET /health/ready
```
Verifica:
- Banco de dados
- Mensageria

---

## 🧪 Testes

O projeto possui **testes unitários focados em comportamento**, cobrindo:

### ContaService
- Cache hit / miss
- Não consultar banco quando cache existe
- Não setar cache indevidamente
- TTL correto

### TransacaoService
- Validações de entrada
- Transferência (erros e sucesso)
- Estorno (erros e sucesso)
- Verificação de:
  - persistência
  - publicação de evento
  - correlationId
  - exchange e routing key corretos

➡️ Testes validam **efeitos colaterais**, não apenas retorno.

---

## 🧩 Concorrência

- Controle de concorrência otimista via `RowVersion`
- Prepara o sistema para múltiplas transações concorrentes na mesma conta
- Base sólida para retry ou serialização futura

---

## 🛠️ Execução Local (Docker Compose)

### Subir ambiente completo
```bash
docker compose up --build
```

### Serviços disponíveis

| Serviço | Endereço |
|------|---------|
| WebApi | http://localhost:8080 |
| Frontend | http://localhost:4200 |
| RabbitMQ UI | http://localhost:15672 |
| SQL Server | localhost:1433 |
| Redis | localhost:6379 |

RabbitMQ:
- user: `user`
- password: `password`

---

## 📊 Observabilidade

### Implementado
- CorrelationId propagado até o worker
- Logs estruturados
- Health checks

### Evoluções recomendadas
- OpenTelemetry
- Métricas Prometheus
- Tracing distribuído
- DLQ + retry no consumer

---

## 📋 Checklist de Produção

- [ ] Secrets em Secret Manager
- [ ] Rate limiting
- [ ] CORS restrito
- [ ] Retry + DLQ
- [ ] Tratamento de concorrência (retry RowVersion)
- [ ] Remover `EnsureCreated` em produção
- [ ] Observabilidade completa

---

## 🧠 Decisões Arquiteturais (ADR)

### Processamento Assíncrono
Transações são criadas como `PENDENTE` e processadas fora do request HTTP para reduzir latência e aumentar escalabilidade.

### Cache de Leitura
Cache Redis aplicado apenas para leitura de contas, com invalidação após sucesso do processamento.

### Estorno como Compensação
Estorno é tratado como operação reversa explícita, garantindo integridade do histórico financeiro.

---

**Perfeitamente utilizável como projeto de portfólio sênior.**
