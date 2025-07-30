// Fix pattern for CacheServiceTests
// Replace GetStringAsync/SetStringAsync with GetAsync/SetAsync

// Original:
_cacheMock.Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("value");

// Fixed:
_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(Encoding.UTF8.GetBytes("value"));

// For SetStringAsync:
_cacheMock.Setup(c => c.SetAsync(
    It.IsAny<string>(), 
    It.IsAny<byte[]>(), 
    It.IsAny<DistributedCacheEntryOptions>(), 
    It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);
