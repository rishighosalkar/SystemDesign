using System;
using ObserverDesignPattern.Observer;

namespace ObserverDesignPattern.Observable;

public class RoyalEnfieldObservable : IObservable
{
    List<INotificationAlert> royalEnfield = new List<INotificationAlert>();
    int stockCount = 0;
    public void AddSubscribers(INotificationAlert notificationAlert)
    {
        royalEnfield.Add(notificationAlert);
    }

    public void RemoveSubscribers(INotificationAlert notificationAlert)
    {
        royalEnfield.Remove(notificationAlert);
    }

    public void NotifySubscribers()
    {
        foreach(INotificationAlert notificationAlert in royalEnfield)
        {
            notificationAlert.UpdateSubscribers();
        }
    }

    public int GetData()
    {
        return royalEnfield.Count();
    }

    public void SetStockCount(int stockCount)
    {
        if(this.stockCount == 0)
        {
            NotifySubscribers();
        }

        this.stockCount += stockCount;
    }
}
