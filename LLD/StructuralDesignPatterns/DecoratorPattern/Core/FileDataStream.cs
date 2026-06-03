using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoratorPattern.Core
{
    public class FileDataStream : IDataStream
    {
        public void Write(string text)
        {
            Console.WriteLine("FileDataStream text: "+ text);
        }
    }
}
