# State Design Pattern - Comprehensive Notes (SSE/SDE-2 Interview Prep)

---

## 1. What is the State Design Pattern?

The State Design Pattern is a **Behavioral Design Pattern** that allows an object to **change its behavior when its internal state changes**. The object will appear to change its class.

> **One-liner for interviews:** "State pattern lets an object alter its behavior when its internal state changes — it looks like the object changed its class."

---

## 2. The Problem It Solves

### Without State Pattern (The Mess):
```csharp
public void ProcessOrder()
{
    if (state == "Created")
    {
        // do created logic
        state = "Paid";
    }
    else if (state == "Paid")
    {
        // do paid logic
        state = "Shipped";
    }
    else if (state == "Shipped")
    {
        // do shipped logic
    }
    // ... grows endlessly with new states
}
```

**Problems:**
- Violates **Open/Closed Principle** — adding a new state means modifying existing code
- Violates **Single Responsibility Principle** — one class handles all state logic
- Complex conditionals become unreadable and error-prone
- State transitions are scattered and hard to trace

### With State Pattern:
Each state is its own class. Adding a new state = adding a new class. **Zero modification** to existing code.

---

## 3. UML / Structure

```
┌─────────────────────┐         ┌─────────────────────────┐
│   OrderProcessing   │         │    <<interface>>         │
│   (Context)         │────────▶│    IOrderState           │
├─────────────────────┤         ├─────────────────────────┤
│ - _orderState       │         │ + NextState(context)     │
│ + SetState(state)   │         │ + CancelOrder(context)   │
│ + NextState()       │         └─────────────────────────┘
│ + CancelOrder()     │                    ▲
└─────────────────────┘                    │
                              ┌────────────┼────────────────┐
                              │            │                 │
                    ┌─────────┴──┐  ┌──────┴─────┐  ┌───────┴─────┐
                    │ CreateState │  │ PaidState  │  │ShippedState │  ...
                    └────────────┘  └────────────┘  └─────────────┘
```

### Participants:
| Role | Class | Responsibility |
|------|-------|----------------|
| **Context** | `OrderProcessing` | Maintains current state, delegates behavior |
| **State Interface** | `IOrderState` | Defines contract for all states |
| **Concrete States** | `CreateState`, `PaidState`, etc. | Implement behavior for that specific state |

---

## 4. How It Works (Flow)

```
Client calls context.NextState()
    → Context delegates to _orderState.NextState(this)
        → Concrete state executes its logic
        → Concrete state calls context.SetState(new NextConcreteState())
            → Context now holds the new state
    → Next call to context.NextState() goes to the NEW state's logic
```

**Key Insight:** The state objects themselves decide what the next state should be. The context doesn't know or care about transition logic.

---

## 5. Project Structure (This Repo)

```
StateDesignPattern/
├── Context/
│   └── OrderProcessing.cs        ← Holds current state, delegates calls
├── States/
│   ├── IOrderState.cs            ← State interface (contract)
│   ├── CreateState.cs            ← Order just created
│   ├── PaidState.cs              ← Payment received
│   ├── ShippedState.cs           ← Order shipped
│   ├── DeliveredState.cs         ← Terminal state (success)
│   └── CancelledState.cs         ← Terminal state (cancelled)
├── Docs/
│   └── NOTES.md                  ← You are here
└── Program.cs                     ← Demo / Client code
```

---

## 6. State Transition Diagram

```
                    ┌──────────────────┐
                    │   CreateState    │
                    └────────┬─────────┘
                             │ NextState()
                             ▼
                    ┌──────────────────┐
         Cancel ◄───│    PaidState     │
                    └────────┬─────────┘
                             │ NextState()
                             ▼
                    ┌──────────────────┐
                    │  ShippedState    │ ← Cannot cancel from here
                    └────────┬─────────┘
                             │ NextState()
                             ▼
                    ┌──────────────────┐
                    │ DeliveredState   │ ← Terminal
                    └──────────────────┘

                    ┌──────────────────┐
                    │ CancelledState   │ ← Terminal (from Create/Paid)
                    └──────────────────┘
```

---

## 7. SOLID Principles Satisfied

| Principle | How State Pattern Satisfies It |
|-----------|-------------------------------|
| **S** - Single Responsibility | Each state class handles only its own behavior |
| **O** - Open/Closed | Add new states without modifying existing classes |
| **L** - Liskov Substitution | All concrete states are interchangeable via IOrderState |
| **I** - Interface Segregation | State interface is focused (only state-related methods) |
| **D** - Dependency Inversion | Context depends on abstraction (IOrderState), not concrete states |

---

## 8. When to Use State Pattern

✅ Use when:
- Object behavior depends on its state and changes at runtime
- You have large conditional blocks (if/else, switch) based on state
- State transitions are complex and you want them explicit
- You want to avoid "state explosion" in a single class

❌ Don't use when:
- Only 2-3 simple states with trivial logic (overkill)
- States don't have significantly different behavior
- State transitions are linear and never change

---

## 9. State vs Strategy Pattern (Common Interview Question!)

