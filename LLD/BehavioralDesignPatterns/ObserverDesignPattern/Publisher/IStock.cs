using ObserverDesignPattern.Subscriber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObserverDesignPattern.Publisher
{
    public interface IStock
    {
        public void Attach(IInvestor investor);
        public void Dettach(IInvestor investor);
        public void NotifyPullModel();
        public void NotifyPushModel();
    }
}
