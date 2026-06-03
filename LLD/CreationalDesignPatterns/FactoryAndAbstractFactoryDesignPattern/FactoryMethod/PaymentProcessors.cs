namespace FactoryMethod;

public class CreditCardProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount) =>
        Console.WriteLine($"Processing ${amount} via Credit Card");
}

public class UpiProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount) =>
        Console.WriteLine($"Processing ${amount} via UPI");
}

public class PayPalProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount) =>
        Console.WriteLine($"Processing ${amount} via PayPal");
}
