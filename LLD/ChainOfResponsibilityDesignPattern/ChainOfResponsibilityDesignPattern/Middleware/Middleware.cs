using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainOfResponsibilityDesignPattern.Middleware
{
    public abstract class Middleware
    {
        private Middleware _nexMiddleware;

        public Middleware Next(Middleware middleware)
        {
            _nexMiddleware = middleware;
            return _nexMiddleware;
        }

        public void Invoke()
        {
            Handle();
            _nexMiddleware?.Invoke();
        }

        public abstract void Handle();
    }
}
