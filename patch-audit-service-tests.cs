// Pattern for fixing AuditServiceTests cache mocks

// For GetAsync with typed result:
byte[]? cachedData = null;
if (expectedLogs != null)
{
    cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expectedLogs));
}
_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(cachedData);
