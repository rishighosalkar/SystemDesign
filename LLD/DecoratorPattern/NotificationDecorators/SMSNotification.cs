using DecoratorPattern.NotificationCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoratorPattern.NotificationDecorators
{
    public class SMSNotification : NotificationDecorator
    {
        public SMSNotification(INotification notification) : base(notification) { }

        public override void Notify(string message)
        {
            Console.WriteLine($"SMS notification {message}");
            base.Notify(message);
        }
    }
}
