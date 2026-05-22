using StateDesignPattern.Context;

namespace StateDesignPattern.States
{
    public class CancelledState : IOrderState
    {
        public void NextState(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Order is cancelled. Cannot proceed.");
        }

        public void CancelOrder(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Order is already cancelled.");
        }
    }
}
