# Factory Method & Abstract Factory Design Patterns

## Table of Contents
- [Factory Method Pattern](#factory-method-pattern)
- [Abstract Factory Pattern](#abstract-factory-pattern)
- [Key Differences](#key-differences)
- [When to Use What](#when-to-use-what)
- [Anti-Patterns & Pitfalls](#anti-patterns--pitfalls)
- [Real-World Usage in Industry](#real-world-usage-in-industry)

---

## Factory Method Pattern

### Intent
Define an interface for creating an object, but let **subclasses** decide which class to instantiate. Factory Method lets a class defer instantiation to subclasses.

### The Problem It Solves
Without Factory Method:
```csharp
// Client is tightly coupled to concrete classes
IPaymentProcessor processor;
if (type == "credit") processor = new CreditCardProcessor();
else if (type == "upi") processor = new UpiProcessor();
else if (type == "paypal") processor = new PayPalProcessor();
// Every new payment type = modify this code (violates Open/Closed Principle)
```

With Factory Method:
```csharp
// Client depends only on abstraction
PaymentFactory factory = new UpiFactory();
factory.MakePayment(500); // doesn't know or care what concrete processor is used
```

### UML Structure
```
┌─────────────────────┐          ┌─────────────────────┐
│   PaymentFactory    │          │  IPaymentProcessor  │
│   (Creator)         │          │  (Product)          │
├─────────────────────┤          ├─────────────────────┤
│ + CreateProcessor() │─creates─▶│ + ProcessPayment()  │
│ + MakePayment()     │          └─────────────────────┘
└─────────┬───────────┘                    ▲
          │ extends                        │ implements
          ▼                                │
┌─────────────────────┐          ┌─────────────────────┐
│   UpiFactory        │          │   UpiProcessor      │
├─────────────────────┤          ├─────────────────────┤
│ + CreateProcessor() │─creates─▶│ + ProcessPayment()  │
└─────────────────────┘          └─────────────────────┘
```

### Participants
| Role | In Our Example | Responsibility |
|------|---------------|----------------|
| Product | `IPaymentProcessor` | Defines the interface of objects the factory creates |
| ConcreteProduct | `CreditCardProcessor`, `UpiProcessor`, `PayPalProcessor` | Implements the Product interface |
| Creator | `PaymentFactory` | Declares the factory method, may provide default impl |
| ConcreteCreator | `CreditCardFactory`, `UpiFactory`, `PayPalFactory` | Overrides factory method to return a ConcreteProduct |

### SOLID Principles Satisfied
- **S** — Each factory has a single responsibility: creating one type of processor
- **O** — Open for extension (new factory + product), closed for modification (no if-else changes)
- **L** — Any ConcreteCreator can substitute the abstract PaymentFactory
- **D** — Client depends on abstractions (PaymentFactory, IPaymentProcessor), not concretions

### When the Factory Method Shines
1. **You don't know the exact type at compile time** — the decision is deferred to runtime or configuration
2. **You want to isolate construction logic** — especially when creation involves setup, validation, or caching
3. **You want to enable extensibility** — third-party developers can add new products without modifying your code
4. **Testing** — you can inject mock factories in unit tests

---

## Abstract Factory Pattern

### Intent
Provide an interface for creating **families of related or dependent objects** without specifying their concrete classes.

### The Problem It Solves
Imagine building a cross-platform app. You need:
- A Button that looks native on each OS
- A Checkbox that looks native on each OS
- **They must be consistent** — you can't accidentally pair a Windows Button with a macOS Checkbox

Without Abstract Factory:
```csharp
// Scattered, error-prone, no consistency guarantee
IButton button = isWindows ? new WindowsButton() : new MacButton();
ICheckbox checkbox = isWindows ? new WindowsCheckbox() : new MacCheckbox();
// What if someone writes: new WindowsButton() + new MacCheckbox()? Bug.
```

With Abstract Factory:
```csharp
// One decision point. Consistency guaranteed.
IUIFactory factory = isWindows ? new WindowsUIFactory() : new MacUIFactory();
var app = new Application(factory); // impossible to mix families
```

### UML Structure
```
┌───────────────┐       ┌──────────┐    ┌────────────┐
│  IUIFactory   │       │ IButton  │    │ ICheckbox  │
├───────────────┤       └────▲─────┘    └─────▲──────┘
│ CreateButton()│──creates───┘                │
│ CreateCheckbox│──creates────────────────────┘
└───────┬───────┘
        │ implements
        ▼
┌──────────────────┐     ┌───────────────┐  ┌─────────────────┐
│ WindowsUIFactory │     │ WindowsButton │  │ WindowsCheckbox │
├──────────────────┤     └───────────────┘  └─────────────────┘
│ CreateButton()   │─creates──▶ WindowsButton
│ CreateCheckbox() │─creates──▶ WindowsCheckbox
└──────────────────┘

┌──────────────────┐     ┌───────────────┐  ┌─────────────────┐
│  MacUIFactory    │     │   MacButton   │  │  MacCheckbox    │
├──────────────────┤     └───────────────┘  └─────────────────┘
│ CreateButton()   │─creates──▶ MacButton
│ CreateCheckbox() │─creates──▶ MacCheckbox
└──────────────────┘
```

### Participants
| Role | In Our Example | Responsibility |
|------|---------------|----------------|
| AbstractFactory | `IUIFactory` | Declares creation methods for each product in the family |
| ConcreteFactory | `WindowsUIFactory`, `MacUIFactory` | Implements creation methods for one specific family |
| AbstractProduct | `IButton`, `ICheckbox` | Declares interface for a type of product |
| ConcreteProduct | `WindowsButton`, `MacCheckbox`, etc. | Implements the product for a specific family |
| Client | `Application` | Uses only AbstractFactory and AbstractProduct interfaces |

### The Consistency Guarantee
This is the **killer feature** of Abstract Factory. Since the client receives a single factory that produces all related objects, it's **structurally impossible** to mix products from different families. The type system enforces correctness.

---

## Key Differences

| Aspect | Factory Method | Abstract Factory |
|--------|---------------|-----------------|
| **Scope** | Creates ONE product | Creates a FAMILY of related products |
| **Mechanism** | Inheritance (subclass overrides factory method) | Composition (client holds a factory reference) |
| **Extension** | Add new subclass of Creator | Add new ConcreteFactory implementing the interface |
| **Complexity** | Lower — one product hierarchy | Higher — multiple product hierarchies |
| **Relationship** | Abstract Factory often USES Factory Methods internally | Factory Method is a building block |
| **Guarantee** | Correct single product | Correct + consistent product family |

### How They Relate
Abstract Factory is essentially a **collection of Factory Methods** grouped together. Each method in `IUIFactory` (CreateButton, CreateCheckbox) is itself a factory method. The Abstract Factory pattern adds the constraint that these methods must produce **compatible** objects.

```
Abstract Factory = Factory Method × N products + family consistency
```

---

## When to Use What

### Use Factory Method When:
- You have **one product type** with multiple variants
- You want subclasses to control what gets created
- You need a **hook** for subclasses to extend creation logic
- Examples: Logger creation, notification sender, document parser

### Use Abstract Factory When:
- You have **multiple related products** that must work together
- You need to enforce **consistency** across a product family
- Your system must support **multiple configurations/platforms**
- Examples: UI toolkits, database access layers (Connection + Command + Reader), cloud provider SDKs (Storage + Compute + Network)

### Use Neither When:
- Object creation is simple and unlikely to change — just use `new`
- You only have one implementation — don't over-engineer
- A simple static factory method or DI container suffices

---

## Anti-Patterns & Pitfalls

### 1. Factory Explosion
**Problem:** Creating a factory for every single class.
**Fix:** Only use factories when there's genuine variation or the creation logic is complex.

### 2. God Factory
**Problem:** One factory that creates everything via a giant switch statement.
```csharp
// This is NOT the Factory Method pattern — it's a Simple Factory (which is fine, but different)
public static IPaymentProcessor Create(string type) => type switch
{
    "credit" => new CreditCardProcessor(),
    "upi" => new UpiProcessor(),
    _ => throw new ArgumentException()
};
```
**Note:** Simple Factory is a valid pragmatic choice but doesn't satisfy Open/Closed. Know the tradeoff.

### 3. Leaking Concrete Types
**Problem:** Factory returns an interface but callers downcast to concrete type.
**Fix:** If you need type-specific behavior, your abstraction is wrong. Redesign the interface.

### 4. Abstract Factory with Single Product
**Problem:** Using Abstract Factory when you only have one product type.
**Fix:** Use Factory Method instead. Abstract Factory's value is in family consistency.

---

## Real-World Usage in Industry

### Factory Method
| System | Product | Variants |
|--------|---------|----------|
| Payment Gateway | `IPaymentProcessor` | Stripe, Razorpay, PayPal |
| Logging Framework | `ILogger` | ConsoleLogger, FileLogger, CloudWatchLogger |
| Notification Service | `INotificationSender` | Email, SMS, Push |
| Serialization | `ISerializer` | JsonSerializer, XmlSerializer, ProtobufSerializer |
| .NET Framework | `DbCommand` | SqlCommand, OracleCommand, MySqlCommand |

### Abstract Factory
| System | Family | Products in Family |
|--------|--------|-------------------|
| Cross-platform UI | Windows / macOS / Linux | Button, Checkbox, Menu, Dialog |
| ADO.NET | SqlServer / Oracle / Postgres | Connection, Command, DataReader, Parameter |
| Cloud SDK | AWS / Azure / GCP | StorageClient, ComputeClient, QueueClient |
| Game Engine | Medieval / Sci-Fi / Modern | Warrior, Weapon, Armor, Vehicle |
| Document Export | PDF / HTML / Word | Header, Paragraph, Table, Image |

### In .NET Specifically
- `DbProviderFactory` — classic Abstract Factory in ADO.NET
- `ILoggerFactory` in Microsoft.Extensions.Logging — Factory Method
- `IServiceProvider` / DI Container — generalized factory (Service Locator, related but distinct)
- `HttpClientFactory` — Factory Method with pooling and configuration

---

## Folder Structure (This Project)

```
FactoryAndAbstractFactoryDesignPattern/
│
├── FactoryMethod/
│   ├── IPaymentProcessor.cs       → Product interface
│   ├── PaymentProcessors.cs       → Concrete products
│   └── PaymentFactory.cs          → Creator (abstract) + Concrete creators
│
├── AbstractFactory/
│   ├── IComponents.cs             → Abstract product interfaces
│   ├── WindowsComponents.cs       → Windows product family
│   ├── MacComponents.cs           → macOS product family
│   ├── UIFactory.cs               → Abstract factory + concrete factories
│   └── Application.cs             → Client consuming the factory
│
├── Program.cs                     → Entry point / demo
└── README.md                      → This file
```

---

## Quick Mental Model

> **Factory Method** = "I'll let my subclass decide what to create"
> **Abstract Factory** = "I'll give you a kit that creates a whole matching set"

Both patterns exist to **decouple object creation from object usage**, making your system flexible, testable, and adherent to SOLID principles.
