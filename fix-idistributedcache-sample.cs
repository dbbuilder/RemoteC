// Instead of mocking extension methods like:
_cacheMock.Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("value");

// Mock the underlying methods:
byte[] cacheValue = Encoding.UTF8.GetBytes("value");
_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(cacheValue);

// For SetStringAsync:
_cacheMock.Setup(c => c.SetAsync(
    It.IsAny<string>(), 
    It.IsAny<byte[]>(), 
    It.IsAny<DistributedCacheEntryOptions>(), 
    It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);

// For RemoveAsync - this is not an extension method, so it works:
_cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);
