using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RemoteC.Data.Entities;
using System.Data;

namespace RemoteC.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly RemoteCDbContext _context;

    public UserRepository(RemoteCDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserAsync(Guid userId)
    {
        var userIdParam = new SqlParameter("@UserId", userId);
        
        var users = await _context.Users
            .FromSqlRaw("EXEC sp_GetUser @UserId", userIdParam)
            .AsNoTracking()
            .ToListAsync();

        var user = users.FirstOrDefault();
        if (user != null)
        {
            // Load roles and permissions separately since stored procedures return multiple result sets
            user.UserRoles = await GetUserRolesInternalAsync(userId);
        }

        return user;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var users = await _context.Users
            .FromSqlRaw("SELECT * FROM Users WHERE Email = @p0", email)
            .AsNoTracking()
            .ToListAsync();

        return users.FirstOrDefault();
    }

    public async Task<User> UpsertUserAsync(string email, string firstName, string lastName, string? azureAdB2CId = null, bool isActive = true)
    {
        var userIdParam = new SqlParameter("@UserId", SqlDbType.UniqueIdentifier)
        {
            Direction = ParameterDirection.Output
        };

        var parameters = new[]
        {
            userIdParam,
            new SqlParameter("@Email", email),
            new SqlParameter("@FirstName", firstName),
            new SqlParameter("@LastName", lastName),
            new SqlParameter("@AzureAdB2CId", (object?)azureAdB2CId ?? DBNull.Value),
            new SqlParameter("@IsActive", isActive)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_UpsertUser @UserId OUTPUT, @Email, @FirstName, @LastName, @AzureAdB2CId, @IsActive",
            parameters);

        var userId = (Guid)userIdParam.Value;
        return (await GetUserAsync(userId))!;
    }

    public async Task<IEnumerable<User>> GetUsersAsync(int pageNumber = 1, int pageSize = 25)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return users;
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, Guid assignedBy)
    {
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedBy = assignedBy,
            AssignedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var userRole = await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.RoleId == roleId)
            .FirstOrDefaultAsync();

        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role)
            .AsNoTracking()
            .ToListAsync();

        return roles;
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId)
    {
        var permissions = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Where(ur => ur.Role.IsActive)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission)
            .Where(p => p.IsActive)
            .Distinct()
            .AsNoTracking()
            .ToListAsync();

        return permissions;
    }

    private async Task<ICollection<UserRole>> GetUserRolesInternalAsync(Guid userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetUserCountAsync()
    {
        return await _context.Users.CountAsync();
    }
}