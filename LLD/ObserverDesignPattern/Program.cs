// See https://aka.ms/new-console-template for more information

using ObserverDesignPattern.Observable;
using ObserverDesignPattern.Observer;

IObservable royalEnfieldObj = new RoyalEnfieldObservable();

INotificationAlert observer1 = new EmailAlertObserver("abc@gmail.com", royalEnfieldObj);
INotificationAlert observer2 = new EmailAlertObserver("xyz@gmail.com", royalEnfieldObj);
INotificationAlert observer3 = new EmailAlertObserver("lmn@gmail.com", royalEnfieldObj);

royalEnfieldObj.AddSubscribers(observer1);
royalEnfieldObj.AddSubscribers(observer2);
royalEnfieldObj.AddSubscribers(observer3);

royalEnfieldObj.SetStockCount(1);