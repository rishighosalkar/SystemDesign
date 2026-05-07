using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateDesignPattern
{
    public interface IOrderState
    {
        public void NextState(OrderProcessing orderProcessing);
        public void CancelledState(OrderProcessing orderProcessing);
    }
}
