// See https://aka.ms/new-console-template for more information

using ObserverDesignPattern.Observable;
using ObserverDesignPattern.Observer;

IObservable royalEnfieldObj = new RoyalEnfieldObservable();

INotificationAlert notificationAlert = new EmailAlertObserver("rushighosalkar@gmail.com", royalEnfieldObj);

royalEnfieldObj.AddSubscribers(notificationAlert);

royalEnfieldObj.SetStockCount(1);