using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Host.Services
{
    /// <summary>
    /// macOS-specific clipboard access using pbcopy and pbpaste
    /// </summary>
    public class MacOSClipboardAccess : IClipboardAccess
    {
        private readonly object _lock = new object();
        private string? _lastText;

        public event EventHandler? ClipboardChanged;

        public bool ContainsText()
        {
            try
            {
                var text = GetTextFromPbPaste();
                return !string.IsNullOrEmpty(text);
            }
            catch
            {
                return false;
            }
        }

        public bool ContainsImage()
        {
            // macOS clipboard image support would require native API
            return false;
        }

        public bool ContainsFileDropList()
        {
            // File drop support would require native API
            return false;
        }

        public async Task<string?> GetTextAsync()
        {
            return await Task.Run(() => GetTextFromPbPaste());
        }

        public Task<byte[]?> GetImageAsync()
        {
            // Not implemented in this simple version
            return Task.FromResult<byte[]?>(null);
        }

        public Task<string[]?> GetFileDropListAsync()
        {
            // Not implemented in this simple version
            return Task.FromResult<string[]?>(null);
        }

        public async Task<bool> SetTextAsync(string text)
        {
            return await Task.Run(() => SetTextToPbCopy(text));
        }

        public Task<bool> SetImageAsync(byte[] imageData)
        {
            // Not implemented in this simple version
            return Task.FromResult(false);
        }

        public Task<bool> SetFileDropListAsync(string[] files)
        {
            // Not implemented in this simple version
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
            var text = GetTextFromPbPaste();
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
            // Would need to implement clipboard monitoring via native API
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

        private string? GetTextFromPbPaste()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "pbpaste",
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

        private bool SetTextToPbCopy(string text)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "pbcopy",
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