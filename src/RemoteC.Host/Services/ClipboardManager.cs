using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Host.Services
{
    /// <summary>
    /// Manages clipboard operations on the host machine
    /// </summary>
    public class ClipboardManager : IClipboardManager
    {
        private readonly ILogger<ClipboardManager> _logger;
        private readonly IClipboardAccess _clipboardAccess;
        private readonly List<ClipboardContent> _history = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private CancellationTokenSource? _monitoringCts;
        private string? _lastContentHash;

        public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;

        public long MaxContentSize { get; set; } = 10 * 1024 * 1024; // 10MB

        public ClipboardManager(ILogger<ClipboardManager> logger, IClipboardAccess clipboardAccess)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clipboardAccess = clipboardAccess ?? throw new ArgumentNullException(nameof(clipboardAccess));
            
            // Subscribe to clipboard access events
            _clipboardAccess.ClipboardChanged += OnClipboardAccessChanged;
        }

        public async Task<ClipboardContent?> GetClipboardContentAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_clipboardAccess.ContainsText())
                {
                    var text = await _clipboardAccess.GetTextAsync();
                    if (string.IsNullOrEmpty(text))
                        return null;

                    var content = new ClipboardContent
                    {
                        Type = ClipboardContentType.Text,
                        Text = text.Length > MaxContentSize ? text.Substring(0, (int)MaxContentSize) : text,
                        Size = text.Length,
                        IsTruncated = text.Length > MaxContentSize,
                        Timestamp = DateTime.UtcNow
                    };
                    content.ContentHash = ComputeHash(content);
                    return content;
                }
                else if (_clipboardAccess.ContainsImage())
                {
                    var imageData = await _clipboardAccess.GetImageAsync();
                    if (imageData == null || imageData.Length == 0)
                        return null;

                    // Detect image format
                    var format = DetectImageFormat(imageData);
                    
                    var content = new ClipboardContent
                    {
                        Type = ClipboardContentType.Image,
                        ImageData = imageData.Length > MaxContentSize 
                            ? imageData.Take((int)MaxContentSize).ToArray() 
                            : imageData,
                        ImageFormat = format,
                        Size = imageData.Length,
                        IsTruncated = imageData.Length > MaxContentSize,
                        Timestamp = DateTime.UtcNow
                    };
                    content.ContentHash = ComputeHash(content);
                    return content;
                }
                else if (_clipboardAccess.ContainsFileDropList())
                {
                    var files = await _clipboardAccess.GetFileDropListAsync();
                    if (files == null || files.Length == 0)
                        return null;

                    var content = new ClipboardContent
                    {
                        Type = ClipboardContentType.FileList,
                        Files = files.Select(f => new ClipboardFile
                        {
                            Path = f,
                            Name = System.IO.Path.GetFileName(f),
                            Size = System.IO.File.Exists(f) ? new System.IO.FileInfo(f).Length : 0,
                            IsAccessible = System.IO.File.Exists(f)
                        }).ToArray(),
                        Size = files.Length,
                        Timestamp = DateTime.UtcNow
                    };
                    content.ContentHash = ComputeHash(content);
                    return content;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clipboard content");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> SetClipboardContentAsync(ClipboardContent content)
        {
            if (content == null)
                return false;

            await _semaphore.WaitAsync();
            try
            {
                switch (content.Type)
                {
                    case ClipboardContentType.Text:
                        if (!string.IsNullOrEmpty(content.Text))
                        {
                            return await _clipboardAccess.SetTextAsync(content.Text);
                        }
                        break;

                    case ClipboardContentType.Image:
                        if (content.ImageData != null && content.ImageData.Length > 0)
                        {
                            return await _clipboardAccess.SetImageAsync(content.ImageData);
                        }
                        break;

                    case ClipboardContentType.Html:
                        if (!string.IsNullOrEmpty(content.Html))
                        {
                            // Set HTML with plain text fallback
                            return await _clipboardAccess.SetHtmlAsync(content.Html, content.Text ?? string.Empty);
                        }
                        break;

                    case ClipboardContentType.FileList:
                        _logger.LogWarning("Setting file list to clipboard is not supported");
                        return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting clipboard content");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> ClearClipboardAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return await _clipboardAccess.ClearAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing clipboard");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task StartMonitoringAsync(int intervalMs = 500)
        {
            if (_monitoringCts != null)
            {
                await StopMonitoringAsync();
            }

            _monitoringCts = new CancellationTokenSource();
            var token = _monitoringCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(intervalMs, token);
                        // Monitoring is handled by clipboard access events
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);

            _logger.LogInformation("Started clipboard monitoring with interval {IntervalMs}ms", intervalMs);
        }

        public async Task StopMonitoringAsync()
        {
            if (_monitoringCts != null)
            {
                _monitoringCts.Cancel();
                _monitoringCts.Dispose();
                _monitoringCts = null;
            }

            _logger.LogInformation("Stopped clipboard monitoring");
            await Task.CompletedTask;
        }

        public async Task AddToHistoryAsync(ClipboardContent content)
        {
            await _semaphore.WaitAsync();
            try
            {
                _history.Add(content);
                
                // Keep only last 100 items
                if (_history.Count > 100)
                {
                    _history.RemoveRange(0, _history.Count - 100);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<ClipboardContent>> GetHistoryAsync(int maxItems)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _history
                    .OrderByDescending(h => h.Timestamp)
                    .Take(maxItems)
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public bool IsFormatSupported(ClipboardContentType type)
        {
            return _clipboardAccess.IsFormatSupported(type);
        }

        private async void OnClipboardAccessChanged(object? sender, EventArgs e)
        {
            try
            {
                var content = await GetClipboardContentAsync();
                if (content != null)
                {
                    // Check if content actually changed
                    if (content.ContentHash != _lastContentHash)
                    {
                        _lastContentHash = content.ContentHash;
                        await AddToHistoryAsync(content);
                        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs { Content = content });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling clipboard change");
            }
        }

        private string DetectImageFormat(byte[] imageData)
        {
            if (imageData.Length < 4)
                return "Unknown";

            // PNG signature
            if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
                return "PNG";

            // JPEG signature
            if (imageData[0] == 0xFF && imageData[1] == 0xD8)
                return "JPEG";

            // BMP signature
            if (imageData[0] == 0x42 && imageData[1] == 0x4D)
                return "BMP";

            // GIF signature
            if (imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46)
                return "GIF";

            return "Unknown";
        }

        private string ComputeHash(ClipboardContent content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] data;

            switch (content.Type)
            {
                case ClipboardContentType.Text:
                    data = Encoding.UTF8.GetBytes(content.Text ?? string.Empty);
                    break;
                case ClipboardContentType.Image:
                    data = content.ImageData ?? Array.Empty<byte>();
                    break;
                case ClipboardContentType.FileList:
                    data = Encoding.UTF8.GetBytes(string.Join(";", content.Files?.Select(f => f.Path) ?? Array.Empty<string>()));
                    break;
                default:
                    data = Array.Empty<byte>();
                    break;
            }

            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        public void Dispose()
        {
            _monitoringCts?.Cancel();
            _monitoringCts?.Dispose();
            _clipboardAccess.ClipboardChanged -= OnClipboardAccessChanged;
            _semaphore?.Dispose();
        }
    }

    /// <summary>
    /// Event args for clipboard changes
    /// </summary>
    public class ClipboardChangedEventArgs : EventArgs
    {
        public ClipboardContent Content { get; set; } = new();
    }

    /// <summary>
    /// Interface for clipboard manager
    /// </summary>
    public interface IClipboardManager : IDisposable
    {
        event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;
        long MaxContentSize { get; set; }
        Task<ClipboardContent?> GetClipboardContentAsync();
        Task<bool> SetClipboardContentAsync(ClipboardContent content);
        Task<bool> ClearClipboardAsync();
        Task StartMonitoringAsync(int intervalMs = 500);
        Task StopMonitoringAsync();
        Task AddToHistoryAsync(ClipboardContent content);
        Task<List<ClipboardContent>> GetHistoryAsync(int maxItems);
        bool IsFormatSupported(ClipboardContentType type);
    }

    /// <summary>
    /// Interface for platform-specific clipboard access
    /// </summary>
    public interface IClipboardAccess
    {
        event EventHandler? ClipboardChanged;
        bool ContainsText();
        bool ContainsImage();
        bool ContainsFileDropList();
        Task<string?> GetTextAsync();
        Task<byte[]?> GetImageAsync();
        Task<string[]?> GetFileDropListAsync();
        Task<bool> SetTextAsync(string text);
        Task<bool> SetImageAsync(byte[] imageData);
        Task<bool> SetHtmlAsync(string html, string textFallback);
        Task<bool> ClearAsync();
        bool IsFormatSupported(ClipboardContentType type);
    }
}