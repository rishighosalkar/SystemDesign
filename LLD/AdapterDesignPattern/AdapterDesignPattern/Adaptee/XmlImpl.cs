using AdapterDesignPattern.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace AdapterDesignPattern.Adaptee
{
    public class XmlImpl : IXml
    {
        public string GetXml(User user)
        {
            return SerializeObjectToXmlString(user);
        }

        public string SerializeObjectToXmlString<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Object to serialize cannot be null.");

            try
            {
                var xmlSerializer = new XmlSerializer(typeof(T));

                using (var stringWriter = new StringWriter())
                using (var xmlWriter = XmlWriter.Create(stringWriter))
                {
                    xmlSerializer.Serialize(xmlWriter, obj);
                    return stringWriter.ToString();
                }
            }
            catch (InvalidOperationException ex)
            {
                // This usually occurs if the object is not serializable
                throw new InvalidOperationException("Error serializing object to XML. Ensure the object is serializable.", ex);
            }
        }
    }
}
