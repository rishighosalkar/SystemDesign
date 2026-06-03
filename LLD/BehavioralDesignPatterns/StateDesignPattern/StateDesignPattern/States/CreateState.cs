using StateDesignPattern.Context;

namespace StateDesignPattern.States
{
    public class CreateState : IOrderState
    {
        public void NextState(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Order created, moving to Paid state.");
            orderProcessing.SetState(new PaidState());
        }

        public void CancelOrder(OrderProcessing orderProcessing)
        {
            Console.WriteLine("Order cancelled from Created state.");
            orderProcessing.SetState(new CancelledState());
        }
    }
}
