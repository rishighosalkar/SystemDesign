using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrategyPattern.PaymentStrategies
{
    public class CreditCardStrategy : IPaymentStrategy
    {
        public void Pay(double amount)
        {
            Console.WriteLine("Credit card strategy");
        }
    }
}
