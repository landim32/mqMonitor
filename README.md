# mqMonitor

Sistema distribuído de monitoramento orientado a eventos utilizando .NET 8, RabbitMQ e PostgreSQL.

## Arquitetura

```
[Test Producer] → RabbitMQ (tests.events) → [Test Worker] → Publica eventos
                                                                    ↓
                                                            RabbitMQ (tests.events)
                                                                    ↓
                                                           [Monitor Service]
                                                                    ↓
                                                            PostgreSQL (Read Model)
                                                                    ↓
                                                            API REST (/api/tests)
```

## Projetos

| Projeto | Descrição |
|---------|-----------|
| `Test.Contracts` | Modelos compartilhados, eventos e comandos |
| `Test.Infrastructure` | RabbitMQ, Entity Framework, injeção de dependência |
| `Test.Monitor` | Consumidor de eventos + API REST de observabilidade |
| `Test.Worker` | Executor de testes + handler de cancelamento |
| `Test.Producer` | CLI para publicar eventos test.created |

## Pré-requisitos

- .NET 8 SDK
- Docker e Docker Compose

## Como executar

### 1. Subir infraestrutura

```bash
docker-compose up -d rabbitmq postgres
```

### 2. Executar Monitor (API)

```bash
cd src/Test.Monitor
dotnet run
```

API disponível em `http://localhost:5000`

### 3. Executar Worker

```bash
cd src/Test.Worker
dotnet run
```

### 4. Enviar testes (Producer)

```bash
cd src/Test.Producer
dotnet run
```

No prompt interativo, use `send 5` para enviar 5 testes.

### Ou com Docker Compose (tudo junto)

```bash
docker-compose up --build
```

## API Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/tests` | Listar todas execuções |
| GET | `/api/tests/{testId}` | Detalhar execução |
| GET | `/api/tests/{testId}/events` | Histórico de eventos do teste |
| POST | `/api/tests/{testId}/cancel` | Cancelar execução |
| GET | `/api/tests/metrics` | Métricas consolidadas |

## Padrões Implementados

- **Event-Driven Architecture (EDA)** - Comunicação via eventos
- **CQRS** - Monitor como Read Side
- **Idempotent Consumer** - Deduplicação por EventId
- **Dead Letter Queue (DLQ)** - Mensagens com falha persistente
- **Retry Pattern** - Reprocessamento com fila intermediária (TTL)
- **Command Pattern** - Cancelamento via `cancel.test`
- **Competing Consumers** - Workers escaláveis horizontalmente

## RabbitMQ Topology

- **Exchange `tests.events`** (topic) - Eventos do ciclo de vida
- **Exchange `tests.commands`** (topic) - Comandos de controle
- **Exchange `tests.dlx`** (topic) - Dead Letter Exchange
- **Queue `tests.worker`** - Worker consome `test.created`
- **Queue `tests.monitor`** - Monitor consome `test.*`
- **Queue `tests.cancel`** - Worker escuta `cancel.test`
- **Queue `tests.retry`** - Retry com TTL de 5s

## Licença

MIT
