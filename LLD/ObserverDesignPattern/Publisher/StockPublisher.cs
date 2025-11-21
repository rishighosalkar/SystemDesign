using ObserverDesignPattern.Subscriber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObserverDesignPattern.Publisher
{
    public class StockPublisher : IStock
    {
        private readonly List<IInvestor> _investors = new();
        public string Symbol;
        private decimal _price;

        public decimal Price
        {
            get => _price;
            set
            {
                if(_price != value)
                {
                    _price = value;
                    NotifyPullModel();
                    NotifyPushModel();
                }
            }
        }

        public StockPublisher(string symbol, decimal price)
        {
            Symbol = symbol;
            _price = price;
        }

        public void Attach(IInvestor investor)
        {
            _investors.Add(investor);
        }

        public void Dettach(IInvestor investor)
        {
            _investors?.Remove(investor);
        }

        public void NotifyPullModel()
        {
            foreach(var investor in _investors)
            {
                investor.UpdatePullModel(this);
            }
        }

        public void NotifyPushModel()
        {
            foreach(var investor in _investors)
            {
                investor.UpdatePushModel(Price, Symbol);
            }
        }
    }
}
