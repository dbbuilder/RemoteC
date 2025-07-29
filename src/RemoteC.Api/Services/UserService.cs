using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public class UserService : IUserService
    {
        private readonly RemoteCDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(RemoteCDbContext context, ILogger<UserService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserDto?> GetUserAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return null;
            }

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
            {
                return null;
            }

            return MapToDto(user);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user {UserId} with email {Email}", user.Id, user.Email);

            return MapToDto(user);
        }

        public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new ArgumentException("Invalid user ID");
            }

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            if (!string.IsNullOrEmpty(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }

            if (!string.IsNullOrEmpty(request.LastName))
            {
                user.LastName = request.LastName;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user {UserId}", user.Id);

            return MapToDto(user);
        }

        public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Enumerable.Empty<string>();
            }

            var permissions = await _context.UserRoles
                .Where(ur => ur.UserId == userGuid)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Contains(permission);
        }

        // Helper method for AuthController
        public async Task<UserDto> CreateOrUpdateUserAsync(string email, string firstName, string lastName, string azureId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    AzureAdB2CId = azureId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                _logger.LogInformation("Creating new user {UserId} from Azure AD", user.Id);
            }
            else
            {
                user.FirstName = firstName;
                user.LastName = lastName;
                user.AzureAdB2CId = azureId;
                user.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Updating existing user {UserId} from Azure AD", user.Id);
            }

            await _context.SaveChangesAsync();

            return MapToDto(user);
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive
            };
        }
    }
}