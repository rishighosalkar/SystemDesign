# SQL & Database Concepts — Study Notes

---

## 1. Clustered Index vs Non-Clustered Index

### Clustered Index
- Physically reorders table data on disk to match index order
- **Only ONE per table** (data can only be sorted one way)
- Leaf nodes **ARE** the actual data pages
- Best for: range queries (`BETWEEN`, `<`, `>`), `ORDER BY`, primary keys

### Non-Clustered Index
- Separate structure with pointers back to data rows
- **Multiple allowed** per table (up to 999 in SQL Server)
- Leaf nodes contain index key + pointer (RID for heap, or clustered key)
- Best for: `WHERE`, `JOIN`, lookups on non-PK columns

### When to Choose

| Scenario | Use |
|---|---|
| Primary key / natural ordering | Clustered |
| Range scans (`WHERE date BETWEEN ...`) | Clustered |
| Frequent lookups on non-PK columns | Non-Clustered |
| Columns in WHERE/JOIN that aren't PK | Non-Clustered |

---

## 2. Covering Index

### What Is a Covering Index?
A covering index is a non-clustered index that contains **all the columns** a query needs — the query engine can satisfy the entire query from the index alone without ever going back to the base table. The execution plan shows this as an **"Index Seek"** (or scan) with no Key Lookup.

Think of it like a textbook's index that not only tells you the page number but also includes the actual answer — you never need to flip to the page.

### Why It Helps
Without a covering index, a non-clustered index seek finds the matching rows but only has the indexed columns. For any other column in your `SELECT`, SQL Server must perform a **Key Lookup** — it takes the clustered key (or RID) from the non-clustered index leaf and goes back to the clustered index (or heap) to fetch the remaining columns. This is essentially a second random I/O **per row**.

- 10 matching rows = 10 Key Lookups = 10 random I/Os
- 10,000 matching rows = 10,000 Key Lookups → optimizer may give up and do a full table scan instead

A covering index eliminates all of this by storing the extra columns right in the index leaf pages.

**Performance impact:**
- Eliminates Key Lookups (the single biggest perf win for selective queries)
- Reduces I/O dramatically — one structure instead of two
- Reduces lock contention — fewer pages touched = fewer locks acquired
- Can turn a slow query into a sub-millisecond query

### How It Works

```sql
-- Query we want to optimize:
SELECT OrderDate, TotalAmount FROM Orders WHERE CustomerId = 42

-- Without covering index (index on CustomerId only):
-- Step 1: Index Seek on CustomerId = 42 → gets clustered key (OrderId)
-- Step 2: Key Lookup into clustered index using OrderId → fetches OrderDate, TotalAmount
-- Problem: Step 2 happens PER ROW — expensive at scale

-- With covering index:
CREATE NONCLUSTERED INDEX IX_Orders_Customer 
ON Orders(CustomerId) 
INCLUDE (OrderDate, TotalAmount)

-- Step 1: Index Seek on CustomerId = 42 → leaf page already has OrderDate, TotalAmount
-- Step 2: NONE — query fully satisfied from index ✅
-- Execution plan shows: "Index Seek" only, no Key Lookup
```

`INCLUDE` columns are stored **only at leaf level**, not in intermediate B-tree nodes. This means they don't increase the B-tree depth (keeping seeks fast) but are available when the engine reads the leaf page.

### When to Use
- Execution plan shows a **Key Lookup** consuming significant cost (often 90%+ of query cost)
- High-frequency queries selecting the same columns repeatedly
- Read-heavy OLTP workloads where every millisecond matters
- Queries returning few rows but needing columns not in the index

### Rules for Creating

**Rule 1: Key columns = filter/sort/join columns**
```sql
-- Query: WHERE CustomerId = 42 ORDER BY OrderDate
-- Key: (CustomerId, OrderDate)  |  INCLUDE: (TotalAmount)
CREATE INDEX IX ON Orders(CustomerId, OrderDate) INCLUDE (TotalAmount)
```

**Rule 2: INCLUDE columns = selected-only columns**
Columns only in `SELECT` go in `INCLUDE`, not in the key.

