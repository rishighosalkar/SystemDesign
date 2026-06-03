using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacadeDesignPattern.LoggingFramework
{
    public class AppLogger : IAppLogger
    {
        public void Error(string message)
        {
            Console.WriteLine($"{message}");
        }

        public void Info(string message)
        {
            Console.WriteLine($"{message}");
        }
    }
}
