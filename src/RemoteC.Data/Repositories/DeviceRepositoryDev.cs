using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace RemoteC.Data.Repositories
{
    /// <summary>
    /// Development repository implementation that uses EF Core directly instead of stored procedures
    /// </summary>
    public class DeviceRepositoryDev : IDeviceRepository
    {
        private readonly RemoteCDbContext _context;
        private readonly IMapper _mapper;

        public DeviceRepositoryDev(RemoteCDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<DeviceDto>> GetUserDevicesAsync(Guid userId, int pageNumber = 1, int pageSize = 25, bool onlineOnly = false)
        {
            var query = _context.Devices
                .Include(d => d.CreatedByUser)
                .Where(d => d.CreatedBy == userId);

            if (onlineOnly)
            {
                query = query.Where(d => d.IsOnline);
            }

            var totalCount = await query.CountAsync();
            
            var devices = await query
                .OrderBy(d => d.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<DeviceDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new PagedResult<DeviceDto>
            {
                Items = devices,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<DeviceDto?> GetDeviceDetailsAsync(Guid deviceId, Guid? userId = null)
        {
            var query = _context.Devices
                .Include(d => d.CreatedByUser)
                .Where(d => d.Id == deviceId);
            
            if (userId.HasValue)
            {
                query = query.Where(d => d.CreatedBy == userId.Value);
            }

            return await query
                .ProjectTo<DeviceDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<DeviceDto> UpsertDeviceAsync(string name, string macAddress, Guid createdBy, 
            string? hostName = null, string? ipAddress = null, string? operatingSystem = null, 
            string? version = null, bool isOnline = true)
        {
            var existingDevice = await _context.Devices
                .Include(d => d.CreatedByUser)
                .FirstOrDefaultAsync(d => d.MacAddress == macAddress && d.CreatedBy == createdBy);

            if (existingDevice != null)
            {
                // Update existing device
                existingDevice.Name = name;
                existingDevice.HostName = hostName;
                existingDevice.IpAddress = ipAddress;
                existingDevice.OperatingSystem = operatingSystem;
                existingDevice.Version = version;
                existingDevice.LastSeenAt = DateTime.UtcNow;
                existingDevice.IsOnline = isOnline;
            }
            else
            {
                // Create new device
                existingDevice = new Device
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    MacAddress = macAddress,
                    CreatedBy = createdBy,
                    HostName = hostName,
                    IpAddress = ipAddress,
                    OperatingSystem = operatingSystem,
                    Version = version,
                    LastSeenAt = DateTime.UtcNow,
                    IsOnline = isOnline,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Devices.Add(existingDevice);
            }

            await _context.SaveChangesAsync();
            
            // Reload with user info for mapping
            await _context.Entry(existingDevice)
                .Reference(d => d.CreatedByUser)
                .LoadAsync();
            
            return _mapper.Map<DeviceDto>(existingDevice);
        }

        public async Task UpdateDeviceStatusAsync(Guid deviceId, bool isOnline, string? ipAddress = null)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null) return;

            device.IsOnline = isOnline;
            device.LastSeenAt = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(ipAddress))
            {
                device.IpAddress = ipAddress;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteDeviceAsync(Guid deviceId)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null) return false;

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DeviceDto>> GetDevicesInGroupAsync(Guid deviceGroupId, int pageNumber = 1, int pageSize = 25)
        {
            // For development, return empty list as device groups might not be set up
            return await Task.FromResult(new List<DeviceDto>());
        }

        public async Task<bool> AddDeviceToGroupAsync(Guid deviceGroupId, Guid deviceId, Guid addedBy)
        {
            // For development, just return true
            return await Task.FromResult(true);
        }

        public async Task<bool> RemoveDeviceFromGroupAsync(Guid deviceGroupId, Guid deviceId)
        {
            // For development, just return true
            return await Task.FromResult(true);
        }

        public async Task<int> GetDeviceCountAsync()
        {
            // For development, return the count of devices from the database
            return await _context.Devices.CountAsync();
        }

        public async Task<int> GetOnlineDeviceCountAsync()
        {
            // For development, return the count of online devices from the database
            return await _context.Devices.Where(d => d.IsOnline).CountAsync();
        }
    }
}