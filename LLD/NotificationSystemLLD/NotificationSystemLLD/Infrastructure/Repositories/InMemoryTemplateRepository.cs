using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Infrastructure.Repositories;

// Keyed by (NotificationType, Channel). In production this would be a DB table.
public class InMemoryTemplateRepository : ITemplateRepository
{
    private readonly Dictionary<(NotificationType, Channel), MessageTemplate> _store;

    public InMemoryTemplateRepository()
    {
        _store = new()
        {
            [(NotificationType.OrderDelivered, Channel.Email)] = new()
            {
                TemplateId   = "tmpl_order_delivered_email",
                Channel      = Channel.Email,
                NotificationType = NotificationType.OrderDelivered,
                Subject      = "Your order {{orderId}} has been delivered!",
                Body         = "Hi {{name}}, your order {{orderId}} was delivered on {{date}}."
            },
            [(NotificationType.OrderDelivered, Channel.Sms)] = new()
            {
                TemplateId   = "tmpl_order_delivered_sms",
                Channel      = Channel.Sms,
                NotificationType = NotificationType.OrderDelivered,
                Body         = "Hi {{name}}, order {{orderId}} delivered. Thanks for shopping!"
            },
            [(NotificationType.PaymentFailed, Channel.Email)] = new()
            {
                TemplateId   = "tmpl_payment_failed_email",
                Channel      = Channel.Email,
                NotificationType = NotificationType.PaymentFailed,
                Subject      = "Payment failed for order {{orderId}}",
                Body         = "Hi {{name}}, your payment of {{amount}} failed. Please retry."
            },
            [(NotificationType.Otp, Channel.Sms)] = new()
            {
                TemplateId   = "tmpl_otp_sms",
                Channel      = Channel.Sms,
                NotificationType = NotificationType.Otp,
                Body         = "Your OTP is {{otp}}. Valid for 5 minutes. Do not share."
            }
        };
    }

    public MessageTemplate? Get(NotificationType type, Channel channel) =>
        _store.TryGetValue((type, channel), out var t) ? t : null;
}
