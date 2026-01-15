Composite Design Pattern (Interview Notes)
Definition

The Composite Design Pattern is a structural design pattern that allows treating individual objects (Leaf) and groups of objects (Composite) uniformly using a common interface.

It is primarily used to represent tree-like hierarchical structures.

Intent

Represent part–whole hierarchies

Allow clients to treat single objects and collections uniformly

Eliminate if-else or type-checking logic in client code

Core Idea

Both Leaf and Composite implement the same interface.
A Composite contains a collection of objects of that same interface.

```
Client → Component → Leaf / Composite
```

Structure

```
Component (interface)
   |
   ├── Leaf        → Individual object
   |
   └── Composite   → Contains children of type Component
```

Participants
Component

Defines the common interface for all objects in the hierarchy.

```C#
public interface IFileSystem
{
    void ls();
}
```

Leaf

Represents a single object that does not have children.

```C#
public class FileSystem : IFileSystem
{
    public void ls()
    {
        Console.WriteLine("File");
    }
}
```

Composite

Represents a collection of components and delegates operations to its children.

```C#
public class DirectorySystem : IFileSystem
{
    private readonly List<IFileSystem> _items = new();

    public void ls()
    {
        Console.WriteLine("Directory");
        foreach (var item in _items)
        {
            item.ls();
        }
    }

    public void Add(IFileSystem fileSystem)
    {
        _items.Add(fileSystem);
    }
}
```

Client Usage

```C#
IFileSystem directory = new DirectorySystem();
IFileSystem file = new FileSystem();

directory.Add(file);
directory.ls();
```

Key Point:
The client does not need to know whether it is working with a file or a directory.

When to Use
Use the Composite Pattern when:
*You are dealing with hierarchical (tree-like) data
*You want to represent part–whole relationships
*You want to avoid conditional logic based on object types

Real-World Examples

*File System → File & Folder
*UI Components → Button, Panel, Window
*Organization Hierarchy → Employee, Manager
*Menu Systems → MenuItem, Menu

Advantages

*Simplifies client code
*Promotes polymorphism
*Supports Open/Closed Principle
*Makes recursive operations easy

Disadvantages

*Hard to restrict what components a composite can contain
*Leaf classes may need to implement unsupported operations
*Can lead to overly generic designs

One-Line Interview Answer
>The Composite Design Pattern allows clients to treat individual objects and compositions of objects uniformly by using a common interface, making it ideal for tree-structured systems.

30-Second Interview Explanation
>Composite is a structural pattern used for hierarchical data like file systems. It defines a common interface for both leaf and composite objects, allowing the client to interact with them uniformly without type checks, resulting in cleaner and more maintainable code.