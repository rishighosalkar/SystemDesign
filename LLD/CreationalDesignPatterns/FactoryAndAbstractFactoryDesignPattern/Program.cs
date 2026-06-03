using FactoryMethod;
using AbstractFactory;

Console.WriteLine("=== Factory Method Pattern ===");
PaymentFactory factory = new UpiFactory();
factory.MakePayment(500);

factory = new CreditCardFactory();
factory.MakePayment(1200);

Console.WriteLine("\n=== Abstract Factory Pattern ===");
IUIFactory uiFactory = new WindowsUIFactory();
var app = new Application(uiFactory);
app.RenderUI();

Console.WriteLine();
uiFactory = new MacUIFactory();
app = new Application(uiFactory);
app.RenderUI();
