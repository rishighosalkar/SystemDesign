using StateDesignPattern.Context;

namespace StateDesignPattern.States
{
    public class DeliveredState : IOrderState
    {
        public void NextState(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Order already delivered. No further state.");
        }

        public void CancelOrder(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Cannot cancel. Order already delivered.");
        }
    }
}
