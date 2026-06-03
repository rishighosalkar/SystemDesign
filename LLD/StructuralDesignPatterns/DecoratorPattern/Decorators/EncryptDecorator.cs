using DecoratorPattern.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoratorPattern.Decorators
{
    public class EncryptDecorator : DataStreamDecorator
    {
        public EncryptDecorator(IDataStream dataStream) : base(dataStream) { }

        public override void Write(string text)
        {
            Console.WriteLine("Encrypting text" + text);
            base.Write(text);
        }
    }
}
