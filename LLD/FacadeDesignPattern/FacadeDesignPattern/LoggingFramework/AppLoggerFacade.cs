using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacadeDesignPattern.LoggingFramework
{
    public class AppLoggerFacade
    {
        private readonly IAppLogger _logger;

        public AppLoggerFacade(IAppLogger logger)
        {
            _logger = logger;
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Error(string message)
        {
            _logger?.Error(message);
        }
    }
}
