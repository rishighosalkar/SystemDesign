namespace AbstractFactory;

public class MacButton : IButton
{
    public void Render() => Console.WriteLine("Rendering macOS-style Button");
}

public class MacCheckbox : ICheckbox
{
    public void Render() => Console.WriteLine("Rendering macOS-style Checkbox");
}