**Rule 3: Don't over-cover**
Including too many columns duplicates the table — massive storage + write overhead.

**Rule 4: Cover top 5-10 most expensive queries, not all queries**

**Rule 5: Covering index on a heap (no clustered index) is even more critical**
RID Lookups into unordered heaps are worse than Key Lookups.

---

## 3. Composite Index

### What
An index on **two or more columns**, sorted by first column, then second within first, etc.
Like a phone book: sorted by last name, then first name.

### How It Works

```sql
CREATE NONCLUSTERED INDEX IX_Orders_Cust_Date 
ON Orders(CustomerId, OrderDate)
```

Leaf level (logically):
```
| CustomerId | OrderDate   | Pointer |
|------------|-------------|---------|
| 1          | 2024-01-05  | →       |
| 1          | 2024-03-12  | →       |
| 2          | 2024-02-01  | →       |
| 2          | 2024-06-15  | →       |
```

### The Leftmost Prefix Rule (MOST IMPORTANT)

```sql
-- Index: (CustomerId, OrderDate, Status)

-- ✅ CAN use index
WHERE CustomerId = 42
WHERE CustomerId = 42 AND OrderDate = '2024-01-01'
WHERE CustomerId = 42 AND OrderDate = '2024-01-01' AND Status = 'Shipped'

-- ❌ CANNOT use index
WHERE OrderDate = '2024-01-01'                    -- skips first column
WHERE Status = 'Shipped'                           -- skips first two columns
WHERE OrderDate = '2024-01-01' AND Status = 'Shipped'  -- skips first column
```

### Column Order Rules

**Rule 1: Equality columns first, range columns last**
```sql
-- Query: WHERE CustomerId = 42 AND OrderDate > '2024-01-01'
-- GOOD: (CustomerId, OrderDate) — seek on both
-- BAD:  (OrderDate, CustomerId) — range on OrderDate stops using CustomerId
```

**Rule 2: High selectivity columns first**
Column that filters out the most rows goes first.

**Rule 3: Don't exceed 4-5 key columns**
Wider keys = deeper B-tree = slower writes.

**Rule 4: Match your most frequent query patterns**
Design the index for the queries you actually run.

---

## 4. Why Multiple Indexes Are Bad

- **Write overhead**: Every INSERT/UPDATE/DELETE updates ALL indexes
- **Storage cost**: Each index is a separate B-tree on disk
- **Maintenance**: More fragmentation, more statistics to update
- **Optimizer confusion**: Too many indexes → suboptimal plan choices
- **Lock contention**: Index updates acquire locks

**Rule of thumb**: Index for read patterns, audit regularly, drop unused indexes.

---

## 5. Index Seek, Index Scan, Table Scan, Key Lookup

### Table Scan — O(n), worst
Reads **every single row** in the table from start to finish. Happens when there's no useful index, or the query returns most of the table anyway.

```sql
-- Orders table has NO index on Status
SELECT * FROM Orders WHERE Status = 'Shipped'
-- Execution plan: Table Scan
-- SQL Server reads all 1M rows to find the ones with Status = 'Shipped'
```

Also called **Clustered Index Scan** if the table has a clustered index (since the table IS the clustered index).

### Index Scan — O(n) on index, better than table scan
Reads **every leaf page** of a non-clustered index. Faster than a table scan only because the index is narrower (fewer columns = fewer pages to read).

```sql
-- Index exists on CustomerId, but query has no WHERE filter
SELECT CustomerId FROM Orders ORDER BY CustomerId
-- Execution plan: Index Scan on IX_Orders_CustomerId
-- Reads all leaf pages of the index (much smaller than full table)
-- Still O(n) but on a smaller structure
```

Also happens when the `WHERE` clause exists but isn't **sargable**:
```sql
-- Index on OrderDate, but function on column prevents seek
SELECT * FROM Orders WHERE YEAR(OrderDate) = 2024
-- Execution plan: Index Scan (not seek!) — must evaluate YEAR() on every row
```

