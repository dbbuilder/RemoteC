using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Host.Services
{
    /// <summary>
    /// Cross-platform clipboard access implementation
    /// </summary>
    public class CrossPlatformClipboardAccess : IClipboardAccess
    {
        private string? _clipboardText;
        private byte[]? _clipboardImage;
        private List<string>? _clipboardFiles;
        private readonly object _lock = new object();

        public event EventHandler? ClipboardChanged;

        public bool ContainsText()
        {
            lock (_lock)
            {
                return !string.IsNullOrEmpty(_clipboardText);
            }
        }

        public bool ContainsImage()
        {
            lock (_lock)
            {
                return _clipboardImage != null && _clipboardImage.Length > 0;
            }
        }

        public bool ContainsFileDropList()
        {
            lock (_lock)
            {
                return _clipboardFiles != null && _clipboardFiles.Count > 0;
            }
        }

        public Task<string?> GetTextAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_clipboardText);
            }
        }

        public Task<byte[]?> GetImageAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_clipboardImage);
            }
        }

        public Task<string[]?> GetFileDropListAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_clipboardFiles?.ToArray());
            }
        }

        public Task<bool> SetTextAsync(string text)
        {
            lock (_lock)
            {
                _clipboardText = text;
                _clipboardImage = null;
                _clipboardFiles = null;
            }
            
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        public Task<bool> SetImageAsync(byte[] imageData)
        {
            lock (_lock)
            {
                _clipboardImage = imageData;
                _clipboardText = null;
                _clipboardFiles = null;
            }
            
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        public Task SetFileDropListAsync(string[] files)
        {
            lock (_lock)
            {
                _clipboardFiles = new List<string>(files);
                _clipboardText = null;
                _clipboardImage = null;
            }
            
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task<bool> ClearAsync()
        {
            lock (_lock)
            {
                _clipboardText = null;
                _clipboardImage = null;
                _clipboardFiles = null;
            }
            
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        public Task<ClipboardContent?> GetContentAsync()
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_clipboardText))
                {
                    return Task.FromResult<ClipboardContent?>(new ClipboardContent
                    {
                        Type = ClipboardContentType.Text,
                        Text = _clipboardText
                    });
                }
                else if (_clipboardImage != null && _clipboardImage.Length > 0)
                {
                    return Task.FromResult<ClipboardContent?>(new ClipboardContent
                    {
                        Type = ClipboardContentType.Image,
                        ImageData = _clipboardImage,
                        ImageFormat = "PNG"
                    });
                }
                else if (_clipboardFiles != null && _clipboardFiles.Count > 0)
                {
                    var files = _clipboardFiles.Select(path => new ClipboardFile
                    {
                        Path = path,
                        Name = System.IO.Path.GetFileName(path),
                        Size = 0,
                        IsAccessible = true
                    }).ToArray();
                    
                    return Task.FromResult<ClipboardContent?>(new ClipboardContent
                    {
                        Type = ClipboardContentType.FileList,
                        Files = files
                    });
                }
                
                return Task.FromResult<ClipboardContent?>(null);
            }
        }

        public Task SetContentAsync(ClipboardContent content)
        {
            switch (content.Type)
            {
                case ClipboardContentType.Text:
                    return SetTextAsync(content.Text ?? string.Empty);
                
                case ClipboardContentType.Image:
                    return SetImageAsync(content.ImageData ?? Array.Empty<byte>());
                
                case ClipboardContentType.FileList:
                    var paths = content.Files?.Select(f => f.Path).ToArray() ?? Array.Empty<string>();
                    return SetFileDropListAsync(paths);
                
                default:
                    return Task.CompletedTask;
            }
        }

        public void StartMonitoring()
        {
            // In a real implementation, this would monitor system clipboard changes
            // For now, this is a stub
        }

        public void StopMonitoring()
        {
            // In a real implementation, this would stop monitoring
            // For now, this is a stub
        }

        public bool IsMonitoring { get; private set; }
        
        public Task<bool> SetHtmlAsync(string html, string textFallback)
        {
            // For now, just set as text
            return SetTextAsync(textFallback);
        }
        
        public bool IsFormatSupported(ClipboardContentType type)
        {
            return type == ClipboardContentType.Text || 
                   type == ClipboardContentType.Image || 
                   type == ClipboardContentType.FileList;
        }
    }
}