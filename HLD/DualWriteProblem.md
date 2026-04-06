# Dual Write Problem — System Design Notes

## 1. What is the Dual Write Problem?

A **dual write** occurs when a service needs to update **two or more separate systems** (e.g., database + message queue, two databases, database + cache) as part of a single business operation, but **cannot do so atomically**.

```
Order Service
     │
     ├──① Write to Database     ✅ Success
     │
     └──② Publish to Kafka      ❌ Fails (network timeout)
     
Result: DB has the order, but no event was published.
        Downstream services (inventory, billing) never learn about it.
        DATA INCONSISTENCY.
```

### Why Can't We Just Use a Transaction?

Distributed transactions (2PC) across heterogeneous systems (e.g., PostgreSQL + Kafka) are:

- **Not supported** — Kafka doesn't participate in XA/2PC
- **Slow** — coordinator-based protocols add latency
- **Fragile** — any participant failure blocks the entire transaction
- **Not scalable** — locks held across systems kill throughput

The fundamental issue: **there is no atomic operation that spans two independent systems**.

---

## 2. All the Ways Dual Writes Fail

### Scenario 1: Second Write Fails

```
① DB.Save(order)        ✅
② Kafka.Publish(event)  ❌  ← network error, broker down, timeout

Result: Order exists in DB but downstream services are unaware.
```

### Scenario 2: First Write Fails After Side Effect

```
① Kafka.Publish(event)  ✅
② DB.Save(order)        ❌  ← constraint violation, DB down

Result: Event published for an order that doesn't exist.
        Inventory reserved for a phantom order.
```

### Scenario 3: Service Crashes Between Writes

```
① DB.Save(order)        ✅
   ── SERVICE CRASHES ──
② Kafka.Publish(event)  ❌  ← never executed

Result: Same as Scenario 1. No retry possible — the in-flight
        state is lost with the process.
```

### Scenario 4: Race Condition with Concurrent Updates

```
Thread A:                          Thread B:
① DB.Save(order, status=PAID)     ① DB.Save(order, status=CANCELLED)
② Kafka.Publish(PAID)             ② Kafka.Publish(CANCELLED)

If execution order is:
  A.① → B.① → B.② → A.②

DB final state:  CANCELLED  ✅
Kafka order:     CANCELLED, then PAID  ❌  ← consumers see PAID last!
```

### The Naive Code That Causes This

```csharp
public async Task PlaceOrder(Order order)
{
    // DUAL WRITE — these two operations are NOT atomic
    await _dbContext.Orders.AddAsync(order);
    await _dbContext.SaveChangesAsync();        // Write 1: Database

    await _messageBus.PublishAsync(new OrderPlacedEvent  // Write 2: Message Bus
    {
        OrderId = order.Id,
        Amount = order.Amount
    });
    // What if PublishAsync throws? DB has the order, bus doesn't.
    // What if the process crashes right here?
}
```

---

## 3. Where Dual Writes Appear in Real Systems

| Pattern | System A | System B | Risk |
|---------|----------|----------|------|
| Event-driven architecture | Database | Message broker (Kafka, SQS) | Lost events |
| Cache-aside | Database | Redis/Memcached | Stale cache |
| Search indexing | Database | Elasticsearch | Missing search results |
| CQRS | Write DB | Read DB / Projection | Stale reads |
| Cross-service sync | Service A's DB | Service B's DB | Data divergence |
| Audit logging | Database | Audit log store | Missing audit trail |

---

## 4. Solution 1: Transactional Outbox Pattern

The **most widely recommended** solution. Write the event to an **outbox table** in the **same database transaction** as the business data. A separate process reads the outbox and publishes to the message broker.

### Architecture

```
Order Service
     │
     ▼
┌─────────────────────────────────┐
│  Database (single transaction)  │
│                                 │
│  ① INSERT INTO orders (...)     │
│  ② INSERT INTO outbox (...)     │
│                                 │
│  COMMIT ← atomic!              │
└─────────────────────────────────┘
                │
                ▼
     Outbox Relay / Poller
                │
                ▼
          Message Broker (Kafka / SQS)
                │
                ▼
        Downstream Consumers
```

### Database Schema

```sql
CREATE TABLE outbox (
    id              BIGINT PRIMARY KEY IDENTITY,
    aggregate_type  VARCHAR(255) NOT NULL,    -- e.g., 'Order'
    aggregate_id    VARCHAR(255) NOT NULL,    -- e.g., order ID
    event_type      VARCHAR(255) NOT NULL,    -- e.g., 'OrderPlaced'
    payload         NVARCHAR(MAX) NOT NULL,   -- JSON serialized event
    created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    published       BIT NOT NULL DEFAULT 0
);

CREATE INDEX IX_outbox_unpublished ON outbox (published, created_at)
    WHERE published = 0;
```

