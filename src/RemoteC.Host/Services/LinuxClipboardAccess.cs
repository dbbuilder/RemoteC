using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Host.Services
{
    /// <summary>
    /// Linux-specific clipboard access using xclip or xsel
    /// </summary>
    public class LinuxClipboardAccess : IClipboardAccess
    {
        private readonly object _lock = new object();
        private string? _lastText;
        private byte[]? _lastImage;

        public event EventHandler? ClipboardChanged;

        public bool ContainsText()
        {
            try
            {
                var text = GetTextFromXClip();
                return !string.IsNullOrEmpty(text);
            }
            catch
            {
                return false;
            }
        }

        public bool ContainsImage()
        {
            // Linux clipboard image support is complex, simplified for now
            return false;
        }

        public bool ContainsFileDropList()
        {
            // File drop support varies by desktop environment
            return false;
        }

        public async Task<string?> GetTextAsync()
        {
            return await Task.Run(() => GetTextFromXClip());
        }

        public Task<byte[]?> GetImageAsync()
        {
            // Not implemented for Linux in this simple version
            return Task.FromResult<byte[]?>(null);
        }

        public Task<string[]?> GetFileDropListAsync()
        {
            // Not implemented for Linux in this simple version
            return Task.FromResult<string[]?>(null);
        }

        public async Task<bool> SetTextAsync(string text)
        {
            return await Task.Run(() => SetTextToXClip(text));
        }

        public Task<bool> SetImageAsync(byte[] imageData)
        {
            // Not implemented for Linux in this simple version
            _lastImage = imageData;
            return Task.FromResult(false);
        }

        public Task<bool> SetFileDropListAsync(string[] files)
        {
            // Not implemented for Linux in this simple version
            return Task.FromResult(false);
        }

        public Task<bool> SetHtmlAsync(string html, string textFallback)
        {
            // For now, just set the text fallback
            return SetTextAsync(textFallback);
        }

        public async Task<bool> ClearAsync()
        {
            return await SetTextAsync(string.Empty);
        }

        public Task<ClipboardContent?> GetContentAsync()
        {
            var text = GetTextFromXClip();
            if (!string.IsNullOrEmpty(text))
            {
                return Task.FromResult<ClipboardContent?>(new ClipboardContent
                {
                    Type = ClipboardContentType.Text,
                    Text = text
                });
            }
            
            return Task.FromResult<ClipboardContent?>(null);
        }

        public Task SetContentAsync(ClipboardContent content)
        {
            switch (content.Type)
            {
                case ClipboardContentType.Text:
                    return SetTextAsync(content.Text ?? string.Empty);
                default:
                    return Task.FromResult(false);
            }
        }

        public void StartMonitoring()
        {
            // Would need to implement clipboard monitoring via X11 events
        }

        public void StopMonitoring()
        {
            // Stop monitoring
        }

        public bool IsMonitoring { get; private set; }

        public bool IsFormatSupported(ClipboardContentType type)
        {
            return type == ClipboardContentType.Text;
        }

        private string? GetTextFromXClip()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "xclip",
                        Arguments = "-selection clipboard -o",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(1000);

                if (process.ExitCode == 0)
                {
                    return output;
                }

                // Try xsel as fallback
                return GetTextFromXSel();
            }
            catch
            {
                return GetTextFromXSel();
            }
        }

        private string? GetTextFromXSel()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "xsel",
                        Arguments = "-b -o",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(1000);

                return process.ExitCode == 0 ? output : null;
            }
            catch
            {
                return null;
            }
        }

        private bool SetTextToXClip(string text)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "xclip",
                        Arguments = "-selection clipboard",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.StandardInput.Write(text);
                process.StandardInput.Close();
                process.WaitForExit(1000);

                if (process.ExitCode == 0)
                {
                    _lastText = text;
                    ClipboardChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                // Try xsel as fallback
                return SetTextToXSel(text);
            }
            catch
            {
                return SetTextToXSel(text);
            }
        }

        private bool SetTextToXSel(string text)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "xsel",
                        Arguments = "-b -i",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.StandardInput.Write(text);
                process.StandardInput.Close();
                process.WaitForExit(1000);

                if (process.ExitCode == 0)
                {
                    _lastText = text;
                    ClipboardChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}