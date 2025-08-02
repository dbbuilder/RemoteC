using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using AutoMapper;
using System.Data;

namespace RemoteC.Data.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly RemoteCDbContext _context;
    private readonly IMapper _mapper;

    public DeviceRepository(RemoteCDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<DeviceDto>> GetUserDevicesAsync(Guid userId, int pageNumber = 1, int pageSize = 25, bool onlineOnly = false)
    {
        var parameters = new[]
        {
            new SqlParameter("@UserId", userId),
            new SqlParameter("@PageNumber", pageNumber),
            new SqlParameter("@PageSize", pageSize),
            new SqlParameter("@OnlineOnly", onlineOnly)
        };

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "sp_GetUserDevices";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddRange(parameters);

        await _context.Database.OpenConnectionAsync();

        var devices = new List<Device>();
        int totalCount = 0;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            devices.Add(new Device
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                HostName = reader.IsDBNull(reader.GetOrdinal("HostName")) ? null : reader.GetString(reader.GetOrdinal("HostName")),
                IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                MacAddress = reader.IsDBNull(reader.GetOrdinal("MacAddress")) ? null : reader.GetString(reader.GetOrdinal("MacAddress")),
                OperatingSystem = reader.IsDBNull(reader.GetOrdinal("OperatingSystem")) ? null : reader.GetString(reader.GetOrdinal("OperatingSystem")),
                Version = reader.IsDBNull(reader.GetOrdinal("Version")) ? null : reader.GetString(reader.GetOrdinal("Version")),
                IsOnline = reader.GetBoolean(reader.GetOrdinal("IsOnline")),
                LastSeenAt = reader.GetDateTime(reader.GetOrdinal("LastSeenAt")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });

            if (totalCount == 0)
                totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
        }

        // Map entities to DTOs
        var deviceDtos = devices.Select(d => _mapper.Map<DeviceDto>(d)).ToList();
        
        return new PagedResult<DeviceDto>
        {
            Items = deviceDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<DeviceDto?> GetDeviceDetailsAsync(Guid deviceId, Guid? userId = null)
    {
        var parameters = new[]
        {
            new SqlParameter("@DeviceId", deviceId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value)
        };

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "sp_GetDeviceDetails";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddRange(parameters);

        await _context.Database.OpenConnectionAsync();

        Device? device = null;

        using var reader = await command.ExecuteReaderAsync();
        
        // Read device details
        if (await reader.ReadAsync())
        {
            device = new Device
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                HostName = reader.IsDBNull(reader.GetOrdinal("HostName")) ? null : reader.GetString(reader.GetOrdinal("HostName")),
                IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                MacAddress = reader.IsDBNull(reader.GetOrdinal("MacAddress")) ? null : reader.GetString(reader.GetOrdinal("MacAddress")),
                OperatingSystem = reader.IsDBNull(reader.GetOrdinal("OperatingSystem")) ? null : reader.GetString(reader.GetOrdinal("OperatingSystem")),
                Version = reader.IsDBNull(reader.GetOrdinal("Version")) ? null : reader.GetString(reader.GetOrdinal("Version")),
                IsOnline = reader.GetBoolean(reader.GetOrdinal("IsOnline")),
                LastSeenAt = reader.GetDateTime(reader.GetOrdinal("LastSeenAt")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                CreatedBy = reader.GetGuid(reader.GetOrdinal("CreatedById"))
            };
        }

        return device != null ? _mapper.Map<DeviceDto>(device) : null;
    }

    public async Task<DeviceDto> UpsertDeviceAsync(string name, string macAddress, Guid createdBy, 
        string? hostName = null, string? ipAddress = null, string? operatingSystem = null, 
        string? version = null, bool isOnline = true)
    {
        var deviceIdParam = new SqlParameter("@DeviceId", SqlDbType.UniqueIdentifier)
        {
            Direction = ParameterDirection.Output
        };

        var parameters = new[]
        {
            deviceIdParam,
            new SqlParameter("@Name", name),
            new SqlParameter("@MacAddress", macAddress),
            new SqlParameter("@CreatedBy", createdBy),
            new SqlParameter("@HostName", (object?)hostName ?? DBNull.Value),
            new SqlParameter("@IpAddress", (object?)ipAddress ?? DBNull.Value),
            new SqlParameter("@OperatingSystem", (object?)operatingSystem ?? DBNull.Value),
            new SqlParameter("@Version", (object?)version ?? DBNull.Value),
            new SqlParameter("@IsOnline", isOnline)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_UpsertDevice @DeviceId OUTPUT, @Name, @MacAddress, @CreatedBy, @HostName, @IpAddress, @OperatingSystem, @Version, @IsOnline",
            parameters);

        var deviceId = (Guid)deviceIdParam.Value;
        return (await GetDeviceDetailsAsync(deviceId))!;
    }

    public async Task UpdateDeviceStatusAsync(Guid deviceId, bool isOnline, string? ipAddress = null)
    {
        var parameters = new[]
        {
            new SqlParameter("@DeviceId", deviceId),
            new SqlParameter("@IsOnline", isOnline),
            new SqlParameter("@IpAddress", (object?)ipAddress ?? DBNull.Value)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_UpdateDeviceStatus @DeviceId, @IsOnline, @IpAddress",
            parameters);
    }

    public async Task<bool> DeleteDeviceAsync(Guid deviceId)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device != null)
        {
            _context.Devices.Remove(device);
            return await _context.SaveChangesAsync() > 0;
        }
        return false;
    }

    public async Task<IEnumerable<DeviceDto>> GetDevicesInGroupAsync(Guid deviceGroupId, int pageNumber = 1, int pageSize = 25)
    {
        var parameters = new[]
        {
            new SqlParameter("@DeviceGroupId", deviceGroupId),
            new SqlParameter("@PageNumber", pageNumber),
            new SqlParameter("@PageSize", pageSize)
        };

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "sp_GetDevicesInGroup";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddRange(parameters);

        await _context.Database.OpenConnectionAsync();

        var devices = new List<Device>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            devices.Add(new Device
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                HostName = reader.IsDBNull(reader.GetOrdinal("HostName")) ? null : reader.GetString(reader.GetOrdinal("HostName")),
                IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                MacAddress = reader.IsDBNull(reader.GetOrdinal("MacAddress")) ? null : reader.GetString(reader.GetOrdinal("MacAddress")),
                OperatingSystem = reader.IsDBNull(reader.GetOrdinal("OperatingSystem")) ? null : reader.GetString(reader.GetOrdinal("OperatingSystem")),
                IsOnline = reader.GetBoolean(reader.GetOrdinal("IsOnline")),
                LastSeenAt = reader.GetDateTime(reader.GetOrdinal("LastSeenAt"))
            });
        }

        return devices.Select(d => _mapper.Map<DeviceDto>(d)).ToList();
    }

    public async Task<bool> AddDeviceToGroupAsync(Guid deviceGroupId, Guid deviceId, Guid addedBy)
    {
        var parameters = new[]
        {
            new SqlParameter("@DeviceGroupId", deviceGroupId),
            new SqlParameter("@DeviceId", deviceId),
            new SqlParameter("@AddedBy", addedBy)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_AddDeviceToGroup @DeviceGroupId, @DeviceId, @AddedBy",
            parameters);

        return true;
    }

    public async Task<bool> RemoveDeviceFromGroupAsync(Guid deviceGroupId, Guid deviceId)
    {
        var parameters = new[]
        {
            new SqlParameter("@DeviceGroupId", deviceGroupId),
            new SqlParameter("@DeviceId", deviceId)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_RemoveDeviceFromGroup @DeviceGroupId, @DeviceId",
            parameters);

        return true;
    }

    public async Task<int> GetDeviceCountAsync()
    {
        var result = await _context.Database.SqlQuery<int>($"SELECT COUNT(*) FROM Devices").FirstOrDefaultAsync();
        return result;
    }

    public async Task<int> GetOnlineDeviceCountAsync()
    {
        var result = await _context.Database.SqlQuery<int>($"SELECT COUNT(*) FROM Devices WHERE IsOnline = 1").FirstOrDefaultAsync();
        return result;
    }
}