### Code Example (C#)

```csharp
// Step 1: Write business data + outbox entry in ONE transaction
public class OrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db) => _db = db;

    public async Task PlaceOrder(Order order)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Orders.Add(order);

        _db.OutboxMessages.Add(new OutboxMessage
        {
            AggregateType = "Order",
            AggregateId = order.Id.ToString(),
            EventType = "OrderPlaced",
            Payload = JsonSerializer.Serialize(new OrderPlacedEvent
            {
                OrderId = order.Id,
                Amount = order.Amount,
                CreatedAt = DateTime.UtcNow
            }),
            Published = false
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        // Both writes succeed or both fail — ACID guarantees this.
    }
}

// Step 2: Background relay polls outbox and publishes to broker
public class OutboxRelay : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageBus _bus;

    public OutboxRelay(IServiceScopeFactory scopeFactory, IMessageBus bus)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var messages = await db.OutboxMessages
                .Where(m => !m.Published)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .ToListAsync(ct);

            foreach (var msg in messages)
            {
                await _bus.PublishAsync(msg.EventType, msg.Payload);
                msg.Published = true;
            }

            await db.SaveChangesAsync(ct);
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }
}
```

### Pros & Cons

| Pros | Cons |
|------|------|
| **Atomic** — single DB transaction | Adds an outbox table + relay process |
| No distributed transactions needed | Polling adds latency (seconds) |
| Works with any message broker | Outbox table grows — needs cleanup |
| Battle-tested pattern | At-least-once delivery — consumers must be idempotent |

### Outbox Cleanup

```sql
-- Periodic job: delete published messages older than 7 days
DELETE FROM outbox WHERE published = 1 AND created_at < DATEADD(DAY, -7, SYSUTCDATETIME());
```

---

## 5. Solution 2: Change Data Capture (CDC)

Instead of polling the outbox, use **CDC** to stream database changes directly to the message broker. The database's **transaction log** (WAL/binlog) becomes the source of events.

### Architecture

```
Order Service
     │
     ▼
┌──────────┐     Transaction Log      ┌───────────┐      ┌───────┐
│ Database │ ──────────────────────▶   │  Debezium │ ──▶  │ Kafka │
│          │     (WAL / binlog)        │  (CDC)    │      │       │
└──────────┘                           └───────────┘      └───────┘
                                                              │
                                                              ▼
                                                        Consumers
```

### How It Works

1. Service writes to the database normally (or to DB + outbox table)
2. **Debezium** (or similar CDC tool) reads the database transaction log
3. Every committed change is captured and published to Kafka as an event
4. Consumers process events from Kafka

### Two CDC Approaches

**Approach A: CDC on the business table directly**

```
orders table change → Debezium → Kafka topic: db.orders
```

- Simple, no outbox table needed
- But: exposes internal DB schema to consumers (tight coupling)

**Approach B: CDC on the outbox table (Outbox + CDC)**

```
outbox table change → Debezium → Kafka topic: order.events
```

- Best of both worlds: atomic writes + real-time streaming
- Decouples internal schema from event schema
- **This is the recommended approach** — used by Debezium's built-in outbox connector

### Debezium Outbox Connector Config

```json
{
    "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
    "transforms": "outbox",
    "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
    "transforms.outbox.table.field.event.key": "aggregate_id",
    "transforms.outbox.table.field.event.type": "event_type",
    "transforms.outbox.table.field.event.payload": "payload",
    "transforms.outbox.route.topic.replacement": "${routedByValue}.events"
}
```

### Pros & Cons

| Pros | Cons |
|------|------|
| Near real-time (ms latency) | Requires CDC infrastructure (Debezium, Kafka Connect) |
| No polling overhead | Operational complexity — another system to manage |
| Captures ALL changes (even direct DB edits) | DB-specific (WAL format varies) |
| No code changes to the service | Ordering guarantees depend on CDC tool config |

---

## 6. Solution 3: Event Sourcing

Instead of storing current state, store **every state change as an immutable event**. The event log IS the source of truth. No dual write because there's only ONE write — to the event store.

### Architecture

```
Order Service
     │
     ▼
┌──────────────────┐
│   Event Store    │  ← single write target
│                  │
│  OrderCreated    │
│  OrderPaid       │──────▶  Projections (read models)
│  OrderShipped    │──────▶  Kafka / downstream consumers
│  OrderCancelled  │
└──────────────────┘
```

