using System.Collections.Concurrent;
using FoodCartLLD.Models;

namespace FoodCartLLD.Services;

public class OrderService
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();
    private readonly CartService _cartService;

    // Valid state transitions: Placed→Preparing→Ready→Delivered, Placed→Cancelled
    private static readonly Dictionary<OrderStatus, OrderStatus[]> _transitions = new()
    {
        [OrderStatus.Placed] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.Ready],
        [OrderStatus.Ready] = [OrderStatus.Delivered],
    };

    public OrderService(CartService cartService) => _cartService = cartService;

    public Order Checkout(Guid userId)
    {
        var cart = _cartService.GetCart(userId);
        if (!cart.Items.Any())
            throw new InvalidOperationException("Cart is empty");

        var order = new Order
        {
            UserId = userId,
            Items = new List<CartItem>(cart.Items),
            TotalAmount = cart.TotalAmount
        };
        _orders[order.Id] = order;
        _cartService.Clear(userId);
        return order;
    }

    public Order? Get(Guid orderId) => _orders.GetValueOrDefault(orderId);

    public List<Order> GetByUser(Guid userId) =>
        _orders.Values.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToList();

    public Order UpdateStatus(Guid orderId, OrderStatus newStatus)
    {
        var order = Get(orderId) ?? throw new InvalidOperationException("Order not found");
        if (!_transitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException($"Cannot transition from {order.Status} to {newStatus}");
        order.Status = newStatus;
        return order;
    }
}
