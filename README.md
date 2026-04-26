# Resilient Order Processing System

## Overview
This project implements a **production-grade backend system** for processing customer orders reliably under concurrent load, as required in the technical assessment.

The system is designed with real-world engineering concerns in mind:
- Concurrency safety
- Data consistency
- Fault tolerance
- Scalability-ready design

---

## Architecture

The system follows a **Clean Architecture-inspired structure**:

API → Application → Infrastructure → Domain

### Layers

**API**
- Handles HTTP requests
- Exposes `/api/orders`
- Configures dependency injection, Swagger, and DB

**Application**
- Contains business logic (`OrderService`)
- Handles validation, retries, orchestration

**Infrastructure**
- EF Core + SQLite
- `AppDbContext`
- Data persistence logic

**Domain**
- Core entities (`Product`, `Order`)

---

## ⚙️ Tech Stack

- ASP.NET Core (.NET 7/8)
- Entity Framework Core
- SQLite
- Polly (Retry handling)
- Swagger (OpenAPI)

---

## How to Run

```bash
cd src/API
dotnet restore
dotnet run
```

Open Swagger:
```
http://localhost:5000/swagger
```

---

## Database

- SQLite database: `orders.db`
- Location: `src/API/`

### Inspect DB

```bash
sqlite3 orders.db
.tables
SELECT * FROM Products;
SELECT * FROM Orders;
```

Or use **DB Browser for SQLite**

---

## API Usage

### Create Order

POST `/api/orders`

Headers:
```
Idempotency-Key: unique-key-123
```

Body:
```json
{
  "items": [
    { "productId": 1, "quantity": 2 }
  ]
}
```

---

## Core Features

### 1. Order Processing
- Supports multiple products
- Validates:
  - Product existence
  - Stock availability

---

### 2. Inventory Management (Concurrency Safe)

To prevent overselling:

- Database transaction:
```csharp
using var tx = await _db.Database.BeginTransactionAsync();
```

- Optimistic concurrency:
```csharp
RowVersion
```

- Retry mechanism:
```csharp
Polly WaitAndRetryAsync
```

---

### 3. Idempotency

Each request includes:

```
Idempotency-Key
```

Ensures:
- No duplicate orders
- Safe retries
- Network fault tolerance

---

### 4. Event-Driven Flow (Simulated)

After order creation:

- OrderPlaced event triggered
- Simulates:
  - Payment processing
  - Inventory confirmation
  - Notification

Example logs:
```
[EVENT] OrderPlaced
[PAYMENT] Processing
[NOTIFICATION] Sent
```

---

## Reliability & Failure Handling

### Concurrency Conflicts
- EF Core concurrency tokens
- Retry with Polly

### Duplicate Requests
- Idempotency keys

### Partial Failures
- Transactions ensure atomic operations

---

## Architecture Decisions

### Optimistic Concurrency (RowVersion)
- Chosen to handle high-concurrency scenarios without locking rows
- Prevents race conditions while maintaining performance
- Works well with EF Core and lightweight databases like SQLite

---

### Transactions for Atomicity
- Ensures stock updates and order creation happen as a single unit
- Prevents partial updates and inconsistent states
- Guarantees data integrity under failure conditions

---

### Retry Strategy (Polly)
- Handles transient concurrency conflicts gracefully
- Automatically retries failed operations due to race conditions
- Improves system resilience under load

---

### Idempotency Handling
- Prevents duplicate order creation from repeated requests
- Ensures safe retries in case of network issues or client retries
- Implemented using unique Idempotency-Key per request

---

## Trade-offs

### Optimistic vs Pessimistic Locking
- Chose optimistic concurrency to avoid database locks and improve throughput
- Trade-off: requires retry logic to handle conflicts

---

### SQLite vs Production Databases
- SQLite chosen for simplicity and ease of setup
- Trade-off: limited concurrency capabilities compared to PostgreSQL/MySQL

---

### In-Memory Event Handling
- Simplifies implementation without external dependencies
- Trade-off: lacks durability and reliability in distributed systems
- Production alternative: Kafka, RabbitMQ

---

### No Distributed Locking
- Avoided Redis/distributed locks to keep system lightweight
- Trade-off: relies on database-level concurrency control instead

---

## Additional Considerations

- Designed for **high concurrency scenarios** with safe stock updates
- Ensured **idempotent APIs** for reliability under retries
- Separated **transactional logic from side effects** to prevent data inconsistency
- Focused on **clarity and maintainability** over over-engineering

### Transactions + RowVersion
- Guarantees atomic updates
- Prevents overselling under concurrent load

---

## Concurrency Testing

Scenario:
- Product stock = 5
- Send 10 concurrent requests

Expected:
- Some succeed
- Some fail- Stock is **never negative**  
- No overselling occurs  

#### Verified Outcomes

| Check | Result |
|------|--------|
| Successful order creation | ✅ |
| Stock validation triggered | ✅ |
| Overselling prevented | ✅ |
| Stock consistency maintained | ✅ |

 Note: Failures only occur when requested quantity exceeds available stock.

## Practical Testing Guide

### Concurrency Test (Parallel Requests)

Simulate multiple simultaneous requests:

```bash
for i in {1..10}; do
  curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-$i" \
  -d '{"items":[{"productId":1,"quantity":30}]}' &
done
wait

### Idempotency Test

**Headers:**

Idempotency-Key: abc123

**Body:**
```json
{
  "items": [
    { "productId": 1, "quantity": 2 }
  ]
}

#### Verified Outcomes

| Check | Result |
|------|--------|
| Duplicate request prevented | ✅ |
| Same order returned | ✅ |
| No extra stock deduction | ✅ |
| System remains consistent | ✅ |

---

## Bonus Features

- Background worker
- Retry mechanism (Polly)
- Swagger documentation
- Logging (Console)

---

## Future Improvements

- Message broker (Kafka/RabbitMQ)
- Distributed locking (Redis)
- Outbox pattern
- Structured logging (Serilog)
- Integration testing

---

## Author Notes

This system was designed with real-world backend considerations:
- Reliability under load
- Data consistency
- Fault tolerance
- Maintainability

