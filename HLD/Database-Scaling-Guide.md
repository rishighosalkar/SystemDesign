# Database Scaling, Indexing & Concurrency — Detailed Study Notes

## Table of Contents

- [1. Concurrency Control — Pessimistic vs Optimistic Locking](#1-concurrency-control--pessimistic-vs-optimistic-locking)
- [2. Indexing Fundamentals](#2-indexing-fundamentals)
- [3. Clustered vs Non-Clustered Index](#3-clustered-vs-non-clustered-index)
- [4. Composite Indexes](#4-composite-indexes)
- [5. Designing Indexes for 10M+ Rows](#5-designing-indexes-for-10m-rows)
- [6. How to Choose Columns for Indexing](#6-how-to-choose-columns-for-indexing)
- [7. Indexing at Scale — Why It Can Be Problematic](#7-indexing-at-scale--why-it-can-be-problematic)
- [8. Alternatives & Complements to Indexing](#8-alternatives--complements-to-indexing)
- [9. Sharding vs Partitioning — Deep Dive](#9-sharding-vs-partitioning--deep-dive)
- [10. Interview-Ready Answers](#10-interview-ready-answers)
- [11. Further Topics](#11-further-topics)

---

## 1. Concurrency Control — Pessimistic vs Optimistic Locking

### 1.1 What is Concurrency Control?

When multiple users try to modify the same data at the same time, we must prevent:

- ❌ Lost updates
- ❌ Dirty reads
- ❌ Inconsistent state

Two strategies to solve this: **Pessimistic Locking** and **Optimistic Locking**.

---

### 1.2 Pessimistic Locking

**Idea:** "I don't trust anyone. Lock the record before editing."

When a transaction reads a record, it **locks it immediately**, so no one else can modify it until the transaction completes.

**Real-Life Example — ATM Withdrawal:**

- User A withdraws ₹5000 → Account row is locked
- User B must **wait** until A completes the transaction

**How It Works (Database Level):**

The database places a `ROW LOCK` or `TABLE LOCK`.

```sql
SELECT * FROM Accounts
WHERE Id = 1
FOR UPDATE;
```

This locks the row until the transaction completes.

**In .NET (EF Core):**

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();

var account = await _context.Accounts
   .FromSqlRaw("SELECT * FROM Accounts WHERE Id = 1 FOR UPDATE")
   .FirstOrDefaultAsync();

account.Balance -= 5000;

await _context.SaveChangesAsync();
await transaction.CommitAsync();
```

**Advantages:**

- ✅ No concurrency conflicts
- ✅ Safe for high-contention systems
- ✅ Good for financial transactions

**Disadvantages:**

- ❌ Poor scalability
- ❌ Can cause deadlocks
- ❌ Threads wait (blocking)

---

### 1.3 Optimistic Locking

**Idea:** "Conflicts are rare. Let everyone work. Detect conflict at save time."

Instead of locking rows, we:

1. Add a **version column**
2. Check version before update
3. If changed → **reject update**

**Real-Life Example — Profile Edit:**

- Both users load version 1
- User A saves → version becomes 2
- User B tries to save → **fails** (version mismatch)

**How It Works:**

Add a `RowVersion` / `Timestamp` / `Version` column.

**In .NET (EF Core):**

Step 1 — Add Version Property:

```csharp
public class Product
{
   public int Id { get; set; }
   public string Name { get; set; }

   [Timestamp]
   public byte[] RowVersion { get; set; }
}
```

OR Fluent API:

```csharp
builder.Property(p => p.RowVersion)
      .IsRowVersion();
```

Step 2 — Save Changes (EF automatically checks version):

```csharp
try
{
   await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
   // Handle conflict
}
```

If someone else modified the row → EF throws `DbUpdateConcurrencyException`.

**What SQL Looks Like Internally:**

```sql
UPDATE Products
SET Name = 'New Name'
WHERE Id = 1 AND RowVersion = @OriginalVersion
```

If **no rows affected** → conflict detected.

**Advantages:**

- ✅ Highly scalable
- ✅ No blocking
- ✅ Great for web applications

**Disadvantages:**

- ❌ Must handle conflicts
- ❌ Retry logic needed

---

### 1.4 Comparison Table

| Feature            | Optimistic         | Pessimistic        |
| ------------------ | ------------------ | ------------------ |
| Locks row early?   | ❌ No              | ✅ Yes             |
| Scalable?          | ✅ High            | ❌ Lower           |
| Conflict detection | At save time       | Before update      |
| Good for           | Web apps           | Banking systems    |
| Blocking           | ❌ No              | ✅ Yes             |

### 1.5 When to Use What?

**Use Optimistic Locking when:**

- Web applications
- Low conflict probability
- High scalability required
- Microservices architecture
- 👉 **90% of modern systems use this**

**Use Pessimistic Locking when:**

- Financial systems
- Inventory systems with limited stock
- High conflict likelihood
- Critical consistency required

---

## 2. Indexing Fundamentals

### 2.1 What is an Index?

An index in SQL is like a **book index**. Instead of scanning the whole table (Full Table Scan), the database uses a data structure (**B-Tree**) to quickly find rows.

- Without index: `O(n)` scan
- With index: `O(log n)` search

---

## 3. Clustered vs Non-Clustered Index

### 3.1 Clustered Index

**Definition:** A clustered index determines the **physical order of data** in a table.

- The table data itself is stored in **sorted order**
- A table can have **ONLY ONE** clustered index
- The data pages = the index (they are the same)
- Implemented using **B-Tree**
- **Leaf nodes contain actual data rows**

**Visual Understanding:**

Without Clustered Index (stored randomly on disk):

| Id | Name |
|----|------|
| 5  | A    |
| 2  | B    |
| 8  | C    |

With Clustered Index on Id (physically sorted):

| Id | Name |
|----|------|
| 2  | B    |
| 5  | A    |
| 8  | C    |

**SQL Example — Create Table (PK = Clustered by default in SQL Server):**

```sql
CREATE TABLE Employees (
   Id INT PRIMARY KEY,  -- Automatically clustered in SQL Server
   Name VARCHAR(100),
   Salary INT
);
```

**Explicitly Creating Clustered Index:**

```sql
CREATE CLUSTERED INDEX IX_Employees_Id
ON Employees(Id);
```

**When to Use Clustered Index:**

- ✅ Primary key
- ✅ Frequently searched column
- ✅ Range queries (`BETWEEN`, `>`, `<`)
- ✅ Sorting operations

**Example — Perfect for clustered index:**

```sql
SELECT * FROM Orders WHERE OrderDate BETWEEN '2025-01-01' AND '2025-01-31';
```

---

### 3.2 Non-Clustered Index

**Definition:** A separate structure that stores **key + pointer** to actual data.

- Does **NOT** change physical order
- You can have **multiple** non-clustered indexes
- Leaf nodes store **pointer (Row ID)**
- If clustered index exists → pointer = clustered key
- If no clustered index → pointer = row address (RID)

**Visual Understanding:**

Table: `| Id | Name | Email |`

Non-clustered index on Email:

| Email           | Pointer |
|-----------------|---------|
| a@gmail.com     | Row 3   |
| b@gmail.com     | Row 1   |

**SQL Example:**

```sql
CREATE NONCLUSTERED INDEX IX_Employees_Name
ON Employees(Name);
```

**Covering Index Example:**

```sql
CREATE NONCLUSTERED INDEX IX_Employees_Name
ON Employees(Name)
INCLUDE (Salary);
```

Now this query **won't touch the main table** → faster:

```sql
SELECT Name, Salary FROM Employees WHERE Name = 'John';
```

---

### 3.3 Clustered vs Non-Clustered Comparison

| Feature                  | Clustered          | Non-Clustered       |
| ------------------------ | ------------------ | ------------------- |
| Physical order changes?  | ✅ Yes             | ❌ No               |
| Number allowed           | 1                  | Multiple            |
| Leaf nodes contain       | Actual data        | Pointer to data     |
| Good for                 | Range queries      | Exact match search  |
| Insert performance       | Slower             | Faster              |
| Storage                  | No extra storage   | Extra storage       |

### 3.4 Performance Impact

- **Clustered Index:** Fast range scans, slower inserts if random keys (like GUID)
- **Non-Clustered Index:** Fast selective queries, slower writes (index maintenance)

### 3.5 What Happens During SELECT?

- If index exists → **Index Seek** (fast) → Key Lookup (if needed)
- If no index → **Table Scan** (slow)

### 3.6 Best Practices

**Use Clustered Index On:**

- Identity column
- Increasing column
- Primary key

**Avoid Clustered Index On:**

- Random GUID
- Frequently updated column

**Real-World Example — E-commerce Orders Table:**

- Clustered: `OrderId` (PK)
- Non-Clustered: `UserId`, `OrderDate`, `Status`

### 3.7 Advanced Concepts (High-Level)

- Composite Index
- Index Fragmentation
- Fill Factor
- Included Columns
- Filtered Index
- Index Scan vs Seek
- Execution Plan


---

## 4. Composite Indexes

### 4.1 What Is a Composite Index?

A composite index is an index built on **multiple columns**.

Single-column index:

```sql
CREATE INDEX IX_Users_Email
ON Users(Email);
```

Composite index:

```sql
CREATE INDEX IX_Users_First_Last
ON Users(FirstName, LastName);
```

### 4.2 Why Composite Indexes Matter

Most real-world queries filter on **multiple columns**:

```sql
SELECT * FROM Orders
WHERE UserId = 10
AND Status = 'Completed'
AND CreatedDate >= '2026-01-01';
```

A single-column index **won't fully optimize** this. A composite index **can**.

### 4.3 How Composite Index Works Internally

Composite indexes use a **B-Tree** structure. If the index is `(UserId, Status, CreatedDate)`, the index is sorted like:

```
UserId → then Status → then CreatedDate

UserId
  ├── Status
  │      ├── CreatedDate
```

### 4.4 The Leftmost Prefix Rule (MOST IMPORTANT)

> This is where most developers fail interviews.

If index is: `(UserId, Status, CreatedDate)`

**✅ Index IS used for:**

- `WHERE UserId = 10`
- `WHERE UserId = 10 AND Status = 'Completed'`
- `WHERE UserId = 10 AND Status = 'Completed' AND CreatedDate > X`

**❌ Index is NOT used efficiently for:**

- `WHERE Status = 'Completed'` (skips first column)
- `WHERE CreatedDate > X` (skips first two columns)

**Because index order matters.**

### 4.5 How to Decide Column Order

Follow this priority:

1. **High Selectivity first** (most unique values)
2. **Frequently used in WHERE**
3. **Used in JOIN**
4. **Used in ORDER BY**

**Bad index:** `(Status, UserId)` — if most queries filter by `UserId` first, this is inefficient.

### 4.6 Practical Example — E-commerce Orders

Most common query:

```sql
SELECT * FROM Orders
WHERE UserId = 10
ORDER BY CreatedDate DESC;
```

Best composite index:

```sql
CREATE INDEX IX_Orders_User_Date
ON Orders(UserId, CreatedDate DESC);
```

Result:

- ✅ No sort needed
- ✅ No table scan
- ✅ Pure index seek

### 4.7 Composite + Covering Index

If query is:

```sql
SELECT Status, Amount
FROM Orders
WHERE UserId = 10;
```

Create:

```sql
CREATE INDEX IX_Orders_User
ON Orders(UserId)
INCLUDE (Status, Amount);
```

Result:

- ✅ No key lookup
- ✅ Fully covered by index
- This is called a **covering index**

### 4.8 Index Seek vs Index Scan

- Good composite index → **Index Seek** ✅
- Bad column order → **Index Scan** ⚠️
- Execution plan will show the difference

### 4.9 Real Production Mistake

Developers create:

```sql
CREATE INDEX IX_Orders_Status_User
ON Orders(Status, UserId);
```

But queries are:

```sql
WHERE UserId = ?
```

DB **ignores index** → full scan → slow system. Column order was wrong.

### 4.10 Advanced Concepts (Senior Level)

**Composite Index with Range Condition:**

If index is `(UserId, CreatedDate)`:

- `WHERE UserId = 10 AND CreatedDate > '2026-01-01'` → ✅ Works well
- `WHERE CreatedDate > '2026-01-01' AND UserId = 10` → ✅ Still works (query order doesn't matter, index order does)
- `WHERE UserId > 10 AND CreatedDate > X` → ⚠️ Range on first column makes second column less useful

**Index Cardinality:**

- Higher uniqueness = better index
- Bad candidate: `Gender` (M/F)
- Good candidate: `Email`, `UserId`, `OrderId`

**Write Performance Tradeoff:**

- Every `INSERT`/`UPDATE` must update **ALL** indexes
- More composite indexes = slower writes
- **Don't over-index**

### 4.11 Single vs Composite Comparison

| Scenario          | Single Index | Composite Index          |
| ----------------- | ------------ | ------------------------ |
| `WHERE A`         | ✅           | ✅                       |
| `WHERE A AND B`   | ❌ (partial) | ✅                       |
| `WHERE B` only    | ❌           | ❌ (if B is not first)   |

### 4.12 .NET Context Example

Table:

```
Interviews
------------
Id
UserId
Status
CreatedAt
Score
```

Most queries: `WHERE UserId = ? AND Status = ? ORDER BY CreatedAt DESC`

Best index:

```sql
CREATE INDEX IX_Interviews_User_Status_Date
ON Interviews(UserId, Status, CreatedAt DESC);
```

This will **massively improve performance**.

---

## 5. Designing Indexes for 10M+ Rows

> At 10M+ rows, indexing is no longer "add index and pray" — it becomes **architecture + workload analysis + trade-offs**.

### 5.1 Step 1: Understand the Workload First (Most Important)

Before creating any index, answer:

- 🔎 What are the **top 5 most frequent queries**?
- 📈 Are **reads >> writes**?
- 🔁 Are there **range queries**?
- 📊 Are there **heavy JOINs**?
- 📅 Is data **time-series**?

> Index design is **workload-driven** — not table-driven.

### 5.2 Step 2: Choose a Good Clustered Index

For 10M+ rows, this matters **A LOT**.

**Rule: Clustered index must be:**

- **Narrow** (small size)
- **Unique**
- **Increasing** (to avoid fragmentation)

```sql
-- ✅ Good:
Id BIGINT IDENTITY PRIMARY KEY

-- ❌ Bad:
GUID NEWID()   -- random inserts = fragmentation
```

**Why?** Random inserts → page splits → fragmentation → slow I/O.

### 5.3 Step 3: Design Composite Indexes Based on Query Patterns

**Example table:** `Orders` (10M rows) — `Id (PK)`, `UserId`, `Status`, `CreatedAt`, `Amount`

Most common query:

```sql
SELECT * FROM Orders
WHERE UserId = ?
AND Status = ?
ORDER BY CreatedAt DESC;
```

Correct index:

```sql
CREATE INDEX IX_Orders_User_Status_Date
ON Orders(UserId, Status, CreatedAt DESC);
```

**Why this order?**

- `UserId` → high selectivity
- `Status` → secondary filter
- `CreatedAt` → supports ORDER BY (no sorting required → huge win)

### 5.4 Step 4: Avoid Index Overload

Every index:

- Increases disk usage
- Slows `INSERT`
- Slows `UPDATE`
- Slows `DELETE`

For 10M+ rows:

- ❌ Don't create 10 indexes blindly
- ✅ Create indexes **only for critical queries**

### 5.5 Step 5: Use Covering Indexes Carefully

```sql
-- Query:
SELECT UserId, Status, Amount FROM Orders WHERE UserId = 10;

-- Covering index:
CREATE INDEX IX_Orders_User
ON Orders(UserId)
INCLUDE (Status, Amount);
```

This avoids key lookup. **BUT:** Too many `INCLUDE` columns → index becomes large → slower memory caching. **Balance is key.**

### 5.6 Step 6: Handle Range Queries Correctly

For time-based queries:

```sql
WHERE CreatedAt BETWEEN '2026-01-01' AND '2026-01-31'
```

Index: `CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);`

If combined:

```sql
WHERE UserId = ? AND CreatedAt > ?
```

Better index: `(UserId, CreatedAt)` — **equality column first → range column next**.

### 5.7 Step 7: Watch Selectivity (Very Important)

| Column Type      | Example          | Good Index? |
| ---------------- | ---------------- | ----------- |
| High selectivity | Email, UserId    | ✅ Yes      |
| Low selectivity  | Gender (M/F)     | ❌ No       |
| Low selectivity  | Boolean columns  | ❌ No       |
| Medium           | Status (few vals)| ⚠️ Only in composite |

> Low selectivity columns **alone** are useless as indexes.

### 5.8 Step 8: Partition Large Tables (Advanced)

For 10M+ rows and time-based data, partition by `CreatedAt` (monthly/yearly):

- Smaller index per partition
- Faster range scans
- Easier archiving

### 5.9 Step 9: Monitor Execution Plans

Always check:

- **Index Seek** → ✅ good
- **Index Scan** → ⚠️ warning
- **Table Scan** → ❌ bad
- **Key Lookup** → sometimes OK, sometimes not

```sql
-- SQL Server:
SET STATISTICS IO ON;

-- PostgreSQL:
EXPLAIN ANALYZE
```

### 5.10 Step 10: Maintain Indexes

For 10M+ rows:

- Rebuild/Reorganize fragmented indexes
- Monitor fill factor
- Update statistics

```sql
ALTER INDEX IX_Orders_User_Status_Date
ON Orders
REBUILD;
```

### 5.11 Real Performance Example

- **Without index:** 10M rows → full scan → 2–5 seconds
- **With correct composite index:** Index Seek → 5–20 ms
- That's **100x improvement**

---

## 6. How to Choose Columns for Indexing

### 6.1 Core Principle

> Indexing is NOT about columns. It's about **query patterns**.

### 6.2 Step 1: Identify Frequent Queries

Check what columns appear in:

- `WHERE` clauses
- `JOIN` conditions
- `ORDER BY`
- `GROUP BY`
- Foreign keys

**Example query:**

```sql
SELECT * FROM Orders
WHERE UserId = 10
AND Status = 'Completed'
ORDER BY CreatedAt DESC;
```

You should index based on **this query** — not table structure.

### 6.3 Step 2: Choose Columns Used in WHERE (Highest Priority)

- ✅ Good: `UserId`, `Email`, `OrderId`
- ❌ Bad: `Gender`, Boolean flags, columns with very few distinct values

### 6.4 Step 3: Check Selectivity (VERY IMPORTANT)

Selectivity = how unique values are.

- **High selectivity** → good index
- **Low selectivity** → bad index

| Column    | Distinct Values | Good Index? |
| --------- | --------------- | ----------- |
| Email     | 10M unique      | ✅ Excellent |
| Gender    | 2 values        | ❌ Bad       |
| Status    | 5 values        | ⚠️ Only in composite |

### 6.5 Step 4: Composite Index Column Order

Follow this rule:

1. **Equality columns first**
2. **Range columns next**
3. **ORDER BY columns last**

```sql
-- Query:
WHERE UserId = ? AND Status = ? AND CreatedAt > ?

-- Best index:
(UserId, Status, CreatedAt)
```

### 6.6 Don't Index Columns That:

- ❌ Are rarely queried
- ❌ Are frequently updated
- ❌ Are very large (`TEXT`/`BLOB`)
- ❌ Have low cardinality alone
- ❌ Belong to small tables (< few thousand rows)

### 6.7 When to Choose Clustered vs Non-Clustered

**Choose Clustered When:**

- Column is `PRIMARY KEY`
- Increasing value (`IDENTITY`, `BIGINT`)
- Used in range queries
- Used frequently in JOINs

**Don't Choose Clustered When:**

- Random GUID (`NEWID`)
- Frequently updated column
- Large composite key
- Non-unique column

**Why?** Random inserts → page splits → fragmentation → performance drop.

**Choose Non-Clustered When:**

- Optimizing a specific query
- Adding composite index
- Creating covering index
- Optimizing foreign key lookups

### 6.8 Decision Table

| Scenario                    | Use              |
| --------------------------- | ---------------- |
| Primary key                 | Clustered        |
| Frequently filtered column  | Non-clustered    |
| Range query                 | Clustered (if primary range column) |
| Supporting JOIN             | Non-clustered    |
| Covering query              | Non-clustered with INCLUDE |

### 6.9 When NOT to Use Indexing

This is where **seniors stand out**.

**❌ 1. Very Small Tables**
If table has 1000 rows → full scan is faster than index seek. Index overhead not worth it.

**❌ 2. High Write Workload Tables**
Every `INSERT`/`UPDATE`/`DELETE` must update indexes. If table has 10 indexes and 1000 writes/sec → system becomes slow.

**❌ 3. Low Selectivity Columns**
Example: `Status` (Pending, Completed, Cancelled). If 80% rows = Pending → index useless.

**❌ 4. Frequently Updated Columns**
Example: `LastAccessedTime`. Index on this → constant rebalancing → slow writes.

**❌ 5. Huge TEXT/BLOB Columns**
Indexing big columns increases index size drastically.

### 6.10 Real Example (10M Rows)

Table: `Users` — `Id (PK)`, `Email`, `City`, `IsActive`, `CreatedAt`

Most common queries: `WHERE Email = ?`, `WHERE City = ?`, `WHERE IsActive = 1`

**Correct indexing:**

- Clustered: `Id`
- Non-clustered: `Email` (high selectivity)
- Non-clustered composite: `(City, IsActive)`
- **Do NOT index:** `IsActive` alone

### 6.11 Performance Thinking for 10M+

Always ask:

- How many reads vs writes?
- Is this OLTP or analytics?
- Is data time-series?
- Do I need partitioning?


---

## 7. Indexing at Scale — Why It Can Be Problematic

> 👉 Indexing is **NOT** bad for large data.
> 👉 **Bad indexing strategy** is bad for large data.

Indexes accelerate reads, but at scale (10M+ / 100M+ rows), they introduce **write amplification**, **memory pressure**, and **maintenance cost**.

### 7.1 Write Amplification (Biggest Problem)

Every `INSERT`, `UPDATE`, `DELETE` must update:

- Table data
- **All indexes** on that table

If table has 8 indexes → **1 write = 9 write operations internally**.

At scale (1000+ writes/sec):

- Disk I/O spikes
- Locking increases
- CPU usage increases

> Over-indexing kills high-write systems.

### 7.2 Index Size Becomes Huge

With 100M rows, each index can be **several GB**.

- Doesn't fit in memory
- Causes disk reads
- Slows down index seeks

> An index only helps if it's **cached in memory**.

### 7.3 Fragmentation Problem

If clustered index is random (GUID):

- Page splits
- Fragmentation
- Increased I/O
- Slow scans

At scale, this becomes very noticeable.

### 7.4 Lock Contention

In high-traffic systems:

- Many transactions modify indexed columns
- More locking
- Deadlocks increase

### 7.5 Diminishing Returns

```sql
SELECT * FROM Orders WHERE Status = 'Pending';
```

If 70% rows = Pending → index won't help much. DB may choose **full scan** instead.

### 7.6 Index Maintenance Overhead

Large systems require:

- Rebuild
- Reorganize
- Statistics updates

Maintenance windows become expensive.

### 7.7 When Is Indexing a Problem? (Summary)

| Scenario                              | Why                                    |
| ------------------------------------- | -------------------------------------- |
| Write-heavy workload (OLTP)           | Write amplification across all indexes |
| Too many indexes                      | Multiplied write cost                  |
| Low-selectivity columns               | Index not used; wasted storage         |
| Large composite indexes               | High memory and I/O cost               |
| Random clustered keys (GUIDs)         | Fragmentation and page splits          |
| Analytics workload scanning most rows | Full scans outperform index seeks      |

---

## 8. Alternatives & Complements to Indexing

> Indexing is not replaced — it is **complemented**. The right alternative depends on the workload.

### 8.1 Partitioning (Very Common)

Instead of one 100M row table → split into partitions by `CreatedAt` (monthly/yearly).

- Smaller index per partition
- Faster range queries
- Easier archiving
- Best for **time-series data**

### 8.2 Sharding

Instead of one big database → split by `UserId % N`. Each shard is smaller.

- Used in large SaaS platforms, multi-tenant systems
- Enables **horizontal scalability**

### 8.3 Denormalization

Instead of JOIN + heavy indexing → store **redundant data**.

- Reduces join cost and need for complex indexes
- Trade-off: **storage vs performance**

### 8.4 Caching (Very Powerful)

Use Redis or in-memory cache instead of hitting DB for repeated reads.

- Reduces index pressure **completely**
- Very powerful for read-heavy workloads

### 8.5 Columnar Databases (For Analytics)

If workload is analytical (scan-heavy), columnar storage outperforms traditional indexing:

- Column-store index (SQL Server)
- PostgreSQL columnar extensions
- Data warehouses (Snowflake, Redshift)
- Better for: `SUM`, `GROUP BY`, aggregations on large datasets

### 8.6 Materialized Views

Pre-compute heavy queries. Instead of indexing → store the result.

- Used in **reporting systems**

### 8.7 Strategy Selection Summary

| Strategy           | Best For                           |
| ------------------ | ---------------------------------- |
| Indexing            | Selective queries, OLTP read-heavy |
| Partitioning        | Time-based large datasets          |
| Sharding            | Horizontal scalability             |
| Caching             | Repeated read queries              |
| Columnar stores     | Analytical workloads               |
| Denormalization     | Reducing JOIN complexity           |
| Materialized Views  | Pre-computed reporting             |

---

## 9. Sharding vs Partitioning — Deep Dive

### 9.1 What is Partitioning?

Partitioning means splitting a large table into smaller pieces **inside the same database**.

- Same database, same server (usually)
- Managed by the DB engine
- From the application perspective → **still one table**

**Example — Orders table partitioned by month:**

```
Orders_2024_Jan
Orders_2024_Feb
Orders_2024_Mar
```

Application still queries normally:

```sql
SELECT * FROM Orders
WHERE CreatedAt > '2024-02-01'
```

The DB **automatically checks only relevant partitions**.

**Benefits:**

- ✅ Faster queries on large tables
- ✅ Smaller indexes per partition
- ✅ Easier archiving
- ✅ Less disk scanning

**Common Partition Types:**

| Type             | Strategy                          | Use Case                  |
| ---------------- | --------------------------------- | ------------------------- |
| Range Partition  | `PARTITION BY RANGE (CreatedAt)`  | Logs, orders, time-series |
| Hash Partition   | `PARTITION BY HASH (UserId)`      | Even distribution         |
| List Partition   | By region (US, EU, Asia)          | Geographic segmentation   |

---

### 9.2 What is Sharding?

Sharding means splitting data **across multiple databases or servers**. Each server stores only part of the data. The **application must know which shard to query**.

**Example — 100M users distributed across shards:**

```
DB1 → Users 1–10M
DB2 → Users 10M–20M
DB3 → Users 20M–30M
```

Or hash-based:

```
Shard1 → UserId % 3 = 0
Shard2 → UserId % 3 = 1
Shard3 → UserId % 3 = 2
```

**Benefits:**

- ✅ Horizontal scaling
- ✅ Handles huge datasets (billions of rows)
- ✅ Distributes load across servers

---

### 9.3 Key Differences

| Feature         | Partitioning          | Sharding              |
| --------------- | --------------------- | --------------------- |
| Level           | Database level        | Architecture level    |
| Server count    | Single server         | Multiple servers      |
| App awareness   | No                    | Yes                   |
| Complexity      | Low                   | High                  |
| Scaling type    | Vertical              | Horizontal            |

---

### 9.4 When to Use Partitioning

Choose partitioning when:

- Table has **10M–500M rows**
- Queries filter by **time or range**
- Single DB server is still powerful enough
- You want easier **data archiving**

Ideal workloads: Orders, Logs, Audit tables, Transactions.

**Example query that benefits:**

```sql
WHERE CreatedAt BETWEEN '2026-01-01' AND '2026-01-31'
```

---

### 9.5 When to Use Sharding

Choose sharding when:

- Database **cannot scale vertically** anymore
- Dataset is **hundreds of millions or billions** of rows
- Traffic is extremely high (e.g., 10k writes/sec, 50k reads/sec)
- Multiple regions or tenants

Used by: **Uber, Instagram, Netflix, Twitter**.

**Example scenario:** 1B users, 10k writes/sec, 50k reads/sec → one database cannot handle this.

---

### 9.6 Real-World Example — E-commerce (300M Orders)

**Step 1:** Use partitioning by `CreatedAt`:

```
Orders_2025
Orders_2026
Orders_2027
```

Still one database.

**Step 2 (if traffic increases):** Shard by `UserId`:

```
Shard1 → Users 1–1M
Shard2 → Users 1M–2M
Shard3 → Users 2M–3M
```

Each shard **still internally uses partitioning**. Large systems **combine both**.

---

### 9.7 Real System Design Scaling Strategy

Most companies scale in this order:

```
1️⃣ Proper indexing
2️⃣ Partitioning
3️⃣ Read replicas
4️⃣ Caching
5️⃣ Sharding (last resort — adds huge complexity)
```

---

## 10. Interview-Ready Answers

### 10.1 Optimistic vs Pessimistic Locking

> "Optimistic locking assumes conflicts are rare and checks version at update time using a RowVersion column. Pessimistic locking locks the record immediately when reading, preventing other transactions from modifying it until completion. Optimistic is more scalable, pessimistic is safer for high-conflict systems like banking."

### 10.2 Clustered vs Non-Clustered Index

> "A clustered index determines the physical order of data and stores actual rows at the leaf level. Only one clustered index is allowed per table. A non-clustered index is a separate structure that stores indexed columns and pointers to actual rows. Multiple non-clustered indexes are allowed. Clustered indexes are good for range queries, while non-clustered indexes are ideal for selective lookups."

### 10.3 Composite Indexes

> "A composite index is an index built on multiple columns. The order of columns matters due to the leftmost prefix rule. It improves performance for queries filtering or sorting on multiple columns, but excessive composite indexes can degrade write performance."

### 10.4 Designing Indexes for 10M+ Rows

> "I first analyze query patterns and frequency. I ensure a narrow, increasing clustered index to avoid fragmentation. Then I create composite indexes based on leftmost prefix rule, prioritizing equality filters before range filters. I avoid low-selectivity columns and over-indexing to protect write performance. I validate using execution plans and maintain indexes via rebuild and statistics updates. For time-series data, I consider partitioning."

### 10.5 How to Choose Indexing Strategy

> "I analyze query patterns first. I index columns used in WHERE, JOIN, and ORDER BY clauses, prioritizing high-selectivity columns. I follow the leftmost prefix rule for composite indexes and place equality conditions before range conditions. I use a narrow, increasing clustered index to avoid fragmentation and create non-clustered indexes for query optimization. I avoid indexing low-selectivity or frequently updated columns and ensure not to over-index high-write tables."

### 10.6 Why Indexing Can Be Bad for Large Data

> "Indexing increases write overhead because every insert, update, and delete must also update all indexes. For large datasets, indexes become large in size, may not fit in memory, and cause additional I/O. Over-indexing can reduce write throughput and increase locking and fragmentation. Instead of blindly adding indexes, we may use partitioning, sharding, caching, denormalization, or columnar storage depending on workload."

### 10.7 Sharding vs Partitioning

> "Partitioning splits large tables inside the same database to improve query performance and manage large datasets, typically using range or hash partitioning. It's suitable when the dataset is large but still manageable on a single server. Sharding distributes data across multiple databases or servers and is used when a single database can no longer handle the load or storage requirements. Partitioning improves query performance, while sharding enables horizontal scalability."

---

## 11. Further Topics

Advanced areas to explore for system design depth:

- **Cost-based optimizer** — How the DB chooses between index scan vs table scan
- **Key lookup elimination** — Avoiding expensive lookups back to the clustered index
- **Partial/Filtered indexes** — Indexing only a subset of rows
- **Indexing strategy for 1000 TPS systems** — Balancing read/write performance
- **Shard key selection** — Choosing the right key to distribute data evenly
- **Hot shard problem** — Uneven data distribution across shards (very common interview question)
- **Consistent hashing** — Dynamic shard management without full redistribution
- **Cross-shard queries** — Querying across multiple shards and aggregating results
- **Resharding strategy** — Redistributing data when adding/removing shards
- **Index fragmentation deep dive** — Fill factor, page splits, rebuild vs reorganize
- **Bitmap indexes** (PostgreSQL) — Efficient for low-cardinality columns
- **Index Seek vs Scan with Execution Plan** — Reading and interpreting query plans
- **How indexes affect INSERT/UPDATE** — Understanding the write penalty
- **PostgreSQL vs SQL Server indexing differences** — Platform-specific strategies
