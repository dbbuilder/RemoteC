using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Api.Services;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class CacheServiceTests
    {
        private readonly Mock<IDistributedCache> _distributedCacheMock;
        private readonly Mock<ILogger<CacheService>> _loggerMock;
        private readonly CacheService _service;

        public CacheServiceTests()
        {
            _distributedCacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<CacheService>>();
            _service = new CacheService(_distributedCacheMock.Object, _loggerMock.Object);
        }

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_WithExistingKey_ReturnsValue()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = new TestObject { Id = 1, Name = "Test" };
            var serializedValue = JsonSerializer.Serialize(expectedValue);
            var bytes = Encoding.UTF8.GetBytes(serializedValue);

            _distributedCacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            // Act
            var result = await _service.GetAsync<TestObject>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedValue.Id, result.Id);
            Assert.Equal(expectedValue.Name, result.Name);
        }

        [Fact]
        public async Task GetAsync_WithNonExistingKey_ReturnsNull()
        {
            // Arrange
            var key = "non-existing-key";
            _distributedCacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            // Act
            var result = await _service.GetAsync<TestObject>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WithInvalidJson_ReturnsNull()
        {
            // Arrange
            var key = "test-key";
            var invalidJson = "{ invalid json }";
            var bytes = Encoding.UTF8.GetBytes(invalidJson);

            _distributedCacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            // Act
            var result = await _service.GetAsync<TestObject>(key);

            // Assert
            Assert.Null(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error deserializing cached value")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region SetAsync Tests

        [Fact]
        public async Task SetAsync_WithValidValue_StoresInCache()
        {
            // Arrange
            var key = "test-key";
            var value = new TestObject { Id = 1, Name = "Test" };
            var expiration = TimeSpan.FromMinutes(30);
            byte[]? capturedBytes = null;
            DistributedCacheEntryOptions? capturedOptions = null;

            _distributedCacheMock.Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (k, b, o, c) => { capturedBytes = b; capturedOptions = o; })
                .Returns(Task.CompletedTask);

            // Act
            await _service.SetAsync(key, value, expiration);

            // Assert
            Assert.NotNull(capturedBytes);
            var storedValue = JsonSerializer.Deserialize<TestObject>(Encoding.UTF8.GetString(capturedBytes));
            Assert.Equal(value.Id, storedValue!.Id);
            Assert.Equal(value.Name, storedValue.Name);

            Assert.NotNull(capturedOptions);
            Assert.Equal(expiration, capturedOptions.AbsoluteExpirationRelativeToNow);
        }

        [Fact]
        public async Task SetAsync_WithNullValue_RemovesFromCache()
        {
            // Arrange
            var key = "test-key";
            _distributedCacheMock.Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.SetAsync<TestObject>(key, null);

            // Assert
            _distributedCacheMock.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
            _distributedCacheMock.Verify(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region RemoveAsync Tests

        [Fact]
        public async Task RemoveAsync_CallsDistributedCacheRemove()
        {
            // Arrange
            var key = "test-key";
            _distributedCacheMock.Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.RemoveAsync(key);

            // Assert
            _distributedCacheMock.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetStringAsync Tests

        [Fact]
        public async Task GetStringAsync_WithExistingKey_ReturnsString()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = "test string value";

            _distributedCacheMock.Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedValue);

            // Act
            var result = await _service.GetStringAsync(key);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        #endregion

        #region SetStringAsync Tests

        [Fact]
        public async Task SetStringAsync_StoresStringInCache()
        {
            // Arrange
            var key = "test-key";
            var value = "test string value";
            var expiration = TimeSpan.FromMinutes(15);

            _distributedCacheMock.Setup(c => c.SetStringAsync(
                key,
                value,
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.SetStringAsync(key, value, expiration);

            // Assert
            _distributedCacheMock.Verify(c => c.SetStringAsync(
                key,
                value,
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expiration),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetOrCreateAsync Tests

        [Fact]
        public async Task GetOrCreateAsync_WithCachedValue_ReturnsCachedValue()
        {
            // Arrange
            var key = "test-key";
            var cachedValue = new TestObject { Id = 1, Name = "Cached" };
            var serializedValue = JsonSerializer.Serialize(cachedValue);
            var bytes = Encoding.UTF8.GetBytes(serializedValue);

            _distributedCacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            var factoryCalled = false;
            Func<Task<TestObject>> factory = () =>
            {
                factoryCalled = true;
                return Task.FromResult(new TestObject { Id = 2, Name = "New" });
            };

            // Act
            var result = await _service.GetOrCreateAsync(key, factory);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedValue.Id, result.Id);
            Assert.Equal(cachedValue.Name, result.Name);
            Assert.False(factoryCalled);
        }

        [Fact]
        public async Task GetOrCreateAsync_WithoutCachedValue_CallsFactoryAndCaches()
        {
            // Arrange
            var key = "test-key";
            var newValue = new TestObject { Id = 2, Name = "New" };

            _distributedCacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            _distributedCacheMock.Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factoryCalled = false;
            Func<Task<TestObject>> factory = () =>
            {
                factoryCalled = true;
                return Task.FromResult(newValue);
            };

            // Act
            var result = await _service.GetOrCreateAsync(key, factory, TimeSpan.FromMinutes(10));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newValue.Id, result.Id);
            Assert.Equal(newValue.Name, result.Name);
            Assert.True(factoryCalled);

            _distributedCacheMock.Verify(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region InvalidatePatternAsync Tests

        [Fact]
        public async Task InvalidatePatternAsync_LogsWarning()
        {
            // Arrange
            var pattern = "test:*";

            // Act
            await _service.InvalidatePatternAsync(pattern);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Pattern-based cache invalidation not implemented")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}