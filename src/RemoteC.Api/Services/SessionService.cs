using Microsoft.EntityFrameworkCore;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using AutoMapper;

namespace RemoteC.Api.Services;

/// <summary>
/// Session management service implementation
/// </summary>
public class SessionService : ISessionService
{
    private readonly RemoteCDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPinService _pinService;
    private readonly IRemoteControlService _remoteControlService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        RemoteCDbContext context,
        IMapper mapper,
        IPinService pinService,
        IRemoteControlService remoteControlService,
        IAuditService auditService,
        ILogger<SessionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _pinService = pinService ?? throw new ArgumentNullException(nameof(pinService));
        _remoteControlService = remoteControlService ?? throw new ArgumentNullException(nameof(remoteControlService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<SessionDto>> GetUserSessionsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting sessions for user {UserId}", userId);

            var sessions = await _context.Sessions
                .Include(s => s.Device)
                .Include(s => s.CreatedByUser)
                .Include(s => s.Participants)
                    .ThenInclude(p => p.User)
                .Where(s => s.CreatedBy.ToString() == userId || 
                           s.Participants.Any(p => p.UserId.ToString() == userId))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var sessionDtos = _mapper.Map<IEnumerable<SessionDto>>(sessions);

            await _auditService.LogActionAsync("session.list", "Session", string.Empty);

            return sessionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SessionDto?> GetSessionAsync(Guid sessionId, string userId)
    {
        try
        {
            _logger.LogInformation("Getting session {SessionId} for user {UserId}", sessionId, userId);

            var session = await _context.Sessions
                .Include(s => s.Device)
                .Include(s => s.CreatedByUser)
                .Include(s => s.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", sessionId);
                return null;
            }

            // Check if user has access to this session
            var hasAccess = session.CreatedBy.ToString() == userId ||
                           session.Participants.Any(p => p.UserId.ToString() == userId);

            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} does not have access to session {SessionId}", userId, sessionId);
                throw new UnauthorizedAccessException("Access denied to session");
            }

            var sessionDto = _mapper.Map<SessionDto>(session);

            await _auditService.LogActionAsync("session.view", "Session", sessionId.ToString(), userId);

            return sessionDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<SessionDto> CreateSessionAsync(CreateSessionRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Creating session for device {DeviceId} by user {UserId}", request.DeviceId, userId);

            // Validate device exists
            var device = await _context.Devices.FirstOrDefaultAsync(d => d.Id.ToString() == request.DeviceId);
            if (device == null)
            {
                throw new ArgumentException($"Device {request.DeviceId} not found");
            }

            // Create session entity
            Guid createdByGuid;
            if (string.IsNullOrEmpty(userId) || userId == "Development User")
            {
                createdByGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"); // Development user
            }
            else if (Guid.TryParse(userId, out var parsedGuid))
            {
                createdByGuid = parsedGuid;
            }
            else
            {
                _logger.LogWarning("Invalid user ID format: {UserId}, using development user", userId);
                createdByGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
            }
                
            var session = new Session
            {
                Name = request.Name,
                DeviceId = device.Id,
                CreatedBy = createdByGuid,
                Type = (Data.Entities.SessionType)request.Type,
                Status = Data.Entities.SessionStatus.Created,
                RequirePin = request.RequirePin
            };

            _context.Sessions.Add(session);

            // Add creator as owner participant
            var ownerParticipant = new SessionParticipant
            {
                SessionId = session.Id,
                UserId = createdByGuid,
                Role = Data.Entities.ParticipantRole.Owner
            };

            _context.SessionParticipants.Add(ownerParticipant);

            // Add invited users as participants
            foreach (var invitedUserId in request.InvitedUsers)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == invitedUserId);
                if (user != null)
                {
                    var participant = new SessionParticipant
                    {
                        SessionId = session.Id,
                        UserId = user.Id,
                        Role = Data.Entities.ParticipantRole.Viewer
                    };

                    _context.SessionParticipants.Add(participant);
                }
            }

            _logger.LogInformation("About to save session {SessionId} to database", session.Id);
            var changesSaved = await _context.SaveChangesAsync();
            _logger.LogInformation("SaveChangesAsync returned {ChangeCount} changes for session {SessionId}", changesSaved, session.Id);

            var sessionDto = _mapper.Map<SessionDto>(session);

            await _auditService.LogActionAsync("session.create", "Session", session.Id.ToString(), userId);

            _logger.LogInformation("Session {SessionId} created successfully", session.Id);

            return sessionDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            throw;
        }
    }

    public async Task<SessionStartResult> StartSessionAsync(Guid sessionId, string userId)
    {
        try
        {
            _logger.LogInformation("Starting session {SessionId} for user {UserId}", sessionId, userId);

            var session = await _context.Sessions
                .Include(s => s.Device)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new ArgumentException($"Session {sessionId} not found");
            }

            // Check permissions
            var hasAccess = session.CreatedBy.ToString() == userId ||
                           await _context.SessionParticipants
                               .AnyAsync(p => p.SessionId == sessionId && p.UserId.ToString() == userId);

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("Access denied to session");
            }

            // Generate PIN if required
            string? pin = null;
            if (session.RequirePin)
            {
                pin = await _pinService.GeneratePinAsync(sessionId);
                session.Status = Data.Entities.SessionStatus.WaitingForPin;
            }
            else
            {
                session.Status = Data.Entities.SessionStatus.Connecting;
            }

            session.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Start remote control session with ControlR
            var connectionUrl = await _remoteControlService.StartRemoteSessionAsync(sessionId, session.DeviceId.ToString());

            var result = new SessionStartResult
            {
                Success = true,
                ConnectionUrl = connectionUrl,
                Pin = pin,
                ConnectionInfo = new Dictionary<string, object>
                {
                    { "sessionId", sessionId },
                    { "deviceId", session.DeviceId },
                    { "requirePin", session.RequirePin }
                }
            };

            await _auditService.LogActionAsync("session.start", "Session", sessionId.ToString(), userId);

            _logger.LogInformation("Session {SessionId} started successfully", sessionId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting session {SessionId}", sessionId);

            return new SessionStartResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task StopSessionAsync(Guid sessionId, string userId)
    {
        try
        {
            _logger.LogInformation("Stopping session {SessionId} for user {UserId}", sessionId, userId);

            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session {sessionId} not found");
            }

            // Check permissions
            var hasAccess = session.CreatedBy.ToString() == userId ||
                           await _context.SessionParticipants
                               .AnyAsync(p => p.SessionId == sessionId && p.UserId.ToString() == userId &&
                                           (p.Role == Data.Entities.ParticipantRole.Owner ||
                                            p.Role == Data.Entities.ParticipantRole.Administrator));

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("Access denied to stop session");
            }

            // Stop remote control session
            await _remoteControlService.StopRemoteSessionAsync(sessionId);

            // Update session status
            session.Status = Data.Entities.SessionStatus.Ended;
            session.EndedAt = DateTime.UtcNow;

            // Mark all participants as disconnected
            var participants = await _context.SessionParticipants
                .Where(p => p.SessionId == sessionId && p.IsConnected)
                .ToListAsync();

            foreach (var participant in participants)
            {
                participant.IsConnected = false;
                participant.LeftAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync("session.stop", "Session", sessionId.ToString(), userId);

            _logger.LogInformation("Session {SessionId} stopped successfully", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<PinGenerationResult> GeneratePinAsync(Guid sessionId, string userId)
    {
        try
        {
            _logger.LogInformation("Generating PIN for session {SessionId} by user {UserId}", sessionId, userId);

            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session {sessionId} not found");
            }

            // Check permissions
            var hasAccess = session.CreatedBy.ToString() == userId ||
                           await _context.SessionParticipants
                               .AnyAsync(p => p.SessionId == sessionId && p.UserId.ToString() == userId &&
                                           (p.Role == Data.Entities.ParticipantRole.Owner ||
                                            p.Role == Data.Entities.ParticipantRole.Administrator));

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("Access denied to generate PIN");
            }

            var pin = await _pinService.GeneratePinAsync(sessionId);
            var expiresAt = DateTime.UtcNow.AddMinutes(10); // PIN expires in 10 minutes

            var result = new PinGenerationResult
            {
                Pin = pin,
                ExpiresAt = expiresAt,
                SmsDelivered = false, // TODO: Implement SMS delivery
                EmailDelivered = false // TODO: Implement email delivery
            };

            await _auditService.LogActionAsync("session.pin.generate", "Session", sessionId.ToString(), userId);

            _logger.LogInformation("PIN generated for session {SessionId}", sessionId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PIN for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> ValidatePinAsync(Guid sessionId, string pin)
    {
        try
        {
            _logger.LogInformation("Validating PIN for session {SessionId}", sessionId);

            var isValid = await _pinService.ValidatePinAsync(sessionId, pin);

            if (isValid)
            {
                // Update session status to connected
                var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
                if (session != null)
                {
                    session.Status = Data.Entities.SessionStatus.Connected;
                    await _context.SaveChangesAsync();
                }
            }

            await _auditService.LogActionAsync("session.pin.validate", "Session", sessionId.ToString(), 
                null, null, new { isValid, pin = "***" });

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task UpdateSessionStatusAsync(Guid sessionId, RemoteC.Shared.Models.SessionStatus status)
    {
        try
        {
            _logger.LogInformation("Updating session {SessionId} status to {Status}", sessionId, status);

            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session {sessionId} not found");
            }

            // Map from shared model SessionStatus to entity SessionStatus
            session.Status = status switch
            {
                RemoteC.Shared.Models.SessionStatus.Created => Data.Entities.SessionStatus.Created,
                RemoteC.Shared.Models.SessionStatus.WaitingForPin => Data.Entities.SessionStatus.WaitingForPin,
                RemoteC.Shared.Models.SessionStatus.Connecting => Data.Entities.SessionStatus.Connecting,
                RemoteC.Shared.Models.SessionStatus.Connected => Data.Entities.SessionStatus.Connected,
                RemoteC.Shared.Models.SessionStatus.Active => Data.Entities.SessionStatus.Active,
                RemoteC.Shared.Models.SessionStatus.Paused => Data.Entities.SessionStatus.Paused,
                RemoteC.Shared.Models.SessionStatus.Disconnected => Data.Entities.SessionStatus.Disconnected,
                RemoteC.Shared.Models.SessionStatus.Ended => Data.Entities.SessionStatus.Ended,
                RemoteC.Shared.Models.SessionStatus.Error => Data.Entities.SessionStatus.Error,
                _ => Data.Entities.SessionStatus.Created // Default to Created instead of Unknown
            };

            // Update timestamps based on status
            if (status == RemoteC.Shared.Models.SessionStatus.Active && session.StartedAt == null)
            {
                session.StartedAt = DateTime.UtcNow;
            }
            else if (status == RemoteC.Shared.Models.SessionStatus.Ended && session.EndedAt == null)
            {
                session.EndedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session {SessionId} status updated to {Status}", sessionId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionId} status", sessionId);
            throw;
        }
    }
}