using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<int, User> _store = [];

    public User? GetById(int userId) =>
        _store.TryGetValue(userId, out var user) ? user : null;

    public void Save(User user) => _store[user.Id] = user;
}