### How It Eliminates Dual Writes

```
Traditional:
  ① Write state to DB        ← Write 1
  ② Publish event to broker  ← Write 2 (dual write!)

Event Sourcing:
  ① Append event to event store  ← ONLY write
  
  Event store subscribers handle the rest:
    → Update read model (projection)
    → Forward to Kafka
    → Update search index
```

### Code Example (C#)

```csharp
// Events — immutable facts
public record OrderCreated(Guid OrderId, string CustomerId, decimal Amount, DateTime OccurredAt);
public record OrderPaid(Guid OrderId, DateTime PaidAt);
public record OrderCancelled(Guid OrderId, string Reason, DateTime CancelledAt);

// Aggregate — rebuilt from events
public class OrderAggregate
{
    public Guid Id { get; private set; }
    public string Status { get; private set; } = "New";
    public decimal Amount { get; private set; }

    private readonly List<object> _uncommittedEvents = new();
    public IReadOnlyList<object> UncommittedEvents => _uncommittedEvents;

    public void Create(Guid id, string customerId, decimal amount)
    {
        Apply(new OrderCreated(id, customerId, amount, DateTime.UtcNow));
    }

    public void Pay()
    {
        if (Status != "New") throw new InvalidOperationException("Order cannot be paid.");
        Apply(new OrderPaid(Id, DateTime.UtcNow));
    }

    private void Apply(object @event)
    {
        When(@event);
        _uncommittedEvents.Add(@event);
    }

    // State transitions driven by events
    private void When(object @event)
    {
        switch (@event)
        {
            case OrderCreated e:
                Id = e.OrderId; Amount = e.Amount; Status = "New"; break;
            case OrderPaid:
                Status = "Paid"; break;
            case OrderCancelled:
                Status = "Cancelled"; break;
        }
    }

    // Rebuild state from history
    public static OrderAggregate FromHistory(IEnumerable<object> events)
    {
        var order = new OrderAggregate();
        foreach (var e in events) order.When(e);
        return order;
    }
}

// Event Store — single write, no dual write
public class EventStore
{
    private readonly AppDbContext _db;

    public EventStore(AppDbContext db) => _db = db;

    public async Task SaveEvents(Guid aggregateId, IEnumerable<object> events)
    {
        foreach (var e in events)
        {
            _db.Events.Add(new StoredEvent
            {
                AggregateId = aggregateId,
                EventType = e.GetType().Name,
                Payload = JsonSerializer.Serialize(e, e.GetType()),
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        // Single write. Subscribers/projections handle the rest.
    }
}
```

### Pros & Cons

| Pros | Cons |
|------|------|
| **Eliminates dual write entirely** | Paradigm shift — steep learning curve |
| Complete audit trail for free | Eventual consistency for read models |
| Time-travel debugging | Event schema evolution is complex |
| Natural fit for event-driven systems | Rebuilding state from events can be slow (use snapshots) |

---

## 7. Solution 4: Listen to Yourself Pattern

The service publishes an event to the message broker FIRST, then **consumes its own event** to update the database. Single source of truth = the event.

### Architecture

```
Order Service
     │
     ① Publish OrderPlaced to Kafka
     │
     ▼
   Kafka
     │
     ├──▶ Order Service (self-consumer) ──▶ ② Save to DB
     ├──▶ Inventory Service
     └──▶ Billing Service
```

### Code Example (C#)

```csharp
// API endpoint — only publishes, does NOT write to DB
public class OrderController : ControllerBase
{
    private readonly IMessageBus _bus;

    public OrderController(IMessageBus bus) => _bus = bus;

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CreateOrderRequest request)
    {
        var @event = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Amount = request.Amount
        };

        await _bus.PublishAsync(@event);  // Single write — to Kafka only
        return Accepted(new { @event.OrderId });
    }
}

// Self-consumer — listens to its own events and writes to DB
public class OrderEventHandler
{
    private readonly AppDbContext _db;

    public OrderEventHandler(AppDbContext db) => _db = db;

    public async Task Handle(OrderPlacedEvent @event)
    {
        var order = new Order
        {
            Id = @event.OrderId,
            CustomerId = @event.CustomerId,
            Amount = @event.Amount
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
    }
}
```

### Pros & Cons

| Pros | Cons |
|------|------|
| No dual write — single write to broker | DB is eventually consistent (not immediate) |
| All consumers (including self) see same event order | Cannot validate against DB before publishing |
| Simple architecture | Harder to return synchronous responses (e.g., "order created") |
| | Broker becomes a critical dependency |

