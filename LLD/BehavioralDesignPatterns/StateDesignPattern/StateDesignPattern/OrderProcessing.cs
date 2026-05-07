using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateDesignPattern
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
    }
}
