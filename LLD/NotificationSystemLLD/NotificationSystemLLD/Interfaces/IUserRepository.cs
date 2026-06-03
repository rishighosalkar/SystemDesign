using NotificationSystemLLD.Domain.Models;

namespace NotificationSystemLLD.Interfaces;

public interface IUserRepository
{
    User? GetById(int userId);
    void Save(User user);
}
