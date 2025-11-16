using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoratorPattern.NotificationCore
{
    public class EmailNotification : INotification
    {
        public void Notify(string message)
        {
            Console.WriteLine($"Base notification method {message}");
        }
    }
}
