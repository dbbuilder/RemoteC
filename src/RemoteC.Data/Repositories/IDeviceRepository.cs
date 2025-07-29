using RemoteC.Data.Entities;

namespace RemoteC.Data.Repositories;

public interface IDeviceRepository
{
    Task<PagedResult<Device>> GetUserDevicesAsync(Guid userId, int pageNumber = 1, int pageSize = 25, bool onlineOnly = false);
    Task<Device?> GetDeviceDetailsAsync(Guid deviceId, Guid? userId = null);
    Task<Device> UpsertDeviceAsync(string name, string macAddress, Guid createdBy, 
        string? hostName = null, string? ipAddress = null, string? operatingSystem = null, 
        string? version = null, bool isOnline = true);
    Task UpdateDeviceStatusAsync(Guid deviceId, bool isOnline, string? ipAddress = null);
    Task<bool> DeleteDeviceAsync(Guid deviceId);
    Task<IEnumerable<Device>> GetDevicesInGroupAsync(Guid deviceGroupId, int pageNumber = 1, int pageSize = 25);
    Task<bool> AddDeviceToGroupAsync(Guid deviceGroupId, Guid deviceId, Guid addedBy);
    Task<bool> RemoveDeviceFromGroupAsync(Guid deviceGroupId, Guid deviceId);
}