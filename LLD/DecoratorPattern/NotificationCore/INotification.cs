using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoratorPattern.NotificationCore
{
    public interface INotification
    {
        public void Notify(string  message);
    }
}
