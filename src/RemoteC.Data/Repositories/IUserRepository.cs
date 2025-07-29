using RemoteC.Data.Entities;

namespace RemoteC.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> UpsertUserAsync(string email, string firstName, string lastName, string? azureAdB2CId = null, bool isActive = true);
    Task<IEnumerable<User>> GetUsersAsync(int pageNumber = 1, int pageSize = 25);
    Task AssignRoleAsync(Guid userId, Guid roleId, Guid assignedBy);
    Task RemoveRoleAsync(Guid userId, Guid roleId);
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId);
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId);
}