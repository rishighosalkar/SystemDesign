# Transaction Patterns: 2-Phase Locking, 3-Phase Commit & Saga Pattern

## 1. Two-Phase Locking (2PL)

2PL is a concurrency control protocol that guarantees **serializability** — the gold standard of transaction isolation. It governs how transactions acquire and release locks on data items.

### Core Rule

A transaction's lifecycle is split into two phases:

- **Phase 1 — Growing Phase:** Transaction can *acquire* locks but cannot release any.
- **Phase 2 — Shrinking Phase:** Transaction can *release* locks but cannot acquire any.

Once a transaction releases its first lock, it can never acquire another.

### Example

```
Transaction T1: Transfer $100 from Account A → Account B

Timeline:
─────────────────────────────────────────────────────
  GROWING PHASE              │  SHRINKING PHASE
─────────────────────────────│───────────────────────
  Lock(A)                    │  Unlock(A)
  Lock(B)                    │  Unlock(B)
  Read(A)                    │
  A = A - 100                │
  Write(A)                   │
  Read(B)                    │
  B = B + 100                │
  Write(B)                   │
─────────────────────────────│───────────────────────
         ▲ Lock Point (all locks held)
```

### Variants

| Variant | Description | Deadlock-Free? | Cascading Abort? |
|---|---|---|---|
| Basic 2PL | Locks released anytime in shrinking phase | No | Possible |
| Strict 2PL | All *exclusive* locks held until commit/abort | No | Prevented |
| Rigorous 2PL | *All* locks (shared + exclusive) held until commit/abort | No | Prevented |
| Conservative 2PL | All locks acquired *before* transaction starts | Yes | Prevented |

### Strict 2PL — Most Common in Practice

```
T1:                          T2:
Lock-X(A)
Read(A)
A = A - 100
Write(A)
Lock-X(B)                   Lock-X(A)  ← BLOCKED (T1 holds A)
Read(B)                         |
B = B + 100                     |  waits...
Write(B)                        |
COMMIT                          |
Unlock(A) ──────────────────→ Lock-X(A) granted
Unlock(B)                    Read(A)   ← sees committed value
                             ...
```

### Problem: Deadlocks

```
T1: Lock(A), then wants Lock(B)
T2: Lock(B), then wants Lock(A)

    T1 ──waits──→ T2
    T2 ──waits──→ T1    ← circular wait = DEADLOCK
```

Resolution strategies: timeout, wait-die, wound-wait, or deadlock detection (waits-for graph).

### Pros & Cons

