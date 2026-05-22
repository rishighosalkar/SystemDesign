using StateDesignPattern.Context;

namespace StateDesignPattern.States
{
    public class ShippedState : IOrderState
    {
        public void NextState(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Order shipped, moving to Delivered state.");
            orderProcessing.SetState(new DeliveredState());
        }

        public void CancelOrder(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Cannot cancel. Order already shipped.");
        }
    }
}
