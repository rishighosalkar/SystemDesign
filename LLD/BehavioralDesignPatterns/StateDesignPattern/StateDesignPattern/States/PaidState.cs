using StateDesignPattern.Context;

namespace StateDesignPattern.States
{
    public class PaidState : IOrderState
    {
        public void NextState(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Payment done, moving to Shipped state.");
            orderProcessing.SetState(new ShippedState());
        }

        public void CancelOrder(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Order cancelled from Paid state. Refund initiated.");
            orderProcessing.SetState(new CancelledState());
        }
    }
}
