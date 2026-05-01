using FoodCartLLD.Models;
using FoodCartLLD.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodCartLLD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController(MenuService menuService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(menuService.GetAll());

    [HttpPost]
    public IActionResult Add(MenuItem item) => Ok(menuService.Add(item));

    [HttpPatch("{id}/toggle")]
    public IActionResult Toggle(Guid id) =>
        menuService.ToggleAvailability(id) ? Ok() : NotFound();
}
