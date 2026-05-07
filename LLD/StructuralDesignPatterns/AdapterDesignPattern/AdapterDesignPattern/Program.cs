// See https://aka.ms/new-console-template for more information
using AdapterDesignPattern.Adaptee;
using AdapterDesignPattern.Adaptor;
using AdapterDesignPattern.Model;

Console.WriteLine("Hello, World!");

XmlImpl xmlImpl = new XmlImpl();

User user = new User { Id = Guid.NewGuid(), Email = "test1234@gmail.com", Name="test"};

Console.WriteLine(xmlImpl.GetXml(user));

IAdaptor adaptor = new XmlToJsonAdaptor(xmlImpl);

Console.WriteLine(adaptor.ConvertXmlToJson(user));