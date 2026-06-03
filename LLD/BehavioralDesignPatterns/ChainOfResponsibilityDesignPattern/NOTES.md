# Chain of Responsibility (CoR) Design Pattern — Comprehensive Notes

## 1. What is Chain of Responsibility?

A **behavioral design pattern** that lets you pass a request along a chain of handlers. Each handler decides either to process the request or pass it to the next handler in the chain.

**Core Idea:** Decouple the sender of a request from its receivers by giving more than one object a chance to handle the request.

---

## 2. Key Components

| Component | Role |
|---|---|
| **Handler (Abstract)** | Defines the interface, holds a reference to the next handler |
| **Concrete Handlers** | Implement the handling logic, decide to process or forward |
| **Client** | Initiates the request to the first handler in the chain |

---

## 3. Two Variants of CoR (Both Implemented Here)

### Variant A — "Pass-through" Chain (Every handler executes)
Each handler processes the request **and** forwards it to the next handler. All handlers in the chain get invoked.

### Variant B — "Short-circuit" Chain (Only one handler executes)
Each handler checks if it **can** handle the request. If yes, it processes and stops. If no, it forwards to the next handler.

---

## 4. Implementation Examples in This Solution

### Example 1: Logger — Variant B (Short-circuit with broadcast)

```
LogProcessor (abstract)
├── InfoLog
└── ErrorLog
```

**Abstract Handler — LogProcessor:**
```csharp
public abstract class LogProcessor
{
    protected LogProcessor nextLogProcessor;  // link to next in chain
    protected LogLevel level;                 // level this handler cares about

    public void Log(LogLevel logLevel, string message)
    {
        if (level == logLevel)       // Can I handle this?
            Message(message);        // Yes → process it
        if (nextLogProcessor != null)
            nextLogProcessor.Log(logLevel, message);  // Always forward
    }

    public abstract void Message(string message);
}
```

**Key observations:**
- Chain is built via **constructor injection**: `new InfoLog(LogLevel.Info, new ErrorLog(LogLevel.Error, null))`
- Each handler checks if the log level matches its own level
- It **always forwards** to the next handler (broadcast behavior) — every handler gets a chance
- This is a hybrid: it checks a condition but doesn't short-circuit

**Chain wiring in Program.cs:**
```csharp
LogProcessor log = new InfoLog(LogLevel.Info, new ErrorLog(LogLevel.Error, null));
log.Log(LogLevel.Info, "test");
// InfoLog handles it (level matches), then forwards to ErrorLog (level doesn't match, skips)
```

---

### Example 2: Approval Workflow — Variant B (True Short-circuit)

```
Approver (abstract)
├── TeamLeadApprover    (amount <= 10,000)
├── ManagerApprover     (10,000 < amount <= 200,000)
└── DirectorApprover    (amount > 200,000)
```

**Abstract Handler — Approver:**
```csharp
public abstract class Approver
{
    protected Approver _nextApprover;

    protected Approver(Approver approver)
    {
        _nextApprover = approver;  // chain link set via constructor
    }

    public abstract void ProvideApproval(int amount);
}
```

**Concrete Handler — TeamLeadApprover:**
```csharp
public override void ProvideApproval(int amount)
{
    if (amount <= 10000)
    {
        Console.WriteLine($"Amount {amount} received for approval from Team Lead");
        return;  // HANDLED — chain stops here
    }
    _nextApprover?.ProvideApproval(amount);  // Can't handle → pass to next
}
```

**Key observations:**
- Chain is built **bottom-up**: Director → Manager(Director) → TeamLead(Manager)
- Each handler has a **threshold**. If the amount fits, it handles and **returns** (short-circuits)
- If it can't handle, it delegates to `_nextApprover`
- `?.` null-conditional ensures the chain terminates safely if no handler can process

**Chain wiring in Program.cs:**
```csharp
Approver director = new DirectorApprover(null);           // end of chain
Approver manager  = new ManagerApprover(director);
Approver teamLead = new TeamLeadApprover(manager);

teamLead.ProvideApproval(2000000);
// TeamLead can't handle (2M > 10K) → Manager can't handle (2M > 200K) → Director handles
```

**Flow for amount = 2,000,000:**
```
TeamLead → (2M > 10K, skip) → Manager → (2M > 200K, skip) → Director → (2M > 200K, HANDLE)
```

---

### Example 3: Middleware Pipeline — Variant A (Pass-through)

```
Middleware (abstract)
├── LoggingMiddleware
├── AuthenticationMiddleware
└── AuthorizationMiddleware
```

**Abstract Handler — Middleware:**
```csharp
public abstract class Middleware
{
    private Middleware _nexMiddleware;

    public Middleware Next(Middleware middleware)
    {
        _nexMiddleware = middleware;
        return _nexMiddleware;  // returns the added middleware for fluent chaining
    }

    public void Invoke()
    {
        Handle();                    // Process this step
        _nexMiddleware?.Invoke();    // Then invoke next
    }

    public abstract void Handle();
}
```

**Key observations:**
- Chain is built via **fluent method chaining**: `logging.Next(auth).Next(authorization)`
- `Next()` returns the middleware it just linked, enabling the fluent API
- `Invoke()` always calls `Handle()` first, then forwards — **every handler executes**
- This mirrors real-world HTTP middleware pipelines (ASP.NET Core, Express.js, etc.)

**Chain wiring in Program.cs:**
```csharp
Middleware logging = new LoggingMiddleware();
Middleware auth    = new AuthenticationMiddleware();
var authorization  = new AuthorizationMiddleware();

logging.Next(auth).Next(authorization);  // logging → auth → authorization
logging.Invoke();
// Output: Logging middleware → Authenication Middleware → Authorization Middleware
```

---

## 5. Comparison of the Three Implementations

| Aspect | Logger | Approval Workflow | Middleware |
|---|---|---|---|
| **Chain construction** | Constructor injection | Constructor injection | Fluent method `Next()` |
| **Forwarding logic** | In base class | In each concrete class | In base class `Invoke()` |
| **Short-circuits?** | No (broadcasts) | Yes (returns on match) | No (all execute) |
| **Handler decides to process?** | Yes (level check) | Yes (amount threshold) | No (always processes) |
| **Real-world analogy** | Log framework levels | Expense approval hierarchy | HTTP request pipeline |

---

## 6. UML Class Diagram (Conceptual)

```
┌──────────────────────┐
│   <<abstract>>        │
│   Handler             │
│───────────────────────│
│ - nextHandler         │
│───────────────────────│
│ + setNext(handler)    │
│ + handle(request)     │
└──────────┬────────────┘
           │ extends
    ┌──────┴──────┐
    │             │
┌───▼───┐   ┌────▼────┐
│Handler │   │Handler  │
│   A    │   │   B     │
└────────┘   └─────────┘
```

---

## 7. When to Use CoR

- Multiple objects can handle a request, and the handler isn't known in advance
- You want to issue a request to one of several objects without specifying the receiver explicitly
- The set of handlers should be specified dynamically
- You want to decouple senders and receivers

**Common real-world uses:**
- HTTP middleware pipelines (ASP.NET Core, Express.js)
- Event bubbling in UI frameworks
- Logging frameworks (log4j, Serilog)
- Exception handling chains
- Approval/escalation workflows
- Input validation chains

---

## 8. Advantages & Disadvantages

**Advantages:**
- **Single Responsibility Principle** — each handler focuses on one concern
- **Open/Closed Principle** — add new handlers without modifying existing ones
- **Loose coupling** — sender doesn't know which handler will process the request
- **Flexible ordering** — chain order can be changed at runtime

**Disadvantages:**
- Request might go **unhandled** if no handler in the chain processes it
- **Debugging complexity** — harder to trace which handler processed a request
- **Performance** — long chains add overhead from traversal

---

## 9. SOLID Principles Applied

| Principle | How it's applied |
|---|---|
| **S** — Single Responsibility | Each handler has one job (e.g., TeamLead only handles ≤10K) |
| **O** — Open/Closed | New handlers (e.g., `VPApprover`) can be added without changing existing code |
| **L** — Liskov Substitution | All concrete handlers are substitutable for the abstract handler |
| **D** — Dependency Inversion | Client depends on the abstract `Approver`/`Middleware`, not concrete classes |

---

## 10. Key Design Decisions in This Solution

1. **Abstract class over interface** — chosen because the base class holds state (`nextHandler`) and provides default forwarding behavior
2. **Null-conditional operator (`?.`)** — cleanly terminates the chain without explicit null checks
3. **Three different chain-building strategies** demonstrated:
   - Constructor injection (Logger, Approval)
   - Fluent API with method chaining (Middleware)
4. **Two forwarding strategies** demonstrated:
   - Base class controls forwarding (Logger, Middleware) — handlers only implement `Handle()`/`Message()`
   - Concrete class controls forwarding (Approval) — each handler decides whether to forward
