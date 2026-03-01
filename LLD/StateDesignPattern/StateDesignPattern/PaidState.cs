using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateDesignPattern
{
    public class PaidState : IOrderState
    {
        public void CancelledState(OrderProcessing orderProcessing)
        {
            throw new NotImplementedException();
        }

        public void NextState(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Payment done, moving to shipped state");
            orderProcessing.SetState(new ShippedState());
        }
    }
}
