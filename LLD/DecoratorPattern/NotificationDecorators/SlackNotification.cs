using DecoratorPattern.NotificationCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoratorPattern.NotificationDecorators
{
    public class SlackNotification : NotificationDecorator
    {
        public SlackNotification(INotification notification) : base(notification) { }

        public override void Notify(string message)
        {
            Console.WriteLine($"Slack notification {message}");
            base.Notify(message);
        }
    }
}
