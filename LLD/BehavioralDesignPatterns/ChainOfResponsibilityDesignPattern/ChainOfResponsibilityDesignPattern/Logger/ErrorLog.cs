using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainOfResponsibilityDesignPattern.Logger
{
    public class ErrorLog : LogProcessor
    {
        public ErrorLog(LogLevel logLevel, LogProcessor processor)
        {
            level = logLevel;
            nextLogProcessor = processor;
        }
        public override void Message(string message)
        {
            Console.WriteLine($"This is Error level log {message}");
        }
    }
}
