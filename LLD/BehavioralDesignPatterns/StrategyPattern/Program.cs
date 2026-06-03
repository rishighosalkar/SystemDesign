
using StrategyPattern.PaymentService;
using StrategyPattern.PaymentStrategies;

PaymentService paymentService = new PaymentService();

paymentService.SetPaymentStrategy(new CreditCardStrategy());
paymentService.Pay(20);

paymentService.SetPaymentStrategy(new UPIStrategy());
paymentService.Pay(100.5);

paymentService.SetPaymentStrategy(new PayPalStrategy());
paymentService.Pay(50);