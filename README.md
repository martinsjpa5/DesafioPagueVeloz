# 💳 Sistema de Transações Financeiras – API & Worker Assíncrono

## 📌 Visão Geral

Este projeto implementa uma **plataforma de transações financeiras** com foco em **robustez, escalabilidade, consistência e boas práticas de mercado**, utilizando uma arquitetura baseada em **DDD (Domain-Driven Design) + Clean Architecture** e processamento **assíncrono orientado a eventos**.

A solução suporta operações financeiras críticas como **Crédito, Débito, Reserva, Captura, Transferência e Estorno**, garantindo:
- Regras de negócio explícitas e centralizadas no domínio
- Processamento resiliente e escalável
- Consistência eventual
- Invalidação de cache orientada a eventos
- Observabilidade por `correlationId`

---

## 🧱 Arquitetura

### Estilo Arquitetural
- Clean Architecture
- Domain-Driven Design (DDD)
- Event-Driven Architecture

### Separação de Camadas

- **Domain**
  - Entidades
  - Enums
  - Serviços de domínio
  - Regras de negócio puras

- **Application**
  - Casos de uso
  - Orquestração
  - Validações
  - Publicação de eventos

- **Infraestrutura**
  - Entity Framework Core
  - RabbitMQ
  - Redis
  - Implementações técnicas

- **WebApi**
  - Controllers
  - Autenticação
  - Autorização
  - Swagger
  - Health Checks

- **WorkerTransacao**
  - Consumers RabbitMQ
  - Processamento assíncrono
  - Invalidação de cache

---

## 🧩 Componentes Principais

### Web API

Responsável por:
- Autenticação de usuários
- Emissão de JWT
- Cadastro de clientes e contas
- Criação de transações
- Consultas de contas e transações

Tecnologias:
- ASP.NET Core
- Identity Core
- JWT Bearer
- Swagger
- EF Core
- Redis
- RabbitMQ (Publisher)

---

### Worker de Transações

Responsável por:
- Consumir eventos de transações criadas
- Processar regras de negócio no domínio
- Atualizar saldos das contas
- Invalidar cache Redis
- Garantir idempotência e resiliência

Tecnologias:
- .NET Worker Service
- RabbitMQ (Consumer Sharded)
- EF Core
- Redis

---

## 🔐 Segurança

- Autenticação via **JWT Bearer**
- Tokens assinados com **HMAC SHA256**
- Claims:
  - `sub` → email do usuário
  - `clienteId` → identificação do cliente
- Endpoints protegidos com `[Authorize]`
- Contexto do usuário acessado via `IUserContext`

---

## 🔁 Fluxo de Processamento de Transação

```text
Cliente
  ↓
Web API
  ↓
Validações iniciais
  ↓
Criação da Transação (Status = PENDENTE)
  ↓
Publicação de Evento (RabbitMQ)
  ↓
Worker consome evento
  ↓
Processamento no Domínio
  ↓
Atualização de saldos
  ↓
Invalidação de cache
```

---

## 📬 Mensageria (RabbitMQ)

### Estratégia

- Arquitetura orientada a eventos
- Sharding determinístico por chave
- Garantia de ordenação por shard
- Single Active Consumer por fila

### Topologia

- Exchange principal: `transacoes.exchange`
- Exchange de retry: `transacoes.exchange.retry`
- Exchange de DLQ: `transacoes.exchange.dlx`

Cada shard possui:
- Fila principal
- Fila de retry com TTL
- Dead Letter Queue (DLQ)

### Resiliência

- Retry automático
- Controle de tentativas via header `x-attempts`
- Após exceder o limite → mensagem enviada para DLQ

---

## 💾 Cache (Redis)

- Cache aplicado para leitura de contas
- Estratégia **cache-first**
- TTL padrão: **1 dia**

### Invalidação de Cache

- Executada somente após sucesso no processamento da transação
- Invalidação automática:
  - Conta origem
  - Conta destino (quando aplicável)

---

## 🧠 Domínio

### Entidades Principais

- Cliente
- Conta
- Transacao

### Tipos de Operação

- Crédito
- Débito
- Reserva
- Captura
- Transferência
- Estorno

### Regras de Negócio

- Validação de quantia e moeda
- Controle de saldo disponível e limite de crédito
- Reserva e captura desacopladas
- Estorno reversível conforme tipo da transação original
- Nenhuma mutação ocorre em caso de falha

Toda a lógica está centralizada no **ProcessadorTransacaoDomainService**.

---

## 🧪 Testes

- Cobertura completa de:
  - Domínio
  - Application Services
- Testes escritos como **documentação executável**
- Validação de cenários de erro e sucesso
- Garantia de não mutação em falhas

---

## ❤️ Health Checks

Disponíveis na Web API:

- `/health/live`
  - Verifica se a aplicação está em execução

- `/health/ready`
  - Verifica dependências:
    - SQL Server
    - RabbitMQ

---

## 📚 Swagger

- Documentação interativa
- Suporte a autenticação JWT Bearer
- Facilita testes manuais e integração

---

## ⚙️ Configuração

### Dependências Externas

- SQL Server
- Redis
- RabbitMQ


---

## 🚀 Considerações de Produção

- Arquitetura preparada para alta concorrência
- Sharding evita contenção em filas
- Cache reduz carga no banco
- Retry e DLQ garantem resiliência
- CorrelationId habilita rastreabilidade ponta-a-ponta

---