---

## 8. Solution 5: Saga Pattern (for Cross-Service Writes)

When the dual write spans **multiple services** (not just DB + broker), use a Saga — a sequence of local transactions with compensating actions on failure.

### Choreography-Based Saga

```
Order Service          Payment Service         Inventory Service
     │                       │                        │
     ① Create Order          │                        │
     │──OrderCreated──▶      │                        │
     │                  ② Charge Payment              │
     │                       │──PaymentCharged──▶     │
     │                       │                   ③ Reserve Stock
     │                       │                        │
     │                       │                   ❌ Out of Stock!
     │                       │    ◀──StockFailed──    │
     │                  ④ Refund Payment              │
     │   ◀──PaymentRefunded──│                        │
     ⑤ Cancel Order          │                        │
```

### Orchestration-Based Saga

```
                    Saga Orchestrator
                         │
            ┌────────────┼────────────┐
            ▼            ▼            ▼
      Order Service  Payment Svc  Inventory Svc
      ① Create       ② Charge     ③ Reserve
            │            │            │
            │            │         ❌ Fail
            │            │            │
      ⑤ Cancel ◀──  ④ Refund  ◀──  Compensate
```

### Code Example — Orchestrator (C#)

```csharp
public class PlaceOrderSaga
{
    private readonly IOrderService _orders;
    private readonly IPaymentService _payments;
    private readonly IInventoryService _inventory;

    public PlaceOrderSaga(
        IOrderService orders, IPaymentService payments, IInventoryService inventory)
    {
        _orders = orders;
        _payments = payments;
        _inventory = inventory;
    }

    public async Task<SagaResult> Execute(PlaceOrderCommand cmd)
    {
        Guid? orderId = null;
        string? paymentId = null;

        try
        {
            // Step 1: Create order
            orderId = await _orders.CreateOrder(cmd.CustomerId, cmd.Amount);

            // Step 2: Charge payment
            paymentId = await _payments.Charge(cmd.CustomerId, cmd.Amount);

            // Step 3: Reserve inventory
            await _inventory.Reserve(orderId.Value, cmd.Items);

            return SagaResult.Success(orderId.Value);
        }
        catch (Exception ex)
        {
            // Compensating actions — reverse in opposite order
            if (paymentId != null)
                await _payments.Refund(paymentId);

            if (orderId != null)
                await _orders.Cancel(orderId.Value, $"Saga failed: {ex.Message}");

            return SagaResult.Failed(ex.Message);
        }
    }
}
```

### Choreography vs Orchestration

| Aspect | Choreography | Orchestration |
|--------|-------------|---------------|
| Coupling | Loose — services react to events | Central coordinator knows all steps |
| Complexity | Hard to trace across services | Easy to understand flow |
| Single point of failure | None | Orchestrator |
| Best for | Simple sagas (2-3 steps) | Complex sagas (4+ steps) |

---

## 9. Solution 6: Idempotent Consumer + Retry

Not a replacement for the above patterns, but a **critical complement**. Since most solutions provide **at-least-once delivery**, consumers must handle duplicate messages safely.

### Code Example (C#)

```csharp
public class IdempotentOrderHandler
{
    private readonly AppDbContext _db;

    public IdempotentOrderHandler(AppDbContext db) => _db = db;

    public async Task Handle(OrderPlacedEvent @event)
    {
        // Check if already processed using the event's unique ID
        bool alreadyProcessed = await _db.ProcessedEvents
            .AnyAsync(e => e.EventId == @event.EventId);

        if (alreadyProcessed) return; // skip duplicate

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = @event.EventId });
        _db.Orders.Add(new Order
        {
            Id = @event.OrderId,
            Amount = @event.Amount
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
```

### Idempotency Key Strategies

| Strategy | How It Works | Example |
|----------|-------------|---------|
| Event ID | Store processed event IDs, skip duplicates | `ProcessedEvents` table |
| Natural key | Use business key as unique constraint | `UNIQUE(order_id)` — INSERT fails on dup |
| Idempotency token | Client sends a unique token per request | `Idempotency-Key` HTTP header (Stripe) |
| Version/ETag | Conditional update with version check | `UPDATE ... WHERE version = @expected` |

---

## 10. Comparison of All Solutions

