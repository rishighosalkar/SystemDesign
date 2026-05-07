using ObserverDesignPattern.Publisher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObserverDesignPattern.Subscriber
{
    public interface IInvestor
    {
        void UpdatePullModel(StockPublisher stockPublisher);
        void UpdatePushModel(decimal price, string symbol);
    }
}
