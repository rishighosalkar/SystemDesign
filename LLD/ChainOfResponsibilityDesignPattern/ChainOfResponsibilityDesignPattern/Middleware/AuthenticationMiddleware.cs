using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainOfResponsibilityDesignPattern.Middleware
{
    public class AuthenticationMiddleware : Middleware
    {
        public override void Handle()
        {
            Console.WriteLine($"Authenication Middleware");
        }
    }
}