| Solution | Consistency | Complexity | Latency | Best For |
|----------|------------|------------|---------|----------|
| **Transactional Outbox** | Strong (local ACID) | Low-Medium | Seconds (polling) | DB + message broker |
| **Outbox + CDC** | Strong (local ACID) | Medium-High | Milliseconds | High-throughput systems |
| **Event Sourcing** | Strong (single write) | High | Low | Event-native architectures |
| **Listen to Yourself** | Eventual | Low | Low | Simple event-driven systems |
| **Saga** | Eventual (compensating) | High | Variable | Cross-service transactions |
| **Idempotent Consumer** | Complement | Low | N/A | Used WITH any above pattern |

---

## 11. Decision Flowchart

```
Need to write to DB + Message Broker?
  │
  ├─ Can you adopt Event Sourcing? ──▶ YES ──▶ Event Sourcing
  │                                     NO         (eliminates the problem)
  │                                      │
  ├─ Need real-time event streaming? ──▶ YES ──▶ Outbox + CDC (Debezium)
  │                                      NO
  │                                      │
  ├─ Simple DB + broker scenario? ──▶ YES ──▶ Transactional Outbox (polling)
  │                                    NO
  │                                     │
  ├─ Multiple services involved? ──▶ YES ──▶ Saga (orchestration or choreography)
  │                                   NO
  │                                    │
  └─ OK with eventual consistency? ──▶ YES ──▶ Listen to Yourself
                                       NO
                                        │
                                   Reconsider requirements.
                                   True distributed ACID is
                                   almost never worth the cost.

ALWAYS add: Idempotent Consumers on the receiving side.
```

---

## 12. Anti-Patterns to Avoid

### ❌ Anti-Pattern 1: Try-Catch-Compensate

```csharp
// DON'T DO THIS — it's still a dual write with a band-aid
try
{
    await _db.SaveChangesAsync();
    await _bus.PublishAsync(event);  // can still fail
}
catch
{
    // "Undo" the DB write? What if THIS fails too?
    await _db.Database.RollbackTransactionAsync();
}
```

The compensating action can also fail, leaving you in an inconsistent state.

### ❌ Anti-Pattern 2: Distributed Transactions (2PC)

```
Coordinator
     │
     ├── PREPARE → DB       ✅
     ├── PREPARE → Kafka    ❌  ← Kafka doesn't support 2PC
     │
     └── ABORT all
```

2PC doesn't work across heterogeneous systems and adds massive latency even when it does.

### ❌ Anti-Pattern 3: Fire-and-Forget Publishing

```csharp
await _db.SaveChangesAsync();
_ = _bus.PublishAsync(event);  // fire and forget — no await
// If this fails silently, you'll never know.
```

### ❌ Anti-Pattern 4: Manual Reconciliation Jobs

```
"We'll run a nightly job to find DB records without matching events and re-publish them."
```

This is a symptom of an unsolved dual write problem, not a solution. It introduces hours of inconsistency and is error-prone.

---

## 13. Real-World Usage

| Company / System | Solution | Details |
|------------------|----------|---------|
| **Debezium** | Outbox + CDC | Built-in outbox event router for Kafka Connect |
| **Stripe** | Idempotency keys | `Idempotency-Key` header on all write APIs |
| **EventStoreDB** | Event Sourcing | Purpose-built event store database |
| **Uber** | Transactional Outbox | Cadence/Temporal workflows with outbox |
| **Netflix** | CDC | Database changes streamed via custom CDC pipeline |
| **Shopify** | Transactional Outbox | Event publishing via outbox in Rails monolith |
| **Axon Framework** | Event Sourcing + Saga | Java/C# framework with built-in saga orchestration |
| **MassTransit (.NET)** | Transactional Outbox | Built-in EF Core outbox with `AddMassTransit` |

---

## 14. System Design Interview Tips

1. **Name the problem explicitly** — "This is a dual write problem because we're writing to two systems without atomicity"
2. **Default to Transactional Outbox** — it's the safest, most practical answer for most scenarios
3. **Mention CDC as an upgrade** — "If we need lower latency, we can replace polling with Debezium CDC"
4. **Always mention idempotency** — "Consumers must be idempotent since we guarantee at-least-once delivery"
5. **Know when to use Sagas** — when the problem spans multiple services, not just DB + broker
6. **Draw the failure scenarios** — show what happens when each component fails
7. **Avoid 2PC** — explicitly state why distributed transactions are impractical here

### Quick Reference

```
Dual Write = writing to 2+ systems without atomicity

Core Solutions:
  1. Transactional Outbox  → write event to DB, relay to broker
  2. CDC (Debezium)        → stream DB log to broker
  3. Event Sourcing        → single write to event store
  4. Listen to Yourself    → publish first, consume own event
  5. Saga                  → chain of local transactions + compensations

Golden Rule: ONE authoritative write, then propagate.
```
