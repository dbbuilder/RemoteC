using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Data.Repositories;

public interface IDeviceRepository
{
    Task<PagedResult<DeviceDto>> GetUserDevicesAsync(Guid userId, int pageNumber = 1, int pageSize = 25, bool onlineOnly = false);
    Task<DeviceDto?> GetDeviceDetailsAsync(Guid deviceId, Guid? userId = null);
    Task<DeviceDto> UpsertDeviceAsync(string name, string macAddress, Guid createdBy, 
        string? hostName = null, string? ipAddress = null, string? operatingSystem = null, 
        string? version = null, bool isOnline = true);
    Task UpdateDeviceStatusAsync(Guid deviceId, bool isOnline, string? ipAddress = null);
    Task<bool> DeleteDeviceAsync(Guid deviceId);
    Task<IEnumerable<DeviceDto>> GetDevicesInGroupAsync(Guid deviceGroupId, int pageNumber = 1, int pageSize = 25);
    Task<bool> AddDeviceToGroupAsync(Guid deviceGroupId, Guid deviceId, Guid addedBy);
    Task<bool> RemoveDeviceFromGroupAsync(Guid deviceGroupId, Guid deviceId);
    Task<int> GetDeviceCountAsync();
    Task<int> GetOnlineDeviceCountAsync();
}