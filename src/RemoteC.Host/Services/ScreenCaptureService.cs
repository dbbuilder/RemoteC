using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Host.Services;

/// <summary>
/// Service for capturing screen content
/// </summary>
public interface IScreenCaptureService
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<ScreenData?> CaptureScreenAsync(int monitorIndex, QualitySettings quality, CancellationToken cancellationToken);
    Task<MonitorInfo[]> GetMonitorsAsync();
    Task DisposeAsync();
}

public class ScreenCaptureService : IScreenCaptureService
{
    private readonly ILogger<ScreenCaptureService> _logger;
    private readonly IRemoteControlProvider _remoteControlProvider;
    private bool _isInitialized;
    private readonly SemaphoreSlim _captureLock = new(1, 1);
    private Bitmap? _previousFrame;
    private readonly object _frameLock = new();

    public ScreenCaptureService(
        ILogger<ScreenCaptureService> logger,
        IRemoteControlProvider remoteControlProvider)
    {
        _logger = logger;
        _remoteControlProvider = remoteControlProvider;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
            return;

        _logger.LogInformation("Initializing screen capture service");
        
        // Initialize the remote control provider
        if (!await _remoteControlProvider.InitializeAsync())
        {
            throw new InvalidOperationException("Failed to initialize remote control provider");
        }

        _isInitialized = true;
        _logger.LogInformation("Screen capture service initialized successfully");
    }

    public async Task<ScreenData?> CaptureScreenAsync(
        int monitorIndex, 
        QualitySettings quality, 
        CancellationToken cancellationToken)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Screen capture service not initialized");

        await _captureLock.WaitAsync(cancellationToken);
        try
        {
            // For Phase 1, we use the provider (ControlR)
            // For Phase 2, this will use the Rust engine
            var frame = await _remoteControlProvider.CaptureScreenAsync("current-session");
            
            if (frame == null || frame.Data.Length == 0)
                return null;

            // Apply quality settings
            var processedData = await ProcessFrameAsync(frame, quality, cancellationToken);
            
            return new ScreenData
            {
                MonitorIndex = monitorIndex,
                Width = frame.Width,
                Height = frame.Height,
                Data = processedData,
                Timestamp = DateTime.UtcNow,
                IsKeyFrame = frame.IsKeyFrame,
                CompressionType = quality.CompressionType,
                Quality = quality.Quality
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing screen");
            return null;
        }
        finally
        {
            _captureLock.Release();
        }
    }

    public async Task<MonitorInfo[]> GetMonitorsAsync()
    {
        return await Task.Run(() =>
        {
            var monitors = new List<MonitorInfo>();
            var index = 0;

            // TODO: Implement proper screen enumeration
            // For now, return a single primary monitor
            monitors.Add(new MonitorInfo
            {
                Index = 0,
                Name = "Primary Monitor",
                IsPrimary = true,
                Bounds = new RemoteC.Shared.Models.Rectangle
                {
                    X = 0,
                    Y = 0,
                    Width = 1920,
                    Height = 1080
                },
                WorkArea = new RemoteC.Shared.Models.Rectangle
                {
                    X = 0,
                    Y = 0,
                    Width = 1920,
                    Height = 1040  // Assuming 40px taskbar
                },
                BitDepth = 32,
                RefreshRate = 60
            });

            return monitors.ToArray();
        });
    }

    private async Task<byte[]> ProcessFrameAsync(
        ScreenFrame frame, 
        QualitySettings quality, 
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            // Apply compression based on quality settings
            using var ms = new MemoryStream(frame.Data);
            using var bitmap = new Bitmap(ms);
            
            // Apply quality adjustments
            if (quality.Scale < 1.0f)
            {
                var newWidth = (int)(bitmap.Width * quality.Scale);
                var newHeight = (int)(bitmap.Height * quality.Scale);
                using var scaled = new Bitmap(newWidth, newHeight);
                using var g = Graphics.FromImage(scaled);
                
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, newWidth, newHeight);
                
                return CompressBitmap(scaled, quality);
            }
            
            return CompressBitmap(bitmap, quality);
        }, cancellationToken);
    }

    private byte[] CompressBitmap(Bitmap bitmap, QualitySettings quality)
    {
        using var ms = new MemoryStream();
        
        var encoder = GetEncoder(quality.CompressionType);
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality.Quality);
        
        bitmap.Save(ms, encoder, encoderParams);
        return ms.ToArray();
    }

    private ImageCodecInfo GetEncoder(CompressionType type)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        var mimeType = type switch
        {
            CompressionType.Jpeg => "image/jpeg",
            CompressionType.Png => "image/png",
            CompressionType.WebP => "image/webp",
            _ => "image/jpeg"
        };
        
        return codecs.FirstOrDefault(c => c.MimeType == mimeType) 
            ?? codecs.First(c => c.MimeType == "image/jpeg");
    }

    public async Task DisposeAsync()
    {
        _logger.LogInformation("Disposing screen capture service");
        
        lock (_frameLock)
        {
            _previousFrame?.Dispose();
            _previousFrame = null;
        }
        
        _captureLock?.Dispose();
        await Task.CompletedTask;
    }
}

public class ScreenData
{
    public int MonitorIndex { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; }
    public bool IsKeyFrame { get; set; }
    public CompressionType CompressionType { get; set; }
    public int Quality { get; set; }
}

public class QualitySettings
{
    public int TargetFps { get; set; } = 30;
    public float Scale { get; set; } = 1.0f;
    public int Quality { get; set; } = 85;
    public CompressionType CompressionType { get; set; } = CompressionType.Jpeg;
    public bool UseHardwareEncoding { get; set; } = true;
    public int Bitrate { get; set; } = 5000000; // 5 Mbps
}

public enum CompressionType
{
    None,
    Jpeg,
    Png,
    WebP,
    H264,
    H265,
    VP8,
    VP9
}