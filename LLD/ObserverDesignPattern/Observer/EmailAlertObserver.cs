using System;
using ObserverDesignPattern.Observable;

namespace ObserverDesignPattern.Observer;

public class EmailAlertObserver : INotificationAlert
{
    private IObservable _observable;
    private string email;
    public EmailAlertObserver(string email, IObservable observable)
    {
        this.email = email;
        _observable = observable;
    }

    public void UpdateSubscribers()
    {
        Console.WriteLine("email sent to " + email + "stock is" + _observable.GetData());
    }
}
