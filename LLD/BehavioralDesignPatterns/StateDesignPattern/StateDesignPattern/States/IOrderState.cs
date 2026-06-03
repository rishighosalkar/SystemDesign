namespace StateDesignPattern.States
{
    public interface IOrderState
    {
        void NextState(Context.OrderProcessing orderProcessing);
        void CancelOrder(Context.OrderProcessing orderProcessing);
    }
}
