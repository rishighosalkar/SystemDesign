In the Decorator Design Pattern, we separate extra features/behaviors into their own decorator classes.

Core Concept
•	You have a base interface or abstract class (e.g., INotifier).
•	You have a concrete implementation (e.g., EmailNotifier).
•	Each additional feature is put into a separate decorator class (e.g., SlackNotifierDecorator, SMSNotifierDecorator, etc.).
•	Decorators wrap objects of the same interface and add behavior on top.

Why separate features?
To achieve single responsibility and composable behaviour.
Each decorator class:
•	Adds only ONE feature
•	Wraps another object
•	Doesn’t modify the original class
•	Can be stacked in any order

Example Structure:
public interface INotifier
{
    void Send(string message);
}

// Core/Base behaviour
public class EmailNotifier : INotifier
{
    public void Send(string message)
    {
        Console.WriteLine("Email: " + message);
    }
}

//Decorators
public abstract class NotifierDecorator : INotifier
{
    protected INotifier _notifier;

    public NotifierDecorator(INotifier notifier)
    {
        _notifier = notifier;
    }

    public virtual void Send(string message)
    {
        _notifier.Send(message);
    }
}

//decorator 1
public class SMSNotifier : NotifierDecorator
{
    public SMSNotifier(INotifier notifier) : base(notifier) { }

    public override void Send(string message)
    {
        base.Send(message);
        Console.WriteLine("SMS: " + message);
    }
}

//decorator 2
public class SlackNotifier : NotifierDecorator
{
    public SlackNotifier(INotifier notifier) : base(notifier) { }

    public override void Send(string message)
    {
        base.Send(message);
        Console.WriteLine("Slack: " + message);
    }
}

//stacking decorator
INotifier notifier =
    new SlackNotifier(
        new SMSNotifier(
            new EmailNotifier()
        )
    );

notifier.Send("Hello!");

Summary:

Are features separated?	Yes — each new feature goes in its own class
Do decorators modify the real object?	No, they wrap it
Can features be combined?	Yes — decorators are stackable
Why do this?	Flexibility, Open/Closed Principle, reusable behaviors

Class Diagram:

          ┌──────────────────────┐
          │ INotifier            │
          │ + Send(message)      │
          └──────────▲───────────┘
                     │
          ┌──────────┴───────────┐
          │ EmailNotifier         │
          │ + Send(message)       │
          └──────────▲───────────┘
                     │
          ┌──────────┴──────────────────────────────┐
          │ NotifierDecorator                        │
          │ - notifier : INotifier                   │
          │ + Send(message)                          │
          └──────────▲───────────────────────────────┘
                     │
        ┌────────────┼───────────────────────────────┐
        │            │                               │
 ┌──────┴──────┐ ┌───┴────────┐              ┌──────────────┐
 │ SMSNotifier │ │ SlackNotifier│             │ TeamsNotifier │
 │ + Send()    │ │ + Send()     │             │ + Send()      │
 └─────────────┘ └──────────────┘             └───────────────┘



When to Prefer Decorator Over Inheritance

Here are the clear, real-world reasons:

✔ 1. When you need dynamic behavior changes

Decorators allow you to add/remove features at runtime.

Example:
var notifier = new SMSNotifier(new EmailNotifier());
// Later...
notifier = new SlackNotifier(notifier);


Inheritance cannot change behavior at runtime.

✔ 2. When you need combinable features

Decorators allow stacking:

Email + SMS

Email + Slack

Email + SMS + Slack

Email only

SMS only

Inheritance would require many combinations:

EmailAndSMSNotifier
EmailSMSAndSlackNotifier
EmailSlackNotifier
...


Explosion of subclasses = ❌

✔ 3. When you must follow Single Responsibility Principle

Each decorator does ONE small thing (e.g., add SMS).

Inheritance tends to grow into a "God class" with too many features.

✔ 4. When you need the Open/Closed Principle

You can add new decorators without modifying existing code.

Inheritance often forces rewriting base classes or adding more subclasses.

✔ 5. When subclassing is not possible

Sometimes the class is sealed, or you don’t own the source code.

Decorator works even when the class cannot be extended.

✔ 6. When you want to avoid deep inheritance trees

Instead of:

BaseNotifier
 |- EmailNotifier
     |- EmailPlusSMSNotifier
        |- EmailSMSPlusSlackNotifier


You just keep wrapping.


❌ When NOT to use Decorator

When the feature must be mandatory

When too many decorators make debugging difficult

When the creation of object graph becomes too complex

When order of decorators matters (and it becomes messy)


Here is a real-world Decorator Pattern example that is not about notifications.

We’ll use a Data Stream Compression + Encryption system — similar to the Java I/O library and .NET stream wrappers.

✅ Real-World Scenario

You have a system that writes data to disk or network.
Depending on requirements, data may need:

Compression

Encryption

Caching

Chunking

You don’t want one giant class like:
EncryptedCompressedCachedStream
CompressedCachedStream
CachedEncryptedStream
EncryptedStream
CompressedStream
...


Real-World: Stream Processing
// Component Interface
public interface IDataStream
{
    void Write(string data);
}

// Concrete Component — The core writer
public class FileDataStream : IDataStream
{
    public void Write(string data)
    {
        Console.WriteLine($"Writing to file: {data}");
    }
}


// Base decorator
public abstract class DataStreamDecorator : IDataStream
{
    protected IDataStream _stream;

    protected DataStreamDecorator(IDataStream stream)
    {
        _stream = stream;
    }

    public virtual void Write(string data)
    {
        _stream.Write(data);
    }
}

// Concrete Decorator #1: Compression
public class CompressedDataStream : DataStreamDecorator
{
    public CompressedDataStream(IDataStream stream) : base(stream) {}

    public override void Write(string data)
    {
        string compressed = $"[COMPRESSED:{data}]";
        Console.WriteLine("Compressing data...");
        base.Write(compressed);
    }
}

// Concrete Decorator #2: Encryption
public class EncryptedDataStream : DataStreamDecorator
{
    public EncryptedDataStream(IDataStream stream) : base(stream) {}

    public override void Write(string data)
    {
        string encrypted = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(data)
        );

        Console.WriteLine("Encrypting data...");
        base.Write(encrypted);
    }
}

// Concrete Decorator #3: Caching
public class CachedDataStream : DataStreamDecorator
{
    public CachedDataStream(IDataStream stream) : base(stream) {}

    public override void Write(string data)
    {
        Console.WriteLine("Caching data before writing...");
        base.Write(data);
    }
}

//Client Code (Combining Decorators)
public class Program
{
    public static void Main()
    {
        IDataStream stream =
            new EncryptedDataStream(               // Decorator 2
                new CompressedDataStream(          // Decorator 1
                    new CachedDataStream(          // Decorator 3
                        new FileDataStream()       // Concrete
                    )
                )
            );

        stream.Write("Hello World");
    }
}