### Index Seek — O(log n), best
Navigates the **B-tree** from root → intermediate → leaf to land directly on matching rows. Only reads the pages it needs.

```sql
-- Index on CustomerId
SELECT * FROM Orders WHERE CustomerId = 42
-- Execution plan: Index Seek on IX_Orders_CustomerId
-- B-tree depth ~3 levels for millions of rows → reads ~3 pages to find the match
-- Then Key Lookup for remaining columns (unless covering index)
```

Range seeks also use the B-tree:
```sql
-- Clustered index on OrderDate
SELECT * FROM Orders WHERE OrderDate BETWEEN '2024-01-01' AND '2024-01-31'
-- Execution plan: Clustered Index Seek
-- Seeks to '2024-01-01' in B-tree, then reads sequentially until '2024-01-31'
```

### Key Lookup — expensive at scale
After a non-clustered index seek finds matching rows, SQL Server only has the indexed columns. For any other column in the query, it must go **back to the clustered index** (or heap) to fetch them — one lookup per row.

```sql
-- Non-clustered index on CustomerId (only)
SELECT OrderDate, TotalAmount FROM Orders WHERE CustomerId = 42
-- Execution plan:
--   1. Index Seek on IX_CustomerId → finds 50 matching rows (has CustomerId + clustered key)
--   2. Key Lookup × 50 → goes to clustered index 50 times to get OrderDate, TotalAmount
-- The 50 Key Lookups often cost MORE than the seek itself
```

**Fix:** Create a covering index to eliminate the Key Lookup entirely (see Section 2).

### Summary

| Operation | What It Does | Performance | When It Happens |
|---|---|---|---|
| **Table Scan** | Reads every row in the table | O(n) — worst | No useful index, or query needs most rows |
| **Index Scan** | Reads every leaf page of an index | O(n) on smaller structure | Index exists but can't seek (non-sargable, no filter) |
| **Index Seek** | B-tree navigation to matching rows | O(log n) — best | Sargable WHERE on indexed column |
| **Key Lookup** | Fetches missing columns from clustered index | O(k) per row — adds up fast | Non-clustered seek + SELECT has non-indexed columns |

**Ranking**: Index Seek > Index Scan > Table Scan

Key Lookups are what covering indexes eliminate.

---

## 6. Fragmentation

### What Is Fragmentation?
Fragmentation is when index pages become inefficiently organized — either pages have wasted empty space, or pages are stored out of logical order on disk. This forces SQL Server to do more I/O to read the same amount of data.

Think of it like a book where pages are either half-empty (internal) or shuffled out of order (external).

### Internal Fragmentation
- Pages not fully filled — wasted space **within** pages
- A page is 8KB in SQL Server. If a page is only 40% full, 60% is wasted.

**How it's caused:**
- `DELETE` statements remove rows, leaving gaps in pages that aren't automatically reclaimed
- `UPDATE` on variable-length columns (`VARCHAR`, `NVARCHAR`) — if the new value is smaller, the row shrinks and leaves a gap; if larger, the row may move entirely, leaving the old space empty
- Low `FILLFACTOR` setting (intentionally leaves space for future inserts, but too low = wasted space)

**Impact:** More pages to read for the same data → more I/O, more buffer pool memory consumed.

### External (Logical) Fragmentation
- Logical order of pages in the index doesn't match physical order on disk
- Pages are scattered — sequential reads become random I/O

**How it's caused:**
- **Page splits** — the #1 cause. When an INSERT or UPDATE needs to go into a page that's already full, SQL Server:
  1. Allocates a new page (often not physically adjacent)
  2. Moves ~50% of rows from the full page to the new page
  3. Inserts the new row
  4. Now pages are out of order on disk
- Frequent inserts into the **middle** of an index (non-sequential keys like GUIDs) cause constant page splits
- Sequential keys (identity columns) mostly append to the end, causing minimal external fragmentation

**Impact:** Range scans and ordered reads become random I/O instead of sequential I/O — significantly slower on HDDs.

### How to Detect Fragmentation

