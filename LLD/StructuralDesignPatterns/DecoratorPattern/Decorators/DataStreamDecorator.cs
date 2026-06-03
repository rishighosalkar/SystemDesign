using DecoratorPattern.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoratorPattern.Decorators
{
    public abstract class DataStreamDecorator : IDataStream
    {
        protected IDataStream _stream;
        public DataStreamDecorator(IDataStream dataStream)
        {
            _stream = dataStream;
        }
        public virtual void Write(string text)
        {
            _stream.Write(text);
        }
    }
}
