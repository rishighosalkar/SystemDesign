using NotificationSystemLLD.Domain.Models;

namespace NotificationSystemLLD.Interfaces;

// Decouples rendering from dispatch.
// In production this would be SQS/RabbitMQ; here it's an in-memory queue.
public interface IMessageQueue
{
    void Enqueue(Notification notification);
    Notification? Dequeue();
    bool IsEmpty { get; }
}
