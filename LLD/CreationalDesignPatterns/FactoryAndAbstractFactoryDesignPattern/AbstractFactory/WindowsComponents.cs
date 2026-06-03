namespace AbstractFactory;

public class WindowsButton : IButton
{
    public void Render() => Console.WriteLine("Rendering Windows-style Button");
}

public class WindowsCheckbox : ICheckbox
{
    public void Render() => Console.WriteLine("Rendering Windows-style Checkbox");
}
