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

### What
An index containing **all columns** a query needs — engine never touches the base table.

### How It Works

```sql
-- Without covering index (index on CustomerId only):
-- Step 1: Index Seek on CustomerId = 42 → gets clustered key
-- Step 2: Key Lookup into clustered index → fetches remaining columns (EXPENSIVE)

-- With covering index:
CREATE NONCLUSTERED INDEX IX_Orders_Customer 
ON Orders(CustomerId) 
INCLUDE (OrderDate, TotalAmount)

-- Step 1: Index Seek → leaf page already has OrderDate, TotalAmount
-- Step 2: NONE — query fully satisfied from index
```

`INCLUDE` columns are stored **only at leaf level**, not in intermediate B-tree nodes.

### When to Use
- Key Lookup in execution plan consuming significant cost
- High-frequency queries selecting same columns repeatedly
- Read-heavy OLTP workloads
- Queries returning few rows but needing columns not in the index

### Why
- Eliminates Key Lookups (biggest perf win for selective queries)
- Reduces I/O dramatically
- Reduces lock contention (fewer pages = fewer locks)

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

| Operation | What It Does | Performance |
|---|---|---|
| **Table Scan** | Reads EVERY row in the table (heap) | O(n) — worst |
| **Index Scan** | Reads EVERY leaf page of an index | Better than table scan if index is narrower |
| **Index Seek** | Navigates B-tree directly to matching rows | O(log n) — best |
| **Key Lookup** | After non-clustered seek, goes back to clustered index for missing columns | Expensive at scale |

**Ranking**: Index Seek > Index Scan > Table Scan

Key Lookups are what covering indexes eliminate.

---

## 6. Fragmentation

### Internal Fragmentation
- Pages not fully filled — wasted space within pages
- Caused by: variable-length updates, deletes leaving gaps
- Result: more pages read for same data

### External (Logical) Fragmentation
- Pages out of physical order on disk
- Caused by: page splits during inserts into full pages
- Result: sequential reads become random I/O

### Solutions

| Fragmentation % | Action |
|---|---|
| < 5% | Do nothing |
| 5–30% | `ALTER INDEX REORGANIZE` (online, lightweight) |
| > 30% | `ALTER INDEX REBUILD` (heavier, can be online) |

**Note**: Less impactful on SSDs but still affects buffer pool efficiency.


---

## 7. Row-Level Lock vs Table-Level Lock

### Row-Level Lock
- Locks individual rows
- Maximum concurrency — others can access other rows
- Higher overhead (more lock objects in memory)
- Use for: **OLTP** — many concurrent users, small targeted operations

### Table-Level Lock
- Locks entire table
- Minimal overhead but blocks all other access
- Use for: **Bulk operations** — mass inserts, ETL, maintenance

### Lock Escalation
SQL Server auto-escalates row → page → table lock when a transaction holds ~5000+ locks.

| Scenario | Lock Level |
|---|---|
| Single row update by PK | Row |
| Bulk insert of 1M rows | Table |
| Read-heavy OLTP | Row |
| Nightly ETL batch job | Table |

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
