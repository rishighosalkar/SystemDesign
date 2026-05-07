using AdapterDesignPattern.Adaptee;
using AdapterDesignPattern.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AdapterDesignPattern.Adaptor
{
    public class XmlToJsonAdaptor : IAdaptor
    {
        XmlImpl xmlImpl;
        public XmlToJsonAdaptor(XmlImpl xmlImpl)
        {
            this.xmlImpl = xmlImpl;
        }
        public string ConvertXmlToJson(User user)
        {
            string xml = xmlImpl.GetXml(user);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            string jsonText = JsonConvert.SerializeObject(xmlDoc);
            return jsonText;
        }
    }
}
