using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Api.Services;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class PinServiceTests
    {
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<PinService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly PinService _service;

        public PinServiceTests()
        {
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<PinService>>();
            _configurationMock = new Mock<IConfiguration>();

            // Setup default configuration
            var pinLengthSection = new Mock<IConfigurationSection>();
            pinLengthSection.Setup(x => x.Value).Returns("6");
            _configurationMock.Setup(x => x.GetSection("Security:PinLength"))
                .Returns(pinLengthSection.Object);
            
            var pinExpirationSection = new Mock<IConfigurationSection>();
            pinExpirationSection.Setup(x => x.Value).Returns("10");
            _configurationMock.Setup(x => x.GetSection("Security:PinExpirationMinutes"))
                .Returns(pinExpirationSection.Object);

            _service = new PinService(_cacheMock.Object, _loggerMock.Object, _configurationMock.Object);
        }

        #region GeneratePinAsync Tests

        [Fact]
        public async Task GeneratePinAsync_GeneratesCorrectLengthPin()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var pin = await _service.GeneratePinAsync(sessionId);

            // Assert
            Assert.NotNull(pin);
            Assert.Equal(6, pin.Length);
            Assert.True(int.TryParse(pin, out _), "PIN should contain only digits");
            
            _cacheMock.Verify(c => c.SetAsync(
                $"pin:session:{sessionId}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GeneratePinAsync_UsesConfiguredPinLength()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            
            // Override the default configuration
            var pinLengthSection = new Mock<IConfigurationSection>();
            pinLengthSection.Setup(x => x.Value).Returns("8");
            _configurationMock.Setup(x => x.GetSection("Security:PinLength"))
                .Returns(pinLengthSection.Object);

            var service = new PinService(_cacheMock.Object, _loggerMock.Object, _configurationMock.Object);

            // Act
            var pin = await service.GeneratePinAsync(sessionId);

            // Assert
            Assert.Equal(8, pin.Length);
        }

        [Fact]
        public async Task GeneratePinAsync_StoresHashedPinInCache()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            string? storedData = null;
            
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (key, value, options, token) => storedData = Encoding.UTF8.GetString(value))
                .Returns(Task.CompletedTask);

            // Act
            var pin = await _service.GeneratePinAsync(sessionId);

            // Assert
            Assert.NotNull(storedData);
            var pinData = JsonSerializer.Deserialize<JsonElement>(storedData);
            Assert.True(pinData.TryGetProperty("HashedPin", out var hashedPin));
            Assert.NotEqual(pin, hashedPin.GetString()); // PIN should be hashed
            Assert.True(pinData.TryGetProperty("CreatedAt", out _));
            Assert.True(pinData.TryGetProperty("ExpiresAt", out _));
            Assert.True(pinData.TryGetProperty("IsUsed", out var isUsed));
            Assert.False(isUsed.GetBoolean());
        }

        #endregion

        #region ValidatePinAsync Tests

        [Fact]
        public async Task ValidatePinAsync_WithCorrectPin_ReturnsTrue()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var pin = "123456";
            
            // First generate the PIN to get the hash
            string? storedData = null;
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (key, value, options, token) => storedData = Encoding.UTF8.GetString(value))
                .Returns(Task.CompletedTask);

            // Generate a PIN (we'll use 123456 for validation)
            _cacheMock.Setup(c => c.GetAsync(
                $"pin:session:{sessionId}",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => storedData != null ? Encoding.UTF8.GetBytes(storedData) : null);

            // Create PIN data with known hash for "123456"
            var pinData = JsonSerializer.Serialize(new
            {
                HashedPin = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=", // SHA256 hash of "123456"
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            });

            _cacheMock.Setup(c => c.GetAsync(
                $"pin:session:{sessionId}",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(pinData));

            _cacheMock.Setup(c => c.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ValidatePinAsync(sessionId, pin);

            // Assert
            Assert.True(result);
            _cacheMock.Verify(c => c.RemoveAsync(
                $"pin:session:{sessionId}",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ValidatePinAsync_WithIncorrectPin_ReturnsFalse()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var pinData = JsonSerializer.Serialize(new
            {
                HashedPin = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=", // Hash of "123456"
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            });

            _cacheMock.Setup(c => c.GetAsync(
                $"pin:session:{sessionId}",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(pinData));

            // Act
            var result = await _service.ValidatePinAsync(sessionId, "999999"); // Wrong PIN

            // Assert
            Assert.False(result);
            _cacheMock.Verify(c => c.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ValidatePinAsync_WithUsedPin_ReturnsFalse()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var pinData = JsonSerializer.Serialize(new
            {
                HashedPin = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = true // Already used
            });

            _cacheMock.Setup(c => c.GetAsync(
                $"pin:session:{sessionId}",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(pinData));

            // Act
            var result = await _service.ValidatePinAsync(sessionId, "123456");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidatePinAsync_WithNonExistentPin_ReturnsFalse()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _cacheMock.Setup(c => c.GetAsync(
                $"pin:session:{sessionId}",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            // Act
            var result = await _service.ValidatePinAsync(sessionId, "123456");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region InvalidatePinAsync Tests

        [Fact]
        public async Task InvalidatePinAsync_RemovesPinFromCache()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _cacheMock.Setup(c => c.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.InvalidatePinAsync(sessionId);

            // Assert
            _cacheMock.Verify(c => c.RemoveAsync(
                $"pin:session:{sessionId}",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region IsPinValidAsync Tests

        [Fact]
        public async Task IsPinValidAsync_WithValidUnusedPin_ReturnsTrue()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var pinData = JsonSerializer.Serialize(new
            {
                HashedPin = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            });

            _cacheMock.Setup(c => c.GetAsync(
                $"pin:session:{sessionId}",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(pinData));

            // Act
            var result = await _service.IsPinValidAsync(sessionId, "123456");

            // Assert
            Assert.True(result);
            // Should NOT remove the PIN (unlike ValidatePinAsync)
            _cacheMock.Verify(c => c.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}