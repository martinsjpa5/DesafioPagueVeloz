# Context Diagram
```mermaid
graph TD
User --> Frontend
Frontend --> API
API --> DB
API --> RabbitMQ
RabbitMQ --> Worker
Worker --> DB
Worker --> Redis
```