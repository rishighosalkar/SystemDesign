using ObserverDesignPattern.Observer;

namespace ObserverDesignPattern.Observable;

public interface IObservable{
    public void AddSubscribers(INotificationAlert notificationAlert);
    public void RemoveSubscribers(INotificationAlert notificationAlert);
    public void NotifySubscribers();
    public int GetData();
    public void SetStockCount(int count);
}