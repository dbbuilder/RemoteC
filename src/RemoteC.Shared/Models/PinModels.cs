namespace RemoteC.Shared.Models;

/// <summary>
/// Extended PIN generation result with additional details
/// </summary>
public class ExtendedPinGenerationResult
{
    /// <summary>
    /// The generated PIN code
    /// </summary>
    public string PinCode { get; set; } = string.Empty;
    
    /// <summary>
    /// The session ID associated with the PIN
    /// </summary>
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// When the PIN expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// The user who created the PIN
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Detailed information about a PIN
/// </summary>
public class PinDetails
{
    /// <summary>
    /// The session ID
    /// </summary>
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// The user ID who created the PIN
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// The device ID associated with the PIN
    /// </summary>
    public string? DeviceId { get; set; }
    
    /// <summary>
    /// When the PIN was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the PIN expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether the PIN has been used
    /// </summary>
    public bool IsUsed { get; set; }
    
    /// <summary>
    /// When the PIN was used
    /// </summary>
    public DateTime? UsedAt { get; set; }
}

/// <summary>
/// Active PIN information (without the actual PIN code)
/// </summary>
public class ActivePinDto
{
    /// <summary>
    /// The session ID
    /// </summary>
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// Masked PIN code for display
    /// </summary>
    public string MaskedPin { get; set; } = string.Empty;
    
    /// <summary>
    /// When the PIN was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the PIN expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether the PIN has been used
    /// </summary>
    public bool IsUsed { get; set; }
    
    /// <summary>
    /// Associated device name
    /// </summary>
    public string? DeviceName { get; set; }
}