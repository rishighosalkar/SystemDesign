using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainOfResponsibilityDesignPattern.Logger
{
    public abstract class LogProcessor
    {
        protected LogProcessor nextLogProcessor;
        protected LogLevel level;
        public void Log(LogLevel logLevel, string message)
        {
            if (level == logLevel)
            {
                Message(message);
            }
            if (nextLogProcessor != null)
            {
                nextLogProcessor.Log(logLevel, message);
            }

        }
        public abstract void Message(string message);
    }
}