```sql
SELECT 
    S.name AS SchemaName,
    T.name AS TableName,
    I.name AS IndexName,
    IPS.avg_fragmentation_in_percent,
    IPS.avg_page_space_used_in_percent,  -- low = internal fragmentation
    IPS.page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'SAMPLED') IPS
JOIN sys.indexes I ON IPS.object_id = I.object_id AND IPS.index_id = I.index_id
JOIN sys.tables T ON I.object_id = T.object_id
JOIN sys.schemas S ON T.schema_id = S.schema_id
WHERE IPS.page_count > 1000  -- skip tiny indexes
ORDER BY IPS.avg_fragmentation_in_percent DESC
```

- `avg_fragmentation_in_percent` → external fragmentation (out-of-order pages)
- `avg_page_space_used_in_percent` → inverse of internal fragmentation (higher = better)

### How to Resolve Fragmentation

| Fragmentation % | Action |
|---|---|
| < 5% | Do nothing |
| 5–30% | `ALTER INDEX IX_Name ON Table REORGANIZE` |
| > 30% | `ALTER INDEX IX_Name ON Table REBUILD` |

**REORGANIZE vs REBUILD:**

| | REORGANIZE | REBUILD |
|---|---|---|
| How | Physically reorders leaf pages in-place | Drops and recreates the entire index |
| Online | Always online | Offline by default, `ONLINE = ON` available (Enterprise) |
| Logging | Minimal — can be stopped/resumed | Fully logged |
| Locks | Lightweight | Heavier — schema modification lock at start/end |
| Effectiveness | Fixes moderate fragmentation | Fixes severe fragmentation, resets page fullness |
| Statistics | Does NOT update statistics | Automatically updates statistics |

### Prevention Strategies
- Use **sequential keys** (identity, sequences) for clustered indexes to avoid mid-index inserts
- Set appropriate **FILLFACTOR** (e.g., 80-90%) on indexes with frequent mid-page inserts — leaves room to reduce page splits
- Avoid **GUIDs as clustered keys** — random values cause constant page splits
- Schedule regular index maintenance jobs (weekly/nightly depending on write volume)

**Note**: Fragmentation is less impactful on SSDs (no seek penalty for random I/O) but still affects buffer pool efficiency — more pages in memory for the same data.


---

## 7. Row-Level Lock vs Table-Level Lock

### Why Locking Exists
When multiple transactions access the same data concurrently, locks prevent them from corrupting each other's work. Without locks, two transactions could update the same row simultaneously — one overwrites the other's changes (lost update), or one reads half-written data (dirty read).

SQL Server uses locks at different granularities — the trade-off is always **concurrency vs overhead**.

### Row-Level Lock
Locks only the **specific row** being read or modified. Other transactions can freely access every other row in the table.

```sql
-- Transaction A:
UPDATE Orders SET Status = 'Shipped' WHERE OrderId = 42
-- Locks ONLY row with OrderId = 42

-- Transaction B (runs concurrently, no blocking):
UPDATE Orders SET Status = 'Cancelled' WHERE OrderId = 99
-- Different row → no conflict → executes immediately

-- Transaction C (BLOCKED until A commits/rollbacks):
UPDATE Orders SET Status = 'Returned' WHERE OrderId = 42
-- Same row → must wait for A's lock to release
```

- Maximum concurrency — hundreds of users can work on different rows simultaneously
- Higher memory overhead — each locked row needs a lock object in memory (~96 bytes per lock)
- Best for: **OLTP** — many concurrent users doing small, targeted operations (e-commerce, banking)

### Table-Level Lock
Locks the **entire table**. No other transaction can read or write any row until the lock is released.

```sql
-- Transaction A:
BEGIN TRANSACTION
-- Bulk loading 500K rows with TABLOCK hint
INSERT INTO Orders WITH (TABLOCK) SELECT * FROM StagingOrders
-- Entire Orders table is locked

-- Transaction B (BLOCKED — even though it touches a completely different row):
SELECT * FROM Orders WHERE OrderId = 1
-- Must wait until A commits, even though row 1 isn't being inserted
```

