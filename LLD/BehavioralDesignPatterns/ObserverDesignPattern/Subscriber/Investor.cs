using ObserverDesignPattern.Publisher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObserverDesignPattern.Subscriber
{
    public class Investor : IInvestor
    {
        public string Name { get; }
        public Investor(string name)
        {
            Name = name;
        }
        public void UpdatePullModel(StockPublisher stockPublisher)
        {
            Console.WriteLine($"Notified user: {Name} with updated price {stockPublisher.Price} using pull model for " +
                $"{stockPublisher.Symbol}");
        }

        public void UpdatePushModel(decimal price, string symbol)
        {
            Console.WriteLine($"Notified user: {Name} with updated price {price} using push model for {symbol}");
        }
    }
}
