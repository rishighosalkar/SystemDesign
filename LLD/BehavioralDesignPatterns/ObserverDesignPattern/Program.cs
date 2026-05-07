// See https://aka.ms/new-console-template for more information

using ObserverDesignPattern.Observable;
using ObserverDesignPattern.Observer;
using ObserverDesignPattern.Publisher;
using ObserverDesignPattern.Subscriber;

//IObservable royalEnfieldObj = new RoyalEnfieldObservable();

//INotificationAlert observer1 = new EmailAlertObserver("abc@gmail.com", royalEnfieldObj);
//INotificationAlert observer2 = new EmailAlertObserver("xyz@gmail.com", royalEnfieldObj);
//INotificationAlert observer3 = new EmailAlertObserver("lmn@gmail.com", royalEnfieldObj);

//royalEnfieldObj.AddSubscribers(observer1);
//royalEnfieldObj.AddSubscribers(observer2);
//royalEnfieldObj.AddSubscribers(observer3);

//royalEnfieldObj.SetStockCount(1);

var zerodhaStock = new StockPublisher("Zerodha", 100);
var zomatoStock = new StockPublisher("Zomato", 250);

var johnDoe = new Investor("John");
var james = new Investor("James");

zerodhaStock.Attach(johnDoe);
zomatoStock.Attach(james);

zerodhaStock.Price = 120;
zomatoStock.Price = 220;