- Minimal overhead — one lock object for the whole table instead of thousands
- Blocks ALL other access — terrible for concurrency
- Best for: **Bulk operations** — mass inserts, ETL jobs, index rebuilds, maintenance windows

### Page-Level Lock (Middle Ground)
SQL Server also locks at the **page level** (8KB page, ~rows on one page). It's a middle ground — less overhead than row locks, less blocking than table locks. You rarely control this directly; the engine chooses it.

### Lock Escalation
SQL Server automatically escalates from fine-grained to coarse-grained locks to save memory.

```
Row Locks → Page Locks → Table Lock
```

When a single transaction accumulates **~5,000+ locks** on one table, SQL Server escalates to a table lock. This is automatic and can cause unexpected blocking.

```sql
-- This UPDATE touches 100K rows → starts with row locks
UPDATE Orders SET Discount = 0.1 WHERE OrderDate < '2023-01-01'
-- After ~5000 row locks → SQL Server escalates to table lock
-- Now EVERY other transaction on Orders is blocked, even unrelated ones
```

**How to handle escalation:**
- Process large updates in **batches** to stay under the threshold:
```sql
WHILE 1 = 1
BEGIN
    UPDATE TOP (1000) Orders SET Discount = 0.1 
    WHERE OrderDate < '2023-01-01' AND Discount != 0.1
    IF @@ROWCOUNT = 0 BREAK
END
```
- Use `ALTER TABLE Orders SET (LOCK_ESCALATION = DISABLE)` to prevent escalation (use cautiously — high memory usage)

### Lock Types (Shared vs Exclusive)

| Lock Type | Acquired By | Allows Others To | Conflicts With |
|---|---|---|---|
| **Shared (S)** | `SELECT` (reads) | Also read (shared locks) | Exclusive locks |
| **Exclusive (X)** | `INSERT`, `UPDATE`, `DELETE` | Nothing — full block | Everything |
| **Update (U)** | First phase of `UPDATE` (find the row) | Shared locks | Exclusive and other Update locks |

- Readers don't block readers (shared + shared = OK)
- Writers block everyone (exclusive blocks all)
- This is why read-heavy workloads scale well, but write contention is the bottleneck

### When to Use What

| Scenario | Lock Level | Why |
|---|---|---|
| Single row update by PK | Row | Minimal blocking, fast |
| Bulk insert of 1M rows | Table (`TABLOCK`) | One lock instead of 1M, faster bulk load |
| Read-heavy OLTP | Row | Max concurrency for concurrent users |
| Nightly ETL batch job | Table | No users online, speed > concurrency |
| Large UPDATE across many rows | Row + batching | Avoid lock escalation surprises |

---

## 8. Isolation Levels in SQL

### Anomalies

- **Dirty Read**: Reading uncommitted data (might be rolled back)
- **Non-Repeatable Read**: Same row, two reads, different values (another txn modified it)
- **Phantom Read**: Same query, two executions, different rows (another txn inserted/deleted)

### Levels (lowest → highest isolation)

| Level | Dirty Read | Non-Repeatable Read | Phantom Read | How |
|---|---|---|---|---|
| **READ UNCOMMITTED** | ✅ | ✅ | ✅ | No locks on reads. Sees uncommitted data. |
| **READ COMMITTED** (default) | ❌ | ✅ | ✅ | Shared locks released immediately after read. |
| **REPEATABLE READ** | ❌ | ❌ | ✅ | Shared locks held until txn ends. |
| **SERIALIZABLE** | ❌ | ❌ | ❌ | Range locks prevent inserts into range. |
| **SNAPSHOT** (MVCC) | ❌ | ❌ | ❌ | Point-in-time snapshot via row versioning. Readers don't block writers. |

### When to Use

| Level | Use Case |
|---|---|
| READ UNCOMMITTED | Reporting on large tables, approximate data OK (`WITH (NOLOCK)`) |
| READ COMMITTED | General OLTP — good default |
| REPEATABLE READ | Consistent reads within txn (financial calculations) |
| SERIALIZABLE | Absolute consistency (seat booking, inventory deduction) |
| SNAPSHOT | Serializable-like consistency without blocking (read-heavy + occasional writes) |

---

## 9. Sharding vs Partitioning

| | Partitioning | Sharding |
|---|---|---|
| **Scope** | Single database | Multiple databases/servers |
| **Data split** | Table split by key | Data distributed across nodes |
| **Transparency** | Transparent to app | App must know which shard |
| **Scaling** | Vertical (same machine) | Horizontal (multiple machines) |

### When to Use Partitioning
- Single server can handle the load
- Want query performance improvement on a specific key
- Need easier maintenance (drop old partitions vs deleting millions of rows)
- Want partition pruning (optimizer skips irrelevant partitions)

### When to Use Sharding
- Single server **can't** handle the load
- Need horizontal **write** scaling
- Need geographic distribution (US shard, EU shard)
- Exhausted vertical scaling + partitioning

**Always try partitioning first.** Sharding adds massive complexity (cross-shard joins, distributed transactions, rebalancing).

---

## 10. Sharding Techniques

### Technique 1: Range-Based Sharding

Split by **value ranges** of shard key.

```
Shard 1: CustomerId 1 - 1,000,000
Shard 2: CustomerId 1,000,001 - 2,000,000
Shard 3: CustomerId 2,000,001 - 3,000,000
```

**Routing**: Check which range the key falls in → route to that shard.

| Pros | Cons |
|---|---|
| Simple to implement | **Hotspots** — last shard gets all new writes |
| Range queries efficient (hit few shards) | Uneven distribution if data not uniform |
| Easy to add new shards | Rebalancing requires moving data |

**Best for**: Time-series data (shard by month/year), predictable sequential growth.

```
Example — Logging system:
  Shard "2024-Q1": Jan-Mar logs
  Shard "2024-Q2": Apr-Jun logs
  → Old shards become read-only, new shard handles writes
```

### Technique 2: Hash-Based Sharding

Apply **hash function** to shard key, modulo by number of shards.

```
Shard = hash(ShardKey) % NumberOfShards

hash(7823) = 48291037 → 48291037 % 4 = 1 → Shard 1
hash(7824) = 93710284 → 93710284 % 4 = 0 → Shard 0
```

| Pros | Cons |
|---|---|
| Even distribution, no hotspots | Range queries hit ALL shards (scatter-gather) |
| Simple deterministic routing | Resharding is painful (changing N rehashes everything) |
| Works with any key type | |

**Mitigating resharding — Consistent Hashing:**
```
Traditional: hash(key) % 4 → adding shard 5 means hash(key) % 5 → almost all keys move

Consistent Hashing: keys and shards mapped to a ring (0-360°)
  → Key goes to nearest shard clockwise
  → Adding a shard only moves keys between the new shard and its neighbor
  → Only ~1/N keys move instead of almost all
```

**Best for**: User data, session stores, any workload needing even distribution.

### Technique 3: Directory-Based Sharding

A **lookup table** (directory) maps each shard key to its shard location.

```
| CustomerId | Shard    |
|------------|----------|
| 1001       | Shard-A  |
| 1002       | Shard-C  |
| 1003       | Shard-B  |
```

**Routing**: Query the directory → get shard → route request.

| Pros | Cons |
|---|---|
| Maximum flexibility — any key to any shard | Directory is a **single point of failure** |
| Easy rebalancing — just update the directory | Extra hop for every query (latency) |
| No constraints on distribution logic | Directory itself needs high availability |

**Best for**: Complex multi-tenant systems, uneven data distribution, custom placement rules.

### Technique 4: Geo-Based Sharding

Shard by **geographic region**.

```
Shard-US: Users in North America
Shard-EU: Users in Europe
Shard-APAC: Users in Asia-Pacific
```

| Pros | Cons |
|---|---|
| Low latency (data near users) | Cross-region queries are slow |
| Data residency compliance (GDPR) | Uneven load if regions differ in size |
| Natural isolation | Users who travel may hit wrong shard |

**Best for**: Global apps with data residency requirements.

### Technique 5: Entity/Relationship-Based Sharding

Keep **related entities together** on the same shard.

```
Shard by TenantId:
  Shard 1: Tenant A's Users, Orders, Products, Invoices
  Shard 2: Tenant B's Users, Orders, Products, Invoices
```

| Pros | Cons |
|---|---|
| JOINs within a tenant stay local | Large tenants can create hotspots |
| No cross-shard transactions needed | Tenant migration between shards is complex |
| Simple application logic | |

**Best for**: Multi-tenant SaaS applications.

### Sharding Technique Comparison

| Technique | Distribution | Range Queries | Rebalancing | Complexity |
|---|---|---|---|---|
| Range | Uneven risk | ✅ Efficient | Hard | Low |
| Hash | Even | ❌ Scatter-gather | Very Hard (use consistent hashing) | Low |
| Directory | Flexible | Depends | Easy (update lookup) | Medium |
| Geo | By region | Regional only | Medium | Medium |
| Entity | By relationship | Within entity | Hard | Low |

---

## 11. SQL Query Optimization

### Step 1: Read the Execution Plan
Always start with `SET STATISTICS IO ON` and actual execution plan. Optimize what the plan tells you.

### Step 2: Indexing
- Add indexes for columns in `WHERE`, `JOIN`, `ORDER BY`, `GROUP BY`
- Use covering indexes to eliminate Key Lookups
- Remove unused indexes (`sys.dm_db_index_usage_stats`)

### Step 3: Write Sargable Queries

```sql
-- ❌ BAD: Function on column kills index seek
WHERE YEAR(OrderDate) = 2024

-- ✅ GOOD: Sargable — enables index seek
WHERE OrderDate >= '2024-01-01' AND OrderDate < '2025-01-01'
```

### Step 4: Select Only What You Need

```sql
-- ❌ BAD
SELECT * FROM Orders WHERE CustomerId = 5

-- ✅ GOOD: Enables covering index
SELECT OrderId, TotalAmount FROM Orders WHERE CustomerId = 5
```

### Step 5: Prefer JOINs Over Correlated Subqueries

```sql
-- ❌ BAD: Subquery runs per row
SELECT *, (SELECT COUNT(*) FROM OrderItems oi WHERE oi.OrderId = o.Id) FROM Orders o

-- ✅ GOOD: JOIN once
SELECT o.*, COUNT(oi.Id)
FROM Orders o LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
GROUP BY o.Id
```

### Common Anti-Patterns

| Anti-Pattern | Problem | Fix |
|---|---|---|
| `SELECT *` | Fetches unnecessary data | Select only needed columns |
| Functions on indexed columns | Non-sargable, causes scan | Keep column bare |
| Implicit conversions (`WHERE VarcharCol = 123`) | Causes scan | Match data types |
| `NOT IN` with NULLs | Unexpected results + poor perf | Use `NOT EXISTS` |
| Missing JOIN predicates | Cartesian product | Always specify ON clause |
| `LIKE '%value%'` | Leading wildcard kills index | Use full-text search |
| Cursor loops | Row-by-row processing | Rewrite as set-based |
| `DISTINCT` to hide duplicates | Masks bad JOINs | Fix the JOIN logic |

### Step 6: Pagination

```sql
-- ❌ BAD: OFFSET scans and discards rows
SELECT * FROM Orders ORDER BY Id OFFSET 100000 ROWS FETCH NEXT 10 ROWS ONLY

-- ✅ GOOD: Keyset pagination — seeks directly
SELECT * FROM Orders WHERE Id > @lastSeenId ORDER BY Id FETCH NEXT 10 ROWS ONLY
```

### Step 7: Temp Tables vs Table Variables

| | Temp Table | Table Variable |
|---|---|---|
| Statistics | ✅ Yes | ❌ No (assumes 1 row) |
| Indexes | ✅ Yes | Limited |
| Use when | Large datasets, complex queries | Small datasets (< 1000 rows) |

---

*End of Notes*
