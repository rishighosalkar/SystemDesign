using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Infrastructure.Queue;

// In production: replace with SQS, RabbitMQ, or Azure Service Bus.
// Priority queues can be added by maintaining separate queues per NotificationPriority.
public class InMemoryMessageQueue : IMessageQueue
{
    private readonly Queue<Notification> _queue = new();

    public void Enqueue(Notification notification) => _queue.Enqueue(notification);

    public Notification? Dequeue() =>
        _queue.TryDequeue(out var notification) ? notification : null;

    public bool IsEmpty => _queue.Count == 0;
}
