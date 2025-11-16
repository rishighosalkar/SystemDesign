using DecoratorPattern.NotificationCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoratorPattern.NotificationDecorators
{
    public abstract class NotificationDecorator : INotification
    {
        protected INotification _notification;

        protected NotificationDecorator(INotification notification)
        {
            _notification = notification;
        }
        public virtual void Notify(string message)
        {
            _notification.Notify(message);
        }
    }
}
