using FoodCartLLD.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodCartLLD.Controllers;

public record AddToCartRequest(Guid MenuItemId, int Quantity = 1);

[ApiController]
[Route("api/[controller]")]
public class CartController(CartService cartService) : ControllerBase
{
    [HttpGet("{userId}")]
    public IActionResult Get(Guid userId) => Ok(cartService.GetCart(userId));

    [HttpPost("{userId}/items")]
    public IActionResult AddItem(Guid userId, AddToCartRequest req) =>
        Ok(cartService.AddItem(userId, req.MenuItemId, req.Quantity));

    [HttpDelete("{userId}/items/{menuItemId}")]
    public IActionResult RemoveItem(Guid userId, Guid menuItemId) =>
        Ok(cartService.RemoveItem(userId, menuItemId));
}