| Aspect | State Pattern | Strategy Pattern |
|--------|--------------|-----------------|
| **Intent** | Object changes behavior as state changes | Client picks an algorithm at runtime |
| **Who triggers change?** | State objects themselves transition | Client/external code sets the strategy |
| **Awareness** | States know about each other (for transitions) | Strategies are independent, unaware of each other |
| **Lifecycle** | State changes multiple times during object lifetime | Strategy typically set once or rarely changed |
| **Analogy** | Traffic light changing colors automatically | Choosing sort algorithm (quick/merge/bubble) |

> **Interview tip:** "State is about *what you are*, Strategy is about *how you do it*."

---

## 10. Real-World Examples

| Domain | States |
|--------|--------|
| **E-commerce Order** | Created → Paid → Shipped → Delivered / Cancelled |
| **Vending Machine** | Idle → HasMoney → Dispensing → OutOfStock |
| **TCP Connection** | Listening → Established → Closed |
| **Document Workflow** | Draft → Review → Approved → Published |
| **ATM Machine** | Idle → CardInserted → PinEntered → Transaction → Ejecting |
| **Traffic Light** | Red → Green → Yellow → Red |

---

## 11. Common Interview Questions & Answers

### Q1: "How does State pattern differ from having an enum + switch?"
**A:** Enum + switch violates OCP. Every new state requires modifying the switch. State pattern encapsulates each state's behavior in its own class — adding a state means adding a class, not touching existing code.

### Q2: "Where are state transitions defined?"
**A:** In the concrete state classes themselves. Each state knows what the next valid state is. This keeps transition logic co-located with state behavior.

### Q3: "Can a state hold data?"
**A:** Yes. Concrete states can have fields. For example, a `RetryState` might hold a retry count. When transitioning, you can pass data through the context.

### Q4: "How do you handle invalid transitions?"
**A:** Each state implements all interface methods. For invalid actions, the state either:
- Throws an exception (strict)
- Logs a message and does nothing (lenient)
- Returns a result indicating failure

### Q5: "What's the downside of State pattern?"
**A:**
- Increased number of classes (one per state)
- States are tightly coupled to each other (they reference next states)
- Can be overkill for simple state machines

### Q6: "How would you unit test this?"
**A:**
- Test each state class independently
- Verify that calling `NextState()` on a state sets the correct next state on the context
- Verify terminal states don't transition
- Verify invalid operations are handled gracefully

---

## 12. Complexity Analysis

| Metric | Value |
|--------|-------|
| **Adding a new state** | O(1) — just add a new class |
| **Adding a new action** | O(n) — must add method to interface + all states |
| **Number of classes** | n + 2 (n states + 1 interface + 1 context) |

**Trade-off:** State pattern optimizes for *adding new states* at the cost of *adding new actions*. If you frequently add new actions, consider a different approach.

---

## 13. Key Takeaways for Interview

1. **State pattern = object behavior changes with internal state**
2. **Context delegates to current state object** — doesn't use conditionals
3. **States transition themselves** — they call `context.SetState(new NextState())`
4. **Satisfies OCP** — new states don't require modifying existing code
5. **Different from Strategy** — states know about each other; strategies don't
6. **Best for:** complex state machines with distinct behavior per state
7. **Watch out for:** class explosion if you have too many states with trivial differences

---

## 14. Code Walkthrough (Quick Reference)

### Interface (Contract):
```csharp
public interface IOrderState
{
    void NextState(OrderProcessing orderProcessing);
    void CancelOrder(OrderProcessing orderProcessing);
}
```

### Context (State Holder):
```csharp
public class OrderProcessing
{
    private IOrderState _orderState;

    public OrderProcessing() => _orderState = new CreateState();
    public void SetState(IOrderState state) => _orderState = state;
    public void NextState() => _orderState.NextState(this);
    public void CancelOrder() => _orderState.CancelOrder(this);
}
```

### Concrete State (Example):
```csharp
public class PaidState : IOrderState
{
    public void NextState(OrderProcessing orderProcessing)
    {
        Console.WriteLine("Payment done, moving to Shipped state.");
        orderProcessing.SetState(new ShippedState());  // Self-transition
    }

    public void CancelOrder(OrderProcessing orderProcessing)
    {
        Console.WriteLine("Order cancelled. Refund initiated.");
        orderProcessing.SetState(new CancelledState());
    }
}
```

### Client Usage:
```csharp
var order = new OrderProcessing();  // Starts in CreateState
order.NextState();   // Created → Paid
order.NextState();   // Paid → Shipped
order.NextState();   // Shipped → Delivered
```

---

## 15. Follow-up: How to Extend This

If an interviewer asks "how would you add X?":

| Extension | Approach |
|-----------|----------|
| Add `ReturnState` | Create new class implementing `IOrderState`, transition from `DeliveredState` |
| Add logging | Decorator on `IOrderState` or add logging in `SetState()` |
| Persist state | Serialize state name in context, use factory to reconstruct |
| Add event on transition | Publish event in `SetState()` method (Observer pattern combo) |
| Async transitions | Make interface methods return `Task`, use `async/await` |

---

*Good luck with your interview! Remember: design patterns are tools, not rules. Know when to use them AND when not to.* 🚀
