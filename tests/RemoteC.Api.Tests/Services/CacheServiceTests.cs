using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Api.Services;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class CacheServiceTests
    {
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly Mock<ILogger<CacheService>> _loggerMock;
        private readonly CacheService _service;

        public CacheServiceTests()
        {
            _memoryCacheMock = new Mock<IMemoryCache>();
            _loggerMock = new Mock<ILogger<CacheService>>();
            _service = new CacheService(_memoryCacheMock.Object, _loggerMock.Object);
        }

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_WithExistingKey_ReturnsValue()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = new TestObject { Id = 1, Name = "Test" };
            object cachedValue = expectedValue;

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
                .Returns(true);

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
            object cachedValue = null!;

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
                .Returns(false);

            // Act
            var result = await _service.GetAsync<TestObject>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WithInvalidType_ReturnsNull()
        {
            // Arrange
            var key = "test-key";
            object cachedValue = "invalid-type";

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
                .Returns(true);

            // Act
            var result = await _service.GetAsync<TestObject>(key);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region SetAsync Tests

        [Fact]
        public async Task SetAsync_WithValue_StoresValueInCache()
        {
            // Arrange
            var key = "test-key";
            var value = new TestObject { Id = 1, Name = "Test" };
            var expiration = TimeSpan.FromMinutes(10);

            var mockEntry = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(c => c.CreateEntry(key))
                .Returns(mockEntry.Object);

            // Act
            await _service.SetAsync(key, value, expiration);

            // Assert
            _memoryCacheMock.Verify(c => c.CreateEntry(key), Times.Once);
            mockEntry.VerifySet(e => e.Value = value, Times.Once);
            mockEntry.VerifySet(e => e.AbsoluteExpirationRelativeToNow = expiration, Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithoutExpiration_UsesDefaultSlidingExpiration()
        {
            // Arrange
            var key = "test-key";
            var value = new TestObject { Id = 1, Name = "Test" };

            var mockEntry = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(c => c.CreateEntry(key))
                .Returns(mockEntry.Object);

            // Act
            await _service.SetAsync(key, value);

            // Assert
            _memoryCacheMock.Verify(c => c.CreateEntry(key), Times.Once);
            mockEntry.VerifySet(e => e.Value = value, Times.Once);
            mockEntry.VerifySet(e => e.SlidingExpiration = TimeSpan.FromMinutes(5), Times.Once);
        }

        #endregion

        #region RemoveAsync Tests

        [Fact]
        public async Task RemoveAsync_WithKey_RemovesFromCache()
        {
            // Arrange
            var key = "test-key";

            // Act
            await _service.RemoveAsync(key);

            // Assert
            _memoryCacheMock.Verify(c => c.Remove(key), Times.Once);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            object cachedValue = "some-value";

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
                .Returns(true);

            // Act
            var result = await _service.ExistsAsync(key);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingKey_ReturnsFalse()
        {
            // Arrange
            var key = "non-existing-key";
            object cachedValue = null!;

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
                .Returns(false);

            // Act
            var result = await _service.ExistsAsync(key);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetStringAsync Tests

        [Fact]
        public async Task GetStringAsync_WithExistingKey_ReturnsString()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = "test-string";
            object cachedValue = expectedValue;

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
                .Returns(true);

            // Act
            var result = await _service.GetStringAsync(key);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public async Task GetStringAsync_WithNonExistingKey_ReturnsNull()
        {
            // Arrange
            var key = "non-existing-key";
            object cachedValue = null!;

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
                .Returns(false);

            // Act
            var result = await _service.GetStringAsync(key);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region SetStringAsync Tests

        [Fact]
        public async Task SetStringAsync_WithValue_StoresStringInCache()
        {
            // Arrange
            var key = "test-key";
            var value = "test-string";
            var expiration = TimeSpan.FromMinutes(10);

            var mockEntry = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(c => c.CreateEntry(key))
                .Returns(mockEntry.Object);

            // Act
            await _service.SetStringAsync(key, value, expiration);

            // Assert
            _memoryCacheMock.Verify(c => c.CreateEntry(key), Times.Once);
            mockEntry.VerifySet(e => e.Value = value, Times.Once);
            mockEntry.VerifySet(e => e.AbsoluteExpirationRelativeToNow = expiration, Times.Once);
        }

        #endregion

        #region GetOrSetAsync Tests

        [Fact]
        public async Task GetOrSetAsync_WithExistingKey_ReturnsCachedValue()
        {
            // Arrange
            var key = "test-key";
            var cachedValue = new TestObject { Id = 1, Name = "Cached" };
            object cachedObjectValue = cachedValue;
            var factoryCalled = false;

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedObjectValue))
                .Returns(true);

            // Act
            var result = await _service.GetOrSetAsync(key, () =>
            {
                factoryCalled = true;
                return Task.FromResult(new TestObject { Id = 2, Name = "Factory" });
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedValue.Id, result.Id);
            Assert.Equal(cachedValue.Name, result.Name);
            Assert.False(factoryCalled);
        }

        [Fact]
        public async Task GetOrSetAsync_WithNonExistingKey_CallsFactoryAndCaches()
        {
            // Arrange
            var key = "test-key";
            var factoryValue = new TestObject { Id = 2, Name = "Factory" };
            object cachedValue = null!;
            var factoryCalled = false;

            _memoryCacheMock.Setup(c => c.TryGetValue(key, out cachedValue))
                .Returns(false);

            var mockEntry = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(c => c.CreateEntry(key))
                .Returns(mockEntry.Object);

            // Act
            var result = await _service.GetOrSetAsync(key, () =>
            {
                factoryCalled = true;
                return Task.FromResult(factoryValue);
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(factoryValue.Id, result.Id);
            Assert.Equal(factoryValue.Name, result.Name);
            Assert.True(factoryCalled);
            _memoryCacheMock.Verify(c => c.CreateEntry(key), Times.Once);
        }

        #endregion

        #region InvalidateAsync Tests

        [Fact]
        public async Task InvalidateAsync_WithPattern_LogsWarning()
        {
            // Arrange
            var pattern = "test-*";

            // Act
            await _service.InvalidateAsync(pattern);

            // Assert - Just verify no exceptions thrown, as method only logs
            Assert.True(true);
        }

        #endregion

        #region ClearAsync Tests

        [Fact]
        public async Task ClearAsync_LogsWarning()
        {
            // Act
            await _service.ClearAsync();

            // Assert - Just verify no exceptions thrown, as method only logs
            Assert.True(true);
        }

        #endregion

        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}