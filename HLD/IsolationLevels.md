# Database Isolation Levels

## Core Concept

Isolation is the "I" in ACID. It defines **how and when changes made by one transaction become visible to other concurrent transactions**. Higher isolation = fewer anomalies but lower concurrency. Lower isolation = more concurrency but risk of reading inconsistent data.

```
The Fundamental Trade-off:

  CONSISTENCY ◄──────────────────────────► PERFORMANCE
  (correctness)                            (throughput)

  Serializable ──── Repeatable Read ──── Read Committed ──── Read Uncommitted
  Most strict                                                  Most relaxed
  Slowest                                                      Fastest
```

---

## Read Phenomena (Anomalies)

Before understanding isolation levels, you must understand the problems they prevent.

### 1. Dirty Read

Reading data written by a transaction that **has not yet committed**. If that transaction rolls back, you read data that never existed.

```
T1 (Transfer):                    T2 (Balance Check):
─────────────────────────────────────────────────────
BEGIN
UPDATE accounts
  SET balance = balance - 500
  WHERE id = 'A';
  (A: 1000 → 500)
                                  BEGIN
                                  SELECT balance FROM accounts
                                    WHERE id = 'A';
                                  → Returns 500  ← DIRTY READ
ROLLBACK;
(A is back to 1000)
                                  -- T2 used 500, but real value is 1000
                                  -- Decision made on phantom data
```

```
Real-World Impact:
  - Inventory system shows item out of stock (another txn reserved but rolled back)
  - Dashboard shows revenue that was never actually collected
  - Fraud detection flags a transaction based on a balance that doesn't exist
```

---

### 2. Non-Repeatable Read

Reading the **same row twice** within a transaction and getting **different values** because another committed transaction modified it in between.

```
T1 (Generate Report):             T2 (Update Price):
─────────────────────────────────────────────────────
BEGIN
SELECT price FROM products
  WHERE id = 'P1';
→ Returns $100
                                  BEGIN
                                  UPDATE products
                                    SET price = 150
                                    WHERE id = 'P1';
                                  COMMIT;

SELECT price FROM products
  WHERE id = 'P1';
→ Returns $150  ← NON-REPEATABLE READ

-- Same query, same txn, different result
COMMIT;
```

```
Real-World Impact:
  - Report header says total = $100, but line items computed with $150
  - Booking system: user sees price $100, clicks pay, charged $150
  - Audit trail inconsistency within the same report generation
```

---

### 3. Phantom Read

Re-executing a **range query** and getting a **different set of rows** because another committed transaction inserted or deleted rows that match the query's WHERE clause.

```
T1 (Count Employees):             T2 (Hire New Employee):
─────────────────────────────────────────────────────
BEGIN
SELECT COUNT(*) FROM employees
  WHERE dept = 'Engineering';
→ Returns 10
                                  BEGIN
                                  INSERT INTO employees
                                    (name, dept)
                                    VALUES ('Alice', 'Engineering');
                                  COMMIT;

SELECT COUNT(*) FROM employees
  WHERE dept = 'Engineering';
→ Returns 11  ← PHANTOM READ

-- A new row "appeared" (phantom) in the same query range
COMMIT;
```

```
Real-World Impact:
  - Pagination breaks: page 1 shows 10 items, re-query shows 11
  - Aggregate mismatch: SUM and COUNT disagree within same report
  - Constraint validation bypassed: "max 10 engineers" check passes, but 11 exist
```

---

### 4. Lost Update

Two transactions read the same value, both modify it, and the **second write overwrites the first** — the first update is silently lost.

```
T1 (Add to Cart):                 T2 (Add to Cart):
─────────────────────────────────────────────────────
BEGIN                             BEGIN
SELECT quantity FROM cart
  WHERE item = 'X';
→ Returns 5
                                  SELECT quantity FROM cart
                                    WHERE item = 'X';
                                  → Returns 5

UPDATE cart SET quantity = 6      
  WHERE item = 'X';
                                  UPDATE cart SET quantity = 6
                                    WHERE item = 'X';
COMMIT;
                                  COMMIT;

-- Expected: 7 (5+1+1), Actual: 6 — T1's update is LOST
```

---

### 5. Write Skew

Two transactions read the **same data**, make decisions based on it, and write to **different rows** — individually valid, but collectively violating a constraint.

```
Constraint: At least 1 doctor must be on-call at all times.
Currently on-call: Dr. A and Dr. B

T1 (Dr. A wants off):            T2 (Dr. B wants off):
─────────────────────────────────────────────────────
BEGIN                             BEGIN
SELECT COUNT(*) FROM doctors
  WHERE on_call = true;
→ Returns 2 (safe to remove one)
                                  SELECT COUNT(*) FROM doctors
                                    WHERE on_call = true;
                                  → Returns 2 (safe to remove one)

UPDATE doctors SET on_call = false
  WHERE name = 'Dr. A';
                                  UPDATE doctors SET on_call = false
                                    WHERE name = 'Dr. B';
COMMIT;
                                  COMMIT;

-- Both saw 2 on-call, both removed themselves
-- Result: 0 doctors on-call — CONSTRAINT VIOLATED
```

---

## Anomaly Summary

```
┌──────────────────────┬──────────────────────────────────────────────┐
│ Anomaly              │ What Goes Wrong                              │
├──────────────────────┼──────────────────────────────────────────────┤
│ Dirty Read           │ Read uncommitted data (may be rolled back)   │
│ Non-Repeatable Read  │ Same row, different value on re-read         │
│ Phantom Read         │ Same range query, different row set          │
│ Lost Update          │ Concurrent writes, one silently overwritten  │
│ Write Skew           │ Valid individual writes, invalid combined    │
└──────────────────────┴──────────────────────────────────────────────┘
```

---

## The Four SQL Standard Isolation Levels

### 1. Read Uncommitted (Lowest)

Transactions can see **uncommitted changes** from other transactions. Essentially no isolation.

```
Behavior:
  - No read locks acquired
  - No write locks checked before reading
  - You see the raw, in-progress state of other transactions

Allowed Anomalies:
  ✗ Dirty Read
  ✗ Non-Repeatable Read
  ✗ Phantom Read
  ✗ Lost Update
```

```sql
-- Session 1
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
BEGIN;
UPDATE accounts SET balance = 0 WHERE id = 1;  -- not committed yet

-- Session 2
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
BEGIN;
SELECT balance FROM accounts WHERE id = 1;
→ Returns 0  (sees uncommitted change)

-- Session 1
ROLLBACK;  -- change is undone, but Session 2 already used it
```

```
Timeline Diagram — Read Uncommitted:
─────────────────────────────────────────────────────────────────────────────
Time ──→   t1       t2       t3       t4       t5       t6       t7
─────────────────────────────────────────────────────────────────────────────

T1:      BEGIN    WRITE                                ROLLBACK
         ║        A=500                                A→1000
         ║        (was 1000)                           (restored)
         ║          │                                    ║
         ║          │                                    ║
T2:      ║        ║ │      BEGIN    READ A   USE 500   ║        COMMIT
         ║        ║ │        ║      → 500 ⚠  (decide)  ║          ║
         ║        ║ │        ║      DIRTY!   based on   ║          ║
         ║        ║ │        ║               bad data   ║          ║
─────────────────────────────────────────────────────────────────────────────
Data:   A=1000   A=500    A=500    A=500    A=500    A=1000    A=1000
        (real)   (dirty)  (dirty)  (dirty)  (dirty)  (real)    (real)
─────────────────────────────────────────────────────────────────────────────
  ⚠ T2 read 500 at t4, but T1 rolled back at t6 → T2 acted on ghost data
```

```
When to Use:
  - Almost never in production
  - Approximate analytics where precision doesn't matter
    (e.g., "roughly how many rows in this table?")
  - Monitoring dashboards that tolerate stale/dirty data
  - SQL Server: WITH (NOLOCK) hint uses this level
```

---

### 2. Read Committed (Default in PostgreSQL, Oracle, SQL Server)

Transactions only see data that has been **committed**. Each SELECT sees a fresh snapshot of committed data at the time of that statement.

```
Behavior:
  - Read locks are acquired and released immediately (per statement)
  - Write locks held until transaction ends
  - Each statement sees the latest committed state

Prevented:
  ✅ Dirty Read

Allowed Anomalies:
  ✗ Non-Repeatable Read
  ✗ Phantom Read
  ✗ Lost Update (in some implementations)
```

```sql
-- Session 1
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN;
UPDATE products SET price = 150 WHERE id = 1;  -- not committed

-- Session 2
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN;
SELECT price FROM products WHERE id = 1;
→ Returns 100  (old committed value — dirty read prevented ✅)

-- Session 1
COMMIT;

-- Session 2
SELECT price FROM products WHERE id = 1;
→ Returns 150  (sees new committed value — non-repeatable read ✗)
```

```
Implementation Approaches:
─────────────────────────────────────────────────────────
1. Lock-Based (SQL Server default)
   - Shared lock acquired for each read, released immediately after read
   - Writers block readers only while writing (short duration)

2. MVCC-Based (PostgreSQL, Oracle)
   - Each statement sees a snapshot of committed data at statement start
   - Readers never block writers, writers never block readers
   - Old row versions kept in undo log / heap
─────────────────────────────────────────────────────────
```

```
Timeline Diagram — Read Committed:
─────────────────────────────────────────────────────────────────────────────
Time ──→   t1       t2       t3       t4       t5       t6       t7
─────────────────────────────────────────────────────────────────────────────

T1:      BEGIN    WRITE                     COMMIT
         ║        price=150                 ║
         ║        (uncommitted)             ║
         ║          │                       ║
         ║          │                       ║
T2:      ║        ║       BEGIN   READ      ║        READ       COMMIT
         ║        ║         ║     price      ║        price        ║
         ║        ║         ║     → 100 ✅   ║        → 150 ⚠     ║
         ║        ║         ║     (old       ║        (new         ║
         ║        ║         ║     committed) ║        committed)   ║
─────────────────────────────────────────────────────────────────────────────
What T2 sees:                     100 ✅              150 ⚠
                                  (correct:           (non-repeatable
                                  no dirty read)       read allowed)
─────────────────────────────────────────────────────────────────────────────
  ✅ Dirty read prevented — T2 sees 100 at t4 (T1 not yet committed)
  ⚠  Non-repeatable read — T2 sees 150 at t6 (T1 committed between reads)
     Same row, same txn, different value
```

```
When to Use:
  - General-purpose OLTP workloads
  - Applications where slight inconsistency within a transaction is acceptable
  - Most web applications (user requests are short-lived)
  - Default for most databases — good balance of safety and performance
```

---

### 3. Repeatable Read (Default in MySQL InnoDB)

Once a transaction reads a row, it sees the **same value for that row** throughout the transaction, even if other transactions modify and commit it.

```
Behavior:
  - Snapshot taken at the START of the transaction (not per statement)
  - All reads within the txn see this consistent snapshot
  - Write locks held until transaction ends

Prevented:
  ✅ Dirty Read
  ✅ Non-Repeatable Read

Allowed Anomalies:
  ✗ Phantom Read (per SQL standard, but MySQL InnoDB prevents this too via gap locks)
```

```sql
-- Session 1
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN;
SELECT price FROM products WHERE id = 1;
→ Returns 100

-- Session 2
BEGIN;
UPDATE products SET price = 200 WHERE id = 1;
COMMIT;

-- Session 1 (same transaction)
SELECT price FROM products WHERE id = 1;
→ Still returns 100  ← Repeatable Read guarantee ✅

COMMIT;
-- Only after commit, a new transaction would see 200
```

```
How It Works — MVCC Snapshot:
─────────────────────────────────────────────────────────
Transaction T1 starts at timestamp t=100

  products table (versioned):
  ┌────┬───────┬────────────┬────────────┐
  │ id │ price │ created_at │ expired_at │
  ├────┼───────┼────────────┼────────────┤
  │  1 │  100  │   t=50     │   t=105    │  ← T1 sees this (visible at t=100)
  │  1 │  200  │   t=105    │   NULL     │  ← T1 does NOT see this (created after t=100)
  └────┴───────┴────────────┴────────────┘

  T1 always reads the version valid at t=100, regardless of new commits.
─────────────────────────────────────────────────────────
```

```
Phantom Read Example (still possible per SQL standard):

T1:                                T2:
BEGIN                              BEGIN
SELECT * FROM orders
  WHERE status = 'pending';
→ Returns 5 rows
                                   INSERT INTO orders (status)
                                     VALUES ('pending');
                                   COMMIT;

SELECT * FROM orders
  WHERE status = 'pending';
→ Returns 6 rows  ← Phantom (new row appeared)

Note: MySQL InnoDB prevents this using next-key locks (gap + record locks).
      PostgreSQL also prevents phantoms at this level via its MVCC implementation.
      So in practice, phantoms are rare at Repeatable Read in modern databases.
```

```
Timeline Diagram — Repeatable Read:
─────────────────────────────────────────────────────────────────────────────
Time ──→   t1       t2       t3       t4       t5       t6       t7
─────────────────────────────────────────────────────────────────────────────
                  ┌─── T1 snapshot taken here (t1) ───────────────────┐
                  │                                                    │
T1:      BEGIN    READ     ║        ║        READ       ║       COMMIT
         ║        price    ║        ║        price       ║         ║
         ║        → 100    ║        ║        → 100 ✅    ║         ║
         ║        ║        ║        ║        (same!)     ║         ║
         ║        ║        ║        ║                    ║         ║
T2:      ║        ║      BEGIN    WRITE     COMMIT       ║         ║
         ║        ║        ║      price=200  ║           ║         ║
         ║        ║        ║        ║        ║           ║         ║
─────────────────────────────────────────────────────────────────────────────
Actual data:     100      100      200      200        200       200
What T1 sees:    100                        100 ✅               (ends)
What T2 sees:                               200                  200
─────────────────────────────────────────────────────────────────────────────
  ✅ T1 reads 100 at both t2 and t5 — repeatable read guaranteed
     T1's snapshot (t1) is frozen; T2's committed write is invisible to T1
     After T1 commits, a NEW transaction would see 200
```

```
Timeline Diagram — Repeatable Read (Phantom Read still possible per SQL standard):
─────────────────────────────────────────────────────────────────────────────
Time ──→   t1       t2       t3       t4       t5       t6
─────────────────────────────────────────────────────────────────────────────

T1:      BEGIN    SELECT COUNT(*)            SELECT COUNT(*)    COMMIT
         ║        WHERE dept='Eng'           WHERE dept='Eng'     ║
         ║        → 10 rows                  → 11 rows ⚠         ║
         ║          │                          │ PHANTOM!         ║
         ║          │                          │                  ║
T2:      ║        ║       BEGIN    INSERT     COMMIT              ║
         ║        ║         ║      ('Alice',   ║                  ║
         ║        ║         ║       'Eng')     ║                  ║
─────────────────────────────────────────────────────────────────────────────
Rows matching:   10        10       11        11        11       11
─────────────────────────────────────────────────────────────────────────────
  ⚠ New row appeared in range query — phantom read
    (MySQL InnoDB prevents this via gap locks; PostgreSQL via MVCC snapshot)
```

```
When to Use:
  - Financial reports that must be self-consistent
  - Any read-heavy transaction that re-reads the same data
  - Batch processing that needs a stable view of data
  - MySQL default — most MySQL applications run at this level
```

---

### 4. Serializable (Highest)

Transactions execute as if they were run **one after another** (serially), even though they may actually run concurrently. Prevents ALL anomalies.

```
Behavior:
  - Equivalent to serial execution
  - No anomalies possible
  - Highest correctness, lowest concurrency

Prevented:
  ✅ Dirty Read
  ✅ Non-Repeatable Read
  ✅ Phantom Read
  ✅ Lost Update
  ✅ Write Skew
```

```sql
-- The Doctor On-Call Problem (Write Skew) — SOLVED

-- Session 1
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN;
SELECT COUNT(*) FROM doctors WHERE on_call = true;
→ Returns 2

UPDATE doctors SET on_call = false WHERE name = 'Dr. A';

-- Session 2
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN;
SELECT COUNT(*) FROM doctors WHERE on_call = true;
→ Returns 2

UPDATE doctors SET on_call = false WHERE name = 'Dr. B';

-- Session 1
COMMIT;  ← succeeds

-- Session 2
COMMIT;  ← FAILS with serialization error
         -- "ERROR: could not serialize access due to read/write dependencies"
         -- Application must retry T2
```

```
Implementation Approaches:
─────────────────────────────────────────────────────────
1. Actual Serial Execution (Redis, VoltDB)
   - Single-threaded execution — truly serial
   - Fast for short transactions, terrible for long ones

2. Two-Phase Locking (2PL) — (SQL Server, MySQL with SELECT ... FOR UPDATE)
   - Shared locks on reads, exclusive locks on writes
   - Range locks to prevent phantoms
   - Deadlock detection required

3. Serializable Snapshot Isolation (SSI) — (PostgreSQL)
   - Optimistic: transactions run concurrently using MVCC
   - At commit time, checks for serialization conflicts
   - If conflict detected → abort and retry
   - Better throughput than 2PL for read-heavy workloads
─────────────────────────────────────────────────────────
```

```
Timeline Diagram — Serializable (2PL-based: lock approach):
─────────────────────────────────────────────────────────────────────────────
Time ──→   t1       t2       t3       t4       t5       t6       t7
─────────────────────────────────────────────────────────────────────────────

T1:      BEGIN    READ     WRITE             COMMIT   UNLOCK
         ║        seats    seats=0            ║        ALL
         ║        → 1      + INSERT           ║          │
         ║        LOCK(S)  LOCK(X)            ║          │
         ║        held     held               ║          │
         ║          │        │                ║          │
T2:      ║        BEGIN   READ seats          ║        ║       READ seats
         ║          ║      → BLOCKED 🔒       ║        ║       → 0
         ║          ║      (T1 holds lock)    ║        ║       (no seats)
         ║          ║      waiting...         ║        ║       ABORT
         ║          ║      waiting...         ║        ║       (app logic)
─────────────────────────────────────────────────────────────────────────────
  🔒 T2 is blocked at t3 because T1 holds a lock on the seats row
     T2 can only proceed after T1 commits and releases locks
     Result: No double-booking — serial equivalent: T1 → T2
```

```
Timeline Diagram — Serializable (SSI-based: optimistic approach — PostgreSQL):
─────────────────────────────────────────────────────────────────────────────
Time ──→   t1       t2       t3       t4       t5       t6       t7
─────────────────────────────────────────────────────────────────────────────

T1:      BEGIN    READ     WRITE             COMMIT ✅
         ║        on_call  Dr.A=off           ║
         ║        → 2      ║                  ║
         ║        ║        ║                  ║
         ║        ║        ║                  ║
T2:      ║      BEGIN     READ     WRITE     ║        COMMIT ❌
         ║        ║       on_call  Dr.B=off   ║        SERIALIZATION
         ║        ║       → 2      ║          ║        ERROR!
         ║        ║       ║        ║          ║        (must retry)
─────────────────────────────────────────────────────────────────────────────
  Both T1 and T2 run concurrently (optimistic — no blocking)
  At commit time, SSI detects rw-dependency cycle:
    T1 read on_call count → T2 wrote Dr.B=off (affects T1's read)
    T2 read on_call count → T1 wrote Dr.A=off (affects T2's read)
  → Cycle detected → T2 aborted → Application retries T2
  → T2 retry reads on_call=1 → cannot remove → constraint preserved ✅
```

```
When to Use:
  - Financial transactions (double-entry bookkeeping)
  - Constraint enforcement that spans multiple rows/tables
  - Any scenario where write skew is unacceptable
  - Systems where correctness > throughput
```

---

## Side-by-Side: Same Scenario Across All Isolation Levels

Scenario: T1 writes price=150 (uncommitted), T2 reads price, T1 commits, T2 reads again.

```
─────────────────────────────────────────────────────────────────────────────
Time ──→       t1          t2          t3          t4          t5
               T1:BEGIN    T1:WRITE    T2:READ     T1:COMMIT   T2:READ
               ║           price=150   price=?     ║           price=?
─────────────────────────────────────────────────────────────────────────────

Read           ║           ║           → 150 ⚠     ║           → 150
Uncommitted    ║           ║           (dirty!)    ║           ║

Read           ║           ║           → 100 ✅    ║           → 150 ⚠
Committed      ║           ║           (clean)     ║           (non-repeatable)

Repeatable     ║           ║           → 100 ✅    ║           → 100 ✅
Read           ║           ║           (clean)     ║           (stable snapshot)

Serializable   ║           ║           → 100 ✅    ║           → 100 ✅
               ║           ║           (clean)     ║           (stable + conflict
               ║           ║                       ║            detection at commit)
─────────────────────────────────────────────────────────────────────────────

Summary of what T2 sees at each read:
┌────────────────────┬──────────────┬──────────────┬───────────────────────┐
│ Isolation Level    │ T2 READ (t3) │ T2 READ (t5) │ Anomaly               │
│                    │ (before T1   │ (after T1    │                       │
│                    │  commits)    │  commits)    │                       │
├────────────────────┼──────────────┼──────────────┼───────────────────────┤
│ Read Uncommitted   │     150      │     150      │ Dirty Read at t3      │
│ Read Committed     │     100      │     150      │ Non-Repeatable at t5  │
│ Repeatable Read    │     100      │     100      │ None                  │
│ Serializable       │     100      │     100      │ None                  │
└────────────────────┴──────────────┴──────────────┴───────────────────────┘
```

---

## Isolation Level Comparison Matrix

```
┌────────────────────┬──────────┬────────────────┬─────────┬──────────────┬────────────┐
│ Isolation Level    │  Dirty   │ Non-Repeatable │ Phantom │ Lost Update  │ Write Skew │
│                    │  Read    │     Read       │  Read   │              │            │
├────────────────────┼──────────┼────────────────┼─────────┼──────────────┼────────────┤
│ Read Uncommitted   │  ✗ Yes   │    ✗ Yes       │ ✗ Yes   │   ✗ Yes      │  ✗ Yes     │
│ Read Committed     │  ✅ No   │    ✗ Yes       │ ✗ Yes   │   ✗ Yes      │  ✗ Yes     │
│ Repeatable Read    │  ✅ No   │    ✅ No        │ ✗ Yes*  │   ✅ No      │  ✗ Yes     │
│ Serializable       │  ✅ No   │    ✅ No        │ ✅ No   │   ✅ No      │  ✅ No     │
└────────────────────┴──────────┴────────────────┴─────────┴──────────────┴────────────┘

* MySQL InnoDB and PostgreSQL prevent phantoms at Repeatable Read in practice.
```

---

## How Popular Databases Implement Isolation

```
┌──────────────┬──────────────────┬──────────────────────────────────────────┐
│ Database     │ Default Level    │ Implementation                           │
├──────────────┼──────────────────┼──────────────────────────────────────────┤
│ PostgreSQL   │ Read Committed   │ MVCC (multi-version concurrency control) │
│              │                  │ SSI for Serializable level               │
├──────────────┼──────────────────┼──────────────────────────────────────────┤
│ MySQL        │ Repeatable Read  │ MVCC + next-key locks (gap locking)      │
│ (InnoDB)     │                  │ Prevents phantoms even at RR             │
├──────────────┼──────────────────┼──────────────────────────────────────────┤
│ SQL Server   │ Read Committed   │ Lock-based by default                    │
│              │                  │ RCSI (snapshot) optional                 │
├──────────────┼──────────────────┼──────────────────────────────────────────┤
│ Oracle       │ Read Committed   │ MVCC (undo segments)                     │
│              │                  │ Only supports RC and Serializable        │
├──────────────┼──────────────────┼──────────────────────────────────────────┤
│ CockroachDB  │ Serializable     │ Serializable by default (SSI-based)      │
│              │                  │ No weaker levels available               │
└──────────────┴──────────────────┴──────────────────────────────────────────┘
```

---

## MVCC — The Engine Behind Modern Isolation

Most modern databases use **Multi-Version Concurrency Control** instead of pure locking.

```
Core Idea: Keep multiple versions of each row. Readers see the version
           appropriate for their snapshot. Writers create new versions.

┌─────────────────────────────────────────────────────────────────┐
│                     MVCC Version Chain                           │
│                                                                  │
│  accounts (id=1):                                                │
│                                                                  │
│  Version 3: balance=300, txn_id=103, created=t3  ← latest       │
│      ↓                                                           │
│  Version 2: balance=200, txn_id=102, created=t2  ← for txns     │
│      ↓                                            started < t3   │
│  Version 1: balance=100, txn_id=101, created=t1  ← oldest       │
│                                                                  │
│  Transaction started at t2 → sees Version 2 (balance=200)        │
│  Transaction started at t3 → sees Version 3 (balance=300)        │
└─────────────────────────────────────────────────────────────────┘

Key Benefit: Readers don't block writers. Writers don't block readers.
             Only writer-writer conflicts need resolution.
```

```
MVCC Visibility Rules (simplified):
─────────────────────────────────────────────────────────
A row version is visible to transaction T if:
  1. The version was created by a committed transaction
  2. That transaction committed BEFORE T's snapshot timestamp
  3. The version was not deleted (or deleted AFTER T's snapshot)

Read Committed:  snapshot = start of each STATEMENT
Repeatable Read: snapshot = start of the TRANSACTION
Serializable:    snapshot = start of the TRANSACTION + conflict detection
─────────────────────────────────────────────────────────
```

---

## Practical Examples: Choosing the Right Level

### Example 1: E-Commerce Product Catalog (Read Committed)

```
Scenario: Users browsing products while admin updates prices.

Why Read Committed:
  - Users always see committed prices (no dirty reads)
  - If price changes mid-browse, showing the new price is acceptable
  - High read concurrency needed — MVCC handles this well
  - No business requirement for repeatable reads within a single page load

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN;
SELECT * FROM products WHERE category = 'electronics';
-- Returns current committed prices, even if they changed since page load
COMMIT;
```

### Example 2: Bank Account Transfer (Repeatable Read)

```
Scenario: Transfer $500 from Account A to Account B.

Why Repeatable Read:
  - Must read A's balance, verify sufficient funds, then debit
  - Balance must not change between the check and the debit
  - Prevents non-repeatable read on the balance

SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN;
SELECT balance FROM accounts WHERE id = 'A';  -- Returns 1000
-- Application logic: 1000 >= 500, proceed

UPDATE accounts SET balance = balance - 500 WHERE id = 'A';
UPDATE accounts SET balance = balance + 500 WHERE id = 'B';
COMMIT;

-- If another txn modified A's balance after our SELECT,
-- the UPDATE will detect the conflict (in MVCC) or block (in 2PL).
```

### Example 3: Seat Booking System (Serializable)

```
Scenario: Two users try to book the last seat on a flight.

Why Serializable:
  - Must prevent double-booking (write skew)
  - Both users read "1 seat available", both try to book

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN;
SELECT available_seats FROM flights WHERE id = 'FL-100';
-- Returns 1

-- Application: 1 > 0, proceed with booking
INSERT INTO bookings (flight_id, passenger) VALUES ('FL-100', 'User-A');
UPDATE flights SET available_seats = available_seats - 1 WHERE id = 'FL-100';
COMMIT;

-- Concurrent transaction for User-B:
-- At Serializable level, one of them will get a serialization error
-- and must retry — preventing double-booking.
```

---

## Locking Strategies at Each Level

```
┌────────────────────┬────────────────────────────────────────────────────┐
│ Isolation Level    │ Locks Acquired                                     │
├────────────────────┼────────────────────────────────────────────────────┤
│ Read Uncommitted   │ No read locks. Writers don't block readers.        │
│                    │                                                    │
│ Read Committed     │ Shared lock on read (released immediately).        │
│                    │ Exclusive lock on write (held until commit).       │
│                    │                                                    │
│ Repeatable Read    │ Shared lock on read (held until commit).           │
│                    │ Exclusive lock on write (held until commit).       │
│                    │                                                    │
│ Serializable       │ Shared lock on read (held until commit).           │
│                    │ Exclusive lock on write (held until commit).       │
│                    │ Range locks on predicates (prevent phantoms).      │
└────────────────────┴────────────────────────────────────────────────────┘

Note: MVCC-based databases (PostgreSQL, Oracle) replace most read locks
      with snapshot visibility rules, achieving better concurrency.
```

---

## Snapshot Isolation (SI) — The "Fifth" Level

Not part of the SQL standard, but widely implemented (SQL Server's RCSI, PostgreSQL's Repeatable Read).

```
Snapshot Isolation:
  - Each transaction sees a consistent snapshot from its start time
  - Readers never block writers, writers never block readers
  - Write-write conflicts detected: if two txns modify the same row,
    the second one to commit is aborted ("first committer wins")

Prevents:
  ✅ Dirty Read
  ✅ Non-Repeatable Read
  ✅ Phantom Read

Does NOT Prevent:
  ✗ Write Skew (this is why SI ≠ Serializable)
```

```
Write Skew Under Snapshot Isolation:

T1 reads X=1, Y=1 (constraint: X+Y > 0)
T2 reads X=1, Y=1

T1: UPDATE X = 0  (X+Y = 0+1 = 1, OK)
T2: UPDATE Y = 0  (X+Y = 1+0 = 1, OK)

Both commit. Result: X=0, Y=0 → X+Y = 0 → CONSTRAINT VIOLATED

SI doesn't catch this because T1 and T2 wrote to different rows.
Serializable (SSI) would detect the read-write dependency and abort one.
```

---

## Decision Guide

```
                        ┌─────────────────────┐
                        │ Do you need strict   │
                        │ correctness across   │
                        │ multiple rows/tables?│
                        └──────────┬──────────┘
                              ╱         ╲
                          YES              NO
                          ╱                  ╲
                 ┌───────▼────────┐    ┌──────▼──────────┐
                 │  SERIALIZABLE  │    │ Do you re-read   │
                 │                │    │ same data within │
                 │ (banking,      │    │ a transaction?   │
                 │  booking,      │    └──────┬──────────┘
                 │  inventory)    │       ╱         ╲
                 └────────────────┘   YES              NO
                                      ╱                  ╲
                            ┌────────▼──────┐    ┌───────▼────────┐
                            │ REPEATABLE    │    │ READ COMMITTED │
                            │ READ          │    │                │
                            │               │    │ (web apps,     │
                            │ (reports,     │    │  CRUD APIs,    │
                            │  batch jobs,  │    │  general OLTP) │
                            │  transfers)   │    └────────────────┘
                            └───────────────┘
```

---

## Common Interview Points

1. **Read Committed is the most common default** — PostgreSQL, Oracle, SQL Server all use it.
2. **MySQL InnoDB defaults to Repeatable Read** and prevents phantoms via gap locks (stronger than SQL standard requires).
3. **Snapshot Isolation ≠ Serializable** — SI allows write skew. PostgreSQL's "Repeatable Read" is actually SI. Its "Serializable" is SSI.
4. **MVCC eliminates most read-write contention** — only write-write conflicts remain.
5. **Serializable doesn't mean slow** — SSI (PostgreSQL) is optimistic and performs well for read-heavy workloads. Only conflicts cause retries.
6. **Application-level retries are mandatory at Serializable** — transactions can be aborted due to serialization failures; the app must handle this.
7. **SELECT ... FOR UPDATE** can simulate higher isolation at a lower level — acquires exclusive lock on selected rows, preventing concurrent modifications.
