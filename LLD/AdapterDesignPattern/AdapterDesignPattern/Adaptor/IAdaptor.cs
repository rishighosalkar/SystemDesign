using AdapterDesignPattern.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdapterDesignPattern.Adaptor
{
    public interface IAdaptor
    {
        public string ConvertXmlToJson(User user);
    }
}
