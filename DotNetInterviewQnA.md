1️⃣ What is .NET Core?
Answer:
 A cross-platform, open-source, high-performance framework for building modern applications (web, APIs, services, cloud).

2️⃣ Difference between .NET Framework and .NET Core?
Answer:
Cross-platform support
Modular architecture
Better performance
Side-by-side versioning
Designed for cloud & microservices

3️⃣ Explain request lifecycle in ASP.NET Core
Answer:
 Request → Kestrel → Middleware pipeline → Routing → Filters → Controller → Action → Response

4️⃣ What is Middleware?
Answer:
 A component that handles HTTP requests/responses. Order matters. Can short-circuit the pipeline.

5️⃣ Use, Run, and Map difference?
Answer:
Use → calls next middleware
Run → terminates pipeline
Map → branches pipeline

6️⃣ Dependency Injection lifetimes?
Answer:
Singleton → once per app
Scoped → once per request
Transient → new instance every time

7️⃣ Why is DbContext Scoped?
Answer:
 DbContext is not thread-safe. Scoped ensures one instance per request.

8️⃣ What is async/await?
Answer:
 Used for non-blocking asynchronous code, freeing threads during IO waits.

9️⃣ Difference between async and multithreading?
Answer:
 Async is about non-blocking IO, multithreading is parallel execution.

🔟 Why .Result or .Wait() is dangerous?
Answer:
 Can cause deadlocks and thread starvation.

1️⃣1️⃣ What is Kestrel?
Answer:
 Cross-platform web server used by ASP.NET Core.

1️⃣2️⃣ What is IHostedService?
Answer:
 Used for background tasks (workers, message consumers).

🟦 B. Entity Framework Core Basics (13–25)
1️⃣3️⃣ What is EF Core?
Answer:
 An ORM that enables database access using .NET objects instead of raw SQL.

1️⃣4️⃣ What is DbContext?
Answer:
 Primary class for database interaction, tracks changes and manages transactions.

1️⃣5️⃣ What is Change Tracking?
Answer:
 EF tracks entity state changes (Added, Modified, Deleted).

1️⃣6️⃣ What happens during SaveChanges()?
Answer:
Detect changes
Generate SQL
Open transaction
Execute commands
Commit transaction

1️⃣7️⃣ Tracking vs No-Tracking?
Answer:
Tracking → updates supported
No-Tracking → faster, read-only queries

1️⃣8️⃣ Lazy Loading vs Eager Loading?
Answer:
Lazy → loads data on access (risk of N+1)
Eager → .Include() upfront

1️⃣9️⃣ What is N+1 problem?
Answer:
 Multiple queries fired for related data, hurting performance.

2️⃣0️⃣ How to avoid N+1?
Answer:
 Use eager loading, projections, or explicit loading.

2️⃣1️⃣ What is Explicit Loading?
Answer:
 Manually loading navigation properties using Entry().Load().

2️⃣2️⃣ How does EF Core handle transactions?
Answer:
 SaveChanges() creates implicit transaction. Explicit transactions needed for multi-step operations.

2️⃣3️⃣ What are migrations?
Answer:
 Versioned schema changes tracked and applied to databases.

2️⃣4️⃣ Code-First vs Database-First?
Answer:
Code-First → models define schema
DB-First → schema defines models

2️⃣5️⃣ What is Shadow Property?
Answer:
 Property not defined in entity class but tracked by EF Core.

🟦 C. Performance & Optimization (26–38)
2️⃣6️⃣ How to optimize EF queries?
Answer:
Projection with Select
AsNoTracking
Indexes
Avoid ToList() early

2️⃣7️⃣ Why projections improve performance?
Answer:
 Fetch only required columns → less memory & network cost.

2️⃣8️⃣ What is compiled query?
Answer:
 Precompiled LINQ query for repeated execution.

2️⃣9️⃣ When to avoid EF Core?
Answer:
 Bulk operations, complex SQL, extreme performance needs.

3️⃣0️⃣ EF Core bulk insert alternatives?
Answer:
 Dapper, ExecuteUpdate, ExecuteDelete, BulkExtensions.

3️⃣1️⃣ What is connection pooling?
Answer:
 Reuse DB connections to reduce overhead.

3️⃣2️⃣ Offset vs Keyset pagination?
Answer:
 Offset is slow for large data; Keyset is faster using indexed columns.

3️⃣3️⃣ What is AsSplitQuery()?
Answer:
 Breaks large joins into multiple queries to avoid cartesian explosion.

3️⃣4️⃣ What is AsSingleQuery()?
Answer:
 Executes one SQL query with joins (default behavior).

3️⃣5️⃣ How to log EF generated SQL?
Answer:
 Enable logging using ILogger or EnableSensitiveDataLogging.

3️⃣6️⃣ What is ExecuteUpdate()?
Answer:
 Performs direct SQL updates without loading entities.

3️⃣7️⃣ What is ExecuteDelete()?
Answer:
 Deletes records directly at DB level.

3️⃣8️⃣ How EF Core handles caching?
Answer:
 First-level cache via DbContext; no second-level cache by default.

🟦 D. Concurrency, Design & Advanced (39–50)
3️⃣9️⃣ What is optimistic concurrency?
Answer:
 Assumes conflicts are rare; detects conflicts using RowVersion.

4️⃣0️⃣ How to implement optimistic concurrency?
Answer:
 Use [Timestamp] or RowVersion column.

4️⃣1️⃣ How to handle concurrency conflict?
Answer:
 Retry logic or user conflict resolution.

4️⃣2️⃣ What is pessimistic concurrency?
Answer:
 Locks data until transaction completes.

4️⃣3️⃣ Repository pattern with EF?
Answer:
 Often unnecessary; EF already implements Repository + UoW.

4️⃣4️⃣ What is Unit of Work?
Answer:
 Single transaction across multiple operations.

4️⃣5️⃣ What is AutoMapper?
Answer:
 Maps entities to DTOs automatically.

4️⃣6️⃣ Why use DTOs?
Answer:
 Security, separation of concerns, performance.

4️⃣7️⃣ How to handle soft deletes?
Answer:
 Boolean flag + global query filters.

4️⃣8️⃣ What is global query filter?
Answer:
 Automatic filtering applied to all queries.

4️⃣9️⃣ EF Core with PostgreSQL specifics?
Answer:
 Supports JSONB, UUID, arrays, full-text search.

5️⃣0️⃣ Most common EF Core mistake?
Answer:
 Loading too much data + ignoring indexes.

