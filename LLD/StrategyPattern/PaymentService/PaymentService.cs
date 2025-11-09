using StrategyPattern.PaymentStrategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrategyPattern.PaymentService
{
    public class PaymentService
    {
        private IPaymentStrategy _paymentStrategy;

        public void SetPaymentStrategy(IPaymentStrategy paymentStrategy)
        {
            _paymentStrategy = paymentStrategy;
        }

        public void Pay(double amount)
        {
            _paymentStrategy.Pay(amount);
        }
    }
}
