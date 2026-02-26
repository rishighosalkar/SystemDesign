using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacadeDesignPattern.LoggingFramework
{
    public interface IAppLogger
    {
        public void Info(string message);
        public void Error(string message);
    }
}
