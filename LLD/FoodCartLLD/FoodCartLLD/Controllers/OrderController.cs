using FoodCartLLD.Models;
using FoodCartLLD.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodCartLLD.Controllers;

public record UpdateStatusRequest(OrderStatus Status);

[ApiController]
[Route("api/[controller]")]
public class OrderController(OrderService orderService) : ControllerBase
{
    [HttpPost("{userId}/checkout")]
    public IActionResult Checkout(Guid userId) => Ok(orderService.Checkout(userId));

    [HttpGet("{orderId}")]
    public IActionResult Get(Guid orderId) =>
        orderService.Get(orderId) is { } order ? Ok(order) : NotFound();

    [HttpGet("user/{userId}")]
    public IActionResult GetByUser(Guid userId) => Ok(orderService.GetByUser(userId));

    [HttpPatch("{orderId}/status")]
    public IActionResult UpdateStatus(Guid orderId, UpdateStatusRequest req) =>
        Ok(orderService.UpdateStatus(orderId, req.Status));
}
