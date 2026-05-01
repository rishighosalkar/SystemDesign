using System.Collections.Concurrent;
using FoodCartLLD.Models;

namespace FoodCartLLD.Services;

public class MenuService
{
    private readonly ConcurrentDictionary<Guid, MenuItem> _items = new();

    public MenuItem Add(MenuItem item) { _items[item.Id] = item; return item; }

    public List<MenuItem> GetAll() => _items.Values.Where(i => i.IsAvailable).ToList();

    public MenuItem? Get(Guid id) => _items.GetValueOrDefault(id);

    public bool ToggleAvailability(Guid id)
    {
        var item = Get(id);
        if (item == null) return false;
        item.IsAvailable = !item.IsAvailable;
        return true;
    }
}
