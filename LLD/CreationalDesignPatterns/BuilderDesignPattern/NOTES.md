# Builder Design Pattern — Comprehensive Notes

## 1. What is the Builder Pattern?

A **Creational Design Pattern** that separates the construction of a complex object from its representation, allowing the same construction process to create different representations.

**Core Idea:** Instead of a constructor with 10 parameters (telescoping constructor problem), build the object step-by-step.

---

## 2. When to Use

- Object has many optional parameters
- Object construction involves multiple steps
- You want to create different representations of the same object
- Constructor becomes unreadable with too many parameters
- You need immutable objects that require all fields set at construction time

---

## 3. Key Components

| Component | Role |
|-----------|------|
| **Builder** | Provides methods to set each part of the product |
| **Product** | The complex object being built |
| **Director** (optional) | Orchestrates the building steps in a specific order |
| **Fluent Interface** | `return this` enables method chaining |

---

## 4. Our Example — SQLQueryBuilder

```csharp
public class SQLQueryBuilder
{
    private string _table;
    private List<string> _columns = new();
    private List<string> _conditions = new();
    private string _orderBy;
    private int? _limit;

    public SQLQueryBuilder From(string table) { _table = table; return this; }
    public SQLQueryBuilder Select(params string[] columns) { _columns.AddRange(columns); return this; }
    public SQLQueryBuilder Where(string condition) { _conditions.Add(condition); return this; }
    public SQLQueryBuilder OrderBy(string column, string direction = "ASC") { ... return this; }
    public SQLQueryBuilder Limit(int count) { _limit = count; return this; }

    public string Build() { /* assembles final SQL string */ }
}
```

**Usage:**
```csharp
var query = new SQLQueryBuilder()
    .Select("name", "email")
    .From("users")
    .Where("age > 18")
    .OrderBy("name")
    .Limit(10)
    .Build();
// SELECT name, email FROM users WHERE age > 18 ORDER BY name ASC LIMIT 10
```

---

## 5. Why `return this`? (Fluent Interface)

Each setter method returns the builder itself, enabling **method chaining**:

```csharp
// WITH fluent interface (readable, expressive)
var query = new SQLQueryBuilder().Select("name").From("users").Build();

// WITHOUT fluent interface (verbose, repetitive)
var builder = new SQLQueryBuilder();
builder.Select("name");
builder.From("users");
var query = builder.Build();
```

The `Build()` method is the only one that returns the **product** (not the builder) — it's the terminal operation.

---

## 6. Builder vs Constructor vs Setters

| Approach | Problem |
|----------|---------|
| **Telescoping Constructor** | `new User(id, name, age, mobile, email, address...)` — unreadable, positional errors |
| **Setters** | Object is mutable, can exist in incomplete/invalid state |
| **Builder** | Readable, validates at `Build()`, can produce immutable objects |

---

## 7. Classic Builder (with Director)

For more complex scenarios where construction order matters:

```csharp
// Interface
public interface IQueryBuilder
{
    IQueryBuilder Select(params string[] columns);
    IQueryBuilder From(string table);
    IQueryBuilder Where(string condition);
    string Build();
}

// Concrete Builders
public class SQLQueryBuilder : IQueryBuilder { /* MySQL syntax */ }
public class PostgresQueryBuilder : IQueryBuilder { /* Postgres syntax */ }

// Director — defines construction order
public class ReportQueryDirector
{
    public string BuildUserReport(IQueryBuilder builder)
    {
        return builder
            .Select("name", "email", "created_at")
            .From("users")
            .Where("active = 1")
            .Build();
    }
}
```

The **Director** is useful when you have predefined construction recipes that should work with any builder implementation.

---

## 8. Real-World Examples

| Library/Framework | Builder Usage |
|-------------------|---------------|
| **StringBuilder** | `new StringBuilder().Append("Hello").Append(" World").ToString()` |
| **HttpClient** | `new HttpRequestMessage` with fluent config |
| **EF Core** | `modelBuilder.Entity<User>().HasKey(u => u.Id)` |
| **LINQ** | `list.Where(...).OrderBy(...).Select(...)` |
| **.NET Generic Host** | `Host.CreateDefaultBuilder().ConfigureServices(...)` |

---

## 9. Advantages & Disadvantages

**✅ Advantages:**
- Readable, self-documenting code
- Enforces valid object construction
- Supports optional parameters cleanly
- Same builder process → different representations
- Isolates complex construction logic

**❌ Disadvantages:**
- More classes/code for simple objects
- Slight overhead vs direct construction
- Can be overkill for objects with few fields

---

## 10. Builder vs Other Creational Patterns

| Pattern | Purpose |
|---------|---------|
| **Builder** | Construct complex objects step-by-step |
| **Factory Method** | Create objects without specifying exact class |
| **Abstract Factory** | Create families of related objects |
| **Prototype** | Clone existing objects |
| **Singleton** | Ensure single instance |

**Key distinction:** Builder focuses on *how* to construct (step-by-step), Factory focuses on *what* to construct (which class).

---

## 11. Best Practices

1. Make the product immutable — only the builder can set fields
2. Validate in `Build()` — throw if required fields are missing
3. Consider making the builder a nested class inside the product
4. Reset the builder after `Build()` if it's reusable
5. Use `params` keyword for variable-length inputs (like `Select(params string[] columns)`)
