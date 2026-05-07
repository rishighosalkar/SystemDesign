namespace FactoryMethod;

public abstract class PaymentFactory
{
    public abstract IPaymentProcessor CreateProcessor();

    public void MakePayment(decimal amount)
    {
        var processor = CreateProcessor();
        processor.ProcessPayment(amount);
    }
}

public class CreditCardFactory : PaymentFactory
{
    public override IPaymentProcessor CreateProcessor() => new CreditCardProcessor();
}

public class UpiFactory : PaymentFactory
{
    public override IPaymentProcessor CreateProcessor() => new UpiProcessor();
}

public class PayPalFactory : PaymentFactory
{
    public override IPaymentProcessor CreateProcessor() => new PayPalProcessor();
}
