using AdapterDesignPattern.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdapterDesignPattern.Adaptee
{
    public interface IXml
    {
        public string GetXml(User users);
    }
}