- ✅ Guarantees serializability
- ✅ Well-understood, battle-tested (used in PostgreSQL, MySQL InnoDB, SQL Server)
- ❌ Susceptible to deadlocks (except conservative variant)
- ❌ Reduced concurrency — long-running transactions block others
- ❌ Not suitable for distributed systems (locks don't span microservices well)

---

## 2. Three-Phase Commit (3PC)

3PC adds a **pre-commit phase** between the voting and commit phases of 2PC to solve the **blocking problem** — where participants are stuck waiting if the coordinator crashes.

### Phases

```
  Coordinator                    Participants
      │                              │
      │──── 1. CAN-COMMIT? ────────→│  Phase 1: Voting
      │←─── YES / NO ───────────────│
      │                              │
      │──── 2. PRE-COMMIT ─────────→│  Phase 2: Pre-Commit
      │←─── ACK ─────────────────── │  (participants know decision)
      │                              │
      │──── 3. DO-COMMIT ──────────→│  Phase 3: Commit
      │←─── DONE ───────────────────│
      │                              │
```

### Example: Distributed Order System

```
Scenario: Place order across 3 services

Coordinator: Order Service
Participants: Inventory, Payment, Shipping

Phase 1 — CAN-COMMIT?
  Coordinator → Inventory: "Can you reserve 5 units of SKU-42?"
  Coordinator → Payment:   "Can you authorize $250 on card ending 1234?"
  Coordinator → Shipping:  "Can you schedule delivery to ZIP 98101?"

  Inventory → YES
  Payment   → YES
  Shipping  → YES

Phase 2 — PRE-COMMIT
  Coordinator → All: "Everyone agreed. Prepare to commit."
  All → ACK

  ★ KEY INSIGHT: Now every participant KNOWS the global decision is COMMIT.
    If coordinator crashes here, participants can independently decide to COMMIT
    after a timeout (unlike 2PC where they'd be stuck).

Phase 3 — DO-COMMIT
  Coordinator → All: "Commit now."
  All: execute and confirm.
```

### 3PC vs 2PC Comparison

```
2PC Failure Scenario:
  Coordinator sends COMMIT to Participant A, then CRASHES.
  Participant B never gets the message.
  B is BLOCKED — doesn't know whether to commit or abort.

3PC Improvement:
  After PRE-COMMIT, all participants know the decision.
  If coordinator crashes after PRE-COMMIT:
    → Participants timeout and COMMIT (safe — everyone voted YES)
  If coordinator crashes before PRE-COMMIT:
    → Participants timeout and ABORT (safe — no one prepared)
```

| Property | 2PC | 3PC |
|---|---|---|
| Blocking on coordinator failure | Yes | No |
| Message complexity | 2 round-trips | 3 round-trips |
| Network partition safe | No | No (can cause inconsistency) |
| Latency | Lower | Higher |
| Used in practice | Widely | Rarely (network partitions are real) |

### Why 3PC Is Rarely Used

In the presence of **network partitions**, 3PC can lead to split-brain:

```
  [Partition A]              [Partition B]
  Participants got           Participants did NOT get
  PRE-COMMIT → COMMIT       PRE-COMMIT → ABORT

  Result: INCONSISTENCY — some committed, some aborted.
```

This is why the industry moved toward **Saga patterns** for distributed transactions.

---

## 3. Saga Pattern

The Saga pattern manages distributed transactions **without locks** by breaking a long-lived transaction into a sequence of **local transactions**, each with a **compensating action** to undo its effects on failure.

### Core Concept

```
Saga = T1 → T2 → T3 → ... → Tn   (happy path)

If Ti fails:
  Execute C(i-1) → C(i-2) → ... → C1  (compensating transactions in reverse)
```

Each Ti commits immediately (no global lock), achieving **eventual consistency** instead of ACID.

### Two Coordination Strategies

#### A. Choreography (Event-Driven)

Each service listens for events and reacts — no central coordinator.

```
Order         Inventory        Payment         Shipping
  │                │               │               │
  │─OrderCreated──→│               │               │
  │                │─Reserved─────→│               │
  │                │               │─Charged──────→│
  │                │               │               │─Shipped──→ ✅
  │                │               │               │
  │  ON FAILURE (Payment fails):   │               │
  │                │               │─ChargeFailed─→│
  │                │←─Release──────│               │
  │←─OrderFailed───│               │               │
```

- ✅ Simple, decoupled, no single point of failure
- ❌ Hard to track overall flow, difficult to debug, risk of cyclic dependencies

#### B. Orchestration (Central Coordinator)

A saga orchestrator tells each participant what to do.

```
         Saga Orchestrator
              │
    ┌─────────┼──────────┐
    ▼         ▼          ▼
 Inventory  Payment   Shipping
```

```
Orchestrator:
  Step 1: Call Inventory.reserve(orderId, items)  → Success
  Step 2: Call Payment.charge(orderId, $250)      → Success
  Step 3: Call Shipping.schedule(orderId, address) → FAILURE ❌

  Compensate:
  Step 2C: Call Payment.refund(orderId, $250)     → Done
  Step 1C: Call Inventory.release(orderId, items) → Done

  Mark saga as FAILED.
```

- ✅ Clear flow, easy to debug, centralized state management
- ❌ Orchestrator is a single point of failure (mitigate with persistence + retries)

### Detailed Example: E-Commerce Order Saga (Orchestration)

```
┌──────────────────────────────────────────────────────────┐
│                    SAGA: PlaceOrder                       │
├──────────┬───────────────────┬────────────────────────────┤
│  Step    │  Forward Action   │  Compensating Action       │
├──────────┼───────────────────┼────────────────────────────┤
│  T1      │  Create Order     │  C1: Cancel Order          │
│  T2      │  Reserve Stock    │  C2: Release Stock         │
│  T3      │  Process Payment  │  C3: Refund Payment        │
│  T4      │  Ship Order       │  C4: Cancel Shipment       │
│  T5      │  Send Confirmation│  C5: Send Cancellation     │
└──────────┴───────────────────┴────────────────────────────┘

Happy Path:  T1 → T2 → T3 → T4 → T5 ✅

Failure at T3 (payment declined):
  T1 ✅ → T2 ✅ → T3 ❌ → C2 (release stock) → C1 (cancel order)
```

### Saga State Machine

```
                    ┌─────────┐
                    │ STARTED │
                    └────┬────┘
                         │ T1 success
                    ┌────▼────┐
                    │RESERVING│
                    └────┬────┘
                    ╱         ╲
              T2 success    T2 fail
                 ╱               ╲
          ┌─────▼─────┐    ┌─────▼──────┐
          │  CHARGING  │    │COMPENSATING│
          └─────┬──────┘    └─────┬──────┘
           ╱        ╲             │ C1
     T3 success   T3 fail        │
        ╱              ╲    ┌─────▼──────┐
  ┌────▼────┐    ┌──────▼───┤  FAILED    │
  │SHIPPING │    │COMPENSATE│└───────────┘
  └────┬────┘    └──────────┘
       │ T4 success
  ┌────▼──────┐
  │ COMPLETED │
  └───────────┘
```

### Semantic Lock Pattern (Handling Dirty Reads)

Since sagas don't hold locks, intermediate states are visible. Use a status flag:

```
Order Table:
┌────────┬────────┬──────────────────┐
│ ID     │ Amount │ Status           │
├────────┼────────┼──────────────────┤
│ ORD-1  │ $250   │ APPROVAL_PENDING │  ← T1 sets this
│ ORD-1  │ $250   │ APPROVED         │  ← T5 sets this (saga complete)
│ ORD-1  │ $250   │ REJECTED         │  ← Compensation sets this
└────────┴────────┴──────────────────┘

Other services check: if status == APPROVAL_PENDING → treat as uncommitted
```

---

## 4. Comparison Matrix

| Criteria | 2PL | 3PC | Saga |
|---|---|---|---|
| Scope | Single DB | Distributed DBs | Distributed services |
| Consistency | Strong (ACID) | Strong (ACID) | Eventual |
| Isolation | Serializable | Serializable | None (requires countermeasures) |
| Blocking | Yes (lock contention) | Non-blocking (in theory) | Non-blocking |
| Failure handling | Rollback | Timeout-based recovery | Compensating transactions |
| Performance | Low concurrency | High latency (3 rounds) | High throughput |
| Network partition tolerance | N/A (single node) | Poor | Good |
| Real-world usage | RDBMS internals | Rare | Microservices (very common) |

---

## 5. When to Use What

- **2PL** → Single database, need strong consistency (banking ledger within one DB)
- **3PC** → Distributed databases in a reliable network (rarely chosen today)
- **Saga** → Microservices, cross-service business transactions, eventual consistency is acceptable (e-commerce, travel booking, food delivery)

> **Industry trend:** Monoliths use 2PL internally, distributed systems use Sagas. 3PC remains mostly academic — real systems prefer Saga + idempotency + outbox pattern for distributed transaction management.
