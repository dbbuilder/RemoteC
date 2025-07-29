using AutoMapper;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.Name)))
            .ForMember(dest => dest.Permissions, opt => opt.Ignore()); // Will be populated separately

        // Session mappings
        CreateMap<Session, SessionDto>()
            .ForMember(dest => dest.DeviceId, opt => opt.MapFrom(src => src.DeviceId.ToString()))
            .ForMember(dest => dest.DeviceName, opt => opt.MapFrom(src => src.Device.Name))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedByUser.Email))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapSessionStatus(src.Status)))
            .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants));

        CreateMap<Session, SessionSummary>()
            .ForMember(dest => dest.DeviceName, opt => opt.MapFrom(src => src.Device.Name))
            .ForMember(dest => dest.HostName, opt => opt.MapFrom(src => src.Device.HostName))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => $"{src.CreatedByUser.FirstName} {src.CreatedByUser.LastName}"));

        // Session participant mappings
        CreateMap<SessionParticipant, SessionParticipantDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => MapParticipantRole(src.Role)))
            .ForMember(dest => dest.Permissions, opt => opt.Ignore()); // Will be populated based on role

        CreateMap<SessionParticipant, SessionParticipantInfo>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));

        // Device mappings
        CreateMap<Device, DeviceDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedByUser.Email));

        // Audit log mappings
        CreateMap<AuditLog, AuditLogDto>();

        // Role mappings
        CreateMap<RemoteC.Data.Entities.Role, RoleDto>();

        // Permission mappings
        CreateMap<Permission, string>()
            .ConvertUsing(src => src.Name);
    }

    private static RemoteC.Shared.Models.SessionStatus MapSessionStatus(Data.Entities.SessionStatus status)
    {
        return status switch
        {
            Data.Entities.SessionStatus.Created => RemoteC.Shared.Models.SessionStatus.Created,
            Data.Entities.SessionStatus.WaitingForPin => RemoteC.Shared.Models.SessionStatus.WaitingForPin,
            Data.Entities.SessionStatus.Connecting => RemoteC.Shared.Models.SessionStatus.Connecting,
            Data.Entities.SessionStatus.Connected => RemoteC.Shared.Models.SessionStatus.Connected,
            Data.Entities.SessionStatus.Active => RemoteC.Shared.Models.SessionStatus.Active,
            Data.Entities.SessionStatus.Paused => RemoteC.Shared.Models.SessionStatus.Paused,
            Data.Entities.SessionStatus.Disconnected => RemoteC.Shared.Models.SessionStatus.Disconnected,
            Data.Entities.SessionStatus.Ended => RemoteC.Shared.Models.SessionStatus.Ended,
            Data.Entities.SessionStatus.Error => RemoteC.Shared.Models.SessionStatus.Error,
            _ => RemoteC.Shared.Models.SessionStatus.Created
        };
    }

    private static RemoteC.Shared.Models.ParticipantRole MapParticipantRole(Data.Entities.ParticipantRole role)
    {
        return role switch
        {
            Data.Entities.ParticipantRole.Viewer => RemoteC.Shared.Models.ParticipantRole.Viewer,
            Data.Entities.ParticipantRole.Controller => RemoteC.Shared.Models.ParticipantRole.Controller,
            Data.Entities.ParticipantRole.Administrator => RemoteC.Shared.Models.ParticipantRole.Administrator,
            Data.Entities.ParticipantRole.Owner => RemoteC.Shared.Models.ParticipantRole.Owner,
            _ => RemoteC.Shared.Models.ParticipantRole.Viewer
        };
    }
}

public class DeviceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? HostName { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeenAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}