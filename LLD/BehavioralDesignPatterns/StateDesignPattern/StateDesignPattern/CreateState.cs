using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateDesignPattern
{
    public class CreateState : IOrderState
    {
        public void CancelledState(OrderProcessing orderProcessing)
        {
            throw new NotImplementedException();
        }

        public void NextState(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Order created successfully, moving to payment state");
            orderProcessing.SetState(new PaidState());
        }
    }
}
