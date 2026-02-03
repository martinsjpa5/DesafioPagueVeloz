# Container Diagram
```mermaid
graph TD
Angular --> WebApi
WebApi --> SQLServer
WebApi --> Redis
WebApi --> RabbitMQ
RabbitMQ --> Worker
Worker --> SQLServer
```