using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainOfResponsibilityDesignPattern.Logger
{
    public class InfoLog : LogProcessor
    {
        public InfoLog(LogLevel logLevel, LogProcessor logProcessor)
        {
            level = logLevel;
            nextLogProcessor = logProcessor;
        }
        public override void Message(string message)
        {
            Console.WriteLine($"This is Info level log {message}");
        }
    }
}
