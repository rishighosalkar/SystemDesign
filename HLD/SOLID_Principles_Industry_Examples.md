# SOLID Principles (Industry-Oriented C# Examples)

## S --- Single Responsibility Principle (SRP)

**Definition:** A class should have only one reason to change.

### ❌ Bad

``` csharp
public class InvoiceService
{
    public void Generate(Invoice invoice) { }
    public void SendEmail(Invoice invoice) { }
    public void Log(string message) { }
}
```

### ✅ Good

``` csharp
public class InvoiceService
{
    private readonly IEmailSender _email;
    public InvoiceService(IEmailSender email) => _email = email;

    public void Generate(Invoice invoice)
    {
        // Generate invoice
        _email.Send(invoice);
    }
}
```

``` csharp
public interface IEmailSender
{
    void Send(Invoice invoice);
}
```

**Interview takeaway:** Business logic, notifications, and logging
belong in separate classes.

------------------------------------------------------------------------

## O --- Open/Closed Principle (OCP)

**Definition:** Open for extension, closed for modification.

### ❌ Bad

``` csharp
public decimal CalculateDiscount(string customerType, decimal amount)
{
    if(customerType=="Regular") return amount*0.05m;
    if(customerType=="Premium") return amount*0.10m;
    return 0;
}
```

### ✅ Good

``` csharp
public interface IDiscountStrategy
{
    decimal Calculate(decimal amount);
}

public class PremiumDiscount : IDiscountStrategy
{
    public decimal Calculate(decimal amount) => amount * 0.10m;
}
```

**Interview takeaway:** Add new strategies instead of editing existing
logic.

------------------------------------------------------------------------

## L --- Liskov Substitution Principle (LSP)

**Definition:** Derived types must be replaceable for their base types.

### ❌ Bad

``` csharp
public class FileStorage
{
    public virtual void Delete(string path) { }
}

public class ReadOnlyStorage : FileStorage
{
    public override void Delete(string path)
    {
        throw new NotSupportedException();
    }
}
```

### ✅ Good

``` csharp
public interface IReadableStorage
{
    Stream Read(string path);
}

public interface IWritableStorage : IReadableStorage
{
    void Delete(string path);
}
```

**Interview takeaway:** Don't inherit behavior you cannot honor.

------------------------------------------------------------------------

## I --- Interface Segregation Principle (ISP)

**Definition:** Clients shouldn't depend on methods they don't use.

### ❌ Bad

``` csharp
public interface ICloudStorage
{
    void Upload();
    void Download();
    void GenerateThumbnail();
}
```

### ✅ Good

``` csharp
public interface IFileStorage
{
    void Upload();
    void Download();
}

public interface IThumbnailGenerator
{
    void GenerateThumbnail();
}
```

**Interview takeaway:** Prefer small, focused interfaces.

------------------------------------------------------------------------

## D --- Dependency Inversion Principle (DIP)

**Definition:** Depend on abstractions, not concrete implementations.

### ❌ Bad

``` csharp
public class OrderService
{
    private readonly SqlRepository _repo = new();
}
```

### ✅ Good

``` csharp
public interface IOrderRepository
{
    Task SaveAsync(Order order);
}

public class OrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo)
    {
        _repo = repo;
    }
}
```

``` csharp
builder.Services.AddScoped<IOrderRepository, SqlRepository>();
```

**Interview takeaway:** Enables testing, loose coupling, and easy
implementation swaps.

------------------------------------------------------------------------

# Quick Summary

  Principle   Rule                           Industry Example
  ----------- ------------------------------ --------------------------------------
  SRP         One responsibility             Invoice generation vs email sender
  OCP         Extend, don't modify           Discount strategies
  LSP         Child honors parent contract   Read-only vs writable storage
  ISP         Small interfaces               File storage vs thumbnail generation
  DIP         Depend on interfaces           Repository injection via DI

## ASP.NET Core Mapping

-   **Controllers**: HTTP handling only (SRP)
-   **Services**: Business logic
-   **Repositories**: Data access
-   **DI Container**: Implements DIP
-   **Strategy Pattern**: Common OCP implementation
-   **CQRS/MediatR**: Naturally supports SRP and OCP
