using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public class RegistryService : IRegistryService
    {
        private readonly ILogger<RegistryService> _logger;
        private readonly Dictionary<string, List<string>> _imageTags = new();

        public RegistryService(ILogger<RegistryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Seed with some test data
            _imageTags["remotec/edge/remotec-agent"] = new List<string> { "latest", "1.0.0", "1.1.0", "1.2.0", "1.2.3", "1.2.4" };
            _imageTags["remotec/edge/test-app"] = new List<string> { "latest", "1.0.0", "1.9.0", "2.0.0" };
            _imageTags["remotec/edge/global-app"] = new List<string> { "latest", "1.0.0" };
        }

        public Task<bool> ImageExistsAsync(string imageName, string tag)
        {
            _logger.LogInformation("Checking if image {ImageName}:{Tag} exists", imageName, tag);
            
            if (_imageTags.TryGetValue(imageName, out var tags))
            {
                return Task.FromResult(tags.Contains(tag));
            }
            
            return Task.FromResult(false);
        }

        public Task<ImageInfo> GetImageInfoAsync(string imageName, string tag)
        {
            // TODO: Implement actual registry API call
            return Task.FromResult(new ImageInfo
            {
                Name = imageName,
                Tag = tag,
                Size = Random.Shared.Next(50_000_000, 500_000_000), // 50MB to 500MB
                Created = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                Digest = $"sha256:{Guid.NewGuid():N}",
                Labels = new Dictionary<string, string>
                {
                    ["maintainer"] = "RemoteC Team",
                    ["version"] = tag
                }
            });
        }

        public Task<List<string>> GetImageTagsAsync(string imageName)
        {
            if (_imageTags.TryGetValue(imageName, out var tags))
            {
                return Task.FromResult(tags.ToList());
            }
            
            return Task.FromResult(new List<string>());
        }

        public Task<bool> PushImageAsync(string imageName, string tag, byte[] imageData)
        {
            // TODO: Implement actual image push
            _logger.LogInformation("Pushing image {ImageName}:{Tag} ({Size} bytes)", 
                imageName, tag, imageData.Length);
            
            if (!_imageTags.ContainsKey(imageName))
            {
                _imageTags[imageName] = new List<string>();
            }
            
            if (!_imageTags[imageName].Contains(tag))
            {
                _imageTags[imageName].Add(tag);
            }
            
            return Task.FromResult(true);
        }

        public Task<bool> PullImageAsync(string imageName, string tag)
        {
            // TODO: Implement actual image pull
            _logger.LogInformation("Pulling image {ImageName}:{Tag}", imageName, tag);
            return Task.FromResult(ImageExistsAsync(imageName, tag).Result);
        }

        public Task<bool> DeleteImageAsync(string imageName, string tag)
        {
            // TODO: Implement actual image deletion
            _logger.LogInformation("Deleting image {ImageName}:{Tag}", imageName, tag);
            
            if (_imageTags.TryGetValue(imageName, out var tags))
            {
                return Task.FromResult(tags.Remove(tag));
            }
            
            return Task.FromResult(false);
        }

        public Task<List<VulnerabilityScan>> ScanImageAsync(string imageName, string tag)
        {
            // TODO: Implement actual vulnerability scanning
            _logger.LogInformation("Scanning image {ImageName}:{Tag} for vulnerabilities", imageName, tag);
            
            // Return mock vulnerabilities for demonstration
            var vulnerabilities = new List<VulnerabilityScan>();
            
            if (Random.Shared.Next(100) < 30) // 30% chance of vulnerabilities
            {
                vulnerabilities.Add(new VulnerabilityScan
                {
                    Severity = "Medium",
                    Package = "openssl",
                    Version = "1.1.1",
                    FixedVersion = "1.1.1g",
                    Description = "OpenSSL vulnerability CVE-2020-1234"
                });
            }
            
            if (Random.Shared.Next(100) < 10) // 10% chance of critical vulnerability
            {
                vulnerabilities.Add(new VulnerabilityScan
                {
                    Severity = "Critical",
                    Package = "log4j",
                    Version = "2.14.0",
                    FixedVersion = "2.17.0",
                    Description = "Log4Shell vulnerability CVE-2021-44228"
                });
            }
            
            return Task.FromResult(vulnerabilities);
        }
    }
}