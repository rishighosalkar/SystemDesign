using StateDesignPattern.States;

namespace StateDesignPattern.Context
{
    public class OrderProcessing
    {
        private IOrderState _orderState;

        public OrderProcessing()
        {
            _orderState = new CreateState();
        }

        public void SetState(IOrderState state)
        {
            _orderState = state;
        }

        public void NextState()
        {
            _orderState.NextState(this);
        }

        public void CancelOrder()
        {
            _orderState.CancelOrder(this);
        }
    }
}
