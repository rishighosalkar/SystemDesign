using System.Collections.Concurrent;
using FoodCartLLD.Models;

namespace FoodCartLLD.Services;

public class CartService
{
    private readonly ConcurrentDictionary<Guid, Cart> _carts = new(); // keyed by userId
    private readonly MenuService _menuService;

    public CartService(MenuService menuService) => _menuService = menuService;

    public Cart GetCart(Guid userId) =>
        _carts.GetOrAdd(userId, id => new Cart { UserId = id });

    public Cart AddItem(Guid userId, Guid menuItemId, int qty)
    {
        var menu = _menuService.Get(menuItemId)
            ?? throw new InvalidOperationException("Menu item not found");
        if (!menu.IsAvailable)
            throw new InvalidOperationException("Item not available");

        var cart = GetCart(userId);
        var existing = cart.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
        if (existing != null)
            existing.Quantity += qty;
        else
            cart.Items.Add(new CartItem
            {
                MenuItemId = menuItemId,
                ItemName = menu.Name,
                Price = menu.Price,
                Quantity = qty
            });
        return cart;
    }

    public Cart RemoveItem(Guid userId, Guid menuItemId)
    {
        var cart = GetCart(userId);
        cart.Items.RemoveAll(i => i.MenuItemId == menuItemId);
        return cart;
    }

    public void Clear(Guid userId) => _carts.TryRemove(userId, out _);
}
