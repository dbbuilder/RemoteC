using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Api.Services;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class EncryptionServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<EncryptionService>> _loggerMock;
        private readonly EncryptionService _service;

        public EncryptionServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<EncryptionService>>();

            // Setup default configuration
            _configurationMock.Setup(c => c["Encryption:MasterKey"])
                .Returns("dGVzdC1tYXN0ZXIta2V5LWZvci11bml0LXRlc3Rpbmc="); // Base64 encoded test key

            _service = new EncryptionService(_configurationMock.Object, _loggerMock.Object);
        }

        #region EncryptAsync Tests

        [Fact]
        public async Task EncryptAsync_WithValidData_ReturnsEncryptedData()
        {
            // Arrange
            var plainData = Encoding.UTF8.GetBytes("This is test data");
            var keyId = await _service.GenerateKeyAsync();

            // Act
            var encryptedData = await _service.EncryptAsync(plainData, keyId);

            // Assert
            Assert.NotNull(encryptedData);
            Assert.NotEqual(plainData, encryptedData);
            Assert.True(encryptedData.Length > plainData.Length); // Should include IV and auth tag
        }

        [Fact]
        public async Task EncryptAsync_WithEmptyData_ReturnsEmptyArray()
        {
            // Arrange
            var plainData = Array.Empty<byte>();
            var keyId = await _service.GenerateKeyAsync();

            // Act
            var encryptedData = await _service.EncryptAsync(plainData, keyId);

            // Assert
            Assert.NotNull(encryptedData);
            Assert.Empty(encryptedData);
        }

        [Fact]
        public async Task EncryptAsync_WithInvalidKeyId_ThrowsException()
        {
            // Arrange
            var plainData = Encoding.UTF8.GetBytes("Test data");
            var invalidKeyId = "invalid-key-id";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.EncryptAsync(plainData, invalidKeyId));
        }

        #endregion

        #region DecryptAsync Tests

        [Fact]
        public async Task DecryptAsync_WithValidEncryptedData_ReturnsOriginalData()
        {
            // Arrange
            var originalData = Encoding.UTF8.GetBytes("This is test data for encryption");
            var keyId = await _service.GenerateKeyAsync();
            var encryptedData = await _service.EncryptAsync(originalData, keyId);

            // Act
            var decryptedData = await _service.DecryptAsync(encryptedData, keyId);

            // Assert
            Assert.NotNull(decryptedData);
            Assert.Equal(originalData, decryptedData);
        }

        [Fact]
        public async Task DecryptAsync_WithTamperedData_ThrowsException()
        {
            // Arrange
            var originalData = Encoding.UTF8.GetBytes("Test data");
            var keyId = await _service.GenerateKeyAsync();
            var encryptedData = await _service.EncryptAsync(originalData, keyId);
            
            // Tamper with the data
            encryptedData[encryptedData.Length - 1] ^= 0xFF;

            // Act & Assert
            await Assert.ThrowsAsync<CryptographicException>(
                () => _service.DecryptAsync(encryptedData, keyId));
        }

        [Fact]
        public async Task DecryptAsync_WithWrongKey_ThrowsException()
        {
            // Arrange
            var originalData = Encoding.UTF8.GetBytes("Test data");
            var keyId1 = await _service.GenerateKeyAsync();
            var keyId2 = await _service.GenerateKeyAsync();
            var encryptedData = await _service.EncryptAsync(originalData, keyId1);

            // Act & Assert
            await Assert.ThrowsAsync<CryptographicException>(
                () => _service.DecryptAsync(encryptedData, keyId2));
        }

        #endregion

        #region GenerateKeyAsync Tests

        [Fact]
        public async Task GenerateKeyAsync_GeneratesUniqueKeys()
        {
            // Act
            var key1 = await _service.GenerateKeyAsync();
            var key2 = await _service.GenerateKeyAsync();
            var key3 = await _service.GenerateKeyAsync();

            // Assert
            Assert.NotNull(key1);
            Assert.NotNull(key2);
            Assert.NotNull(key3);
            Assert.NotEqual(key1, key2);
            Assert.NotEqual(key2, key3);
            Assert.NotEqual(key1, key3);
        }

        [Fact]
        public async Task GenerateKeyAsync_GeneratedKeysAreUsable()
        {
            // Arrange
            var testData = Encoding.UTF8.GetBytes("Test data for key validation");

            // Act
            var keyId = await _service.GenerateKeyAsync();
            var encrypted = await _service.EncryptAsync(testData, keyId);
            var decrypted = await _service.DecryptAsync(encrypted, keyId);

            // Assert
            Assert.Equal(testData, decrypted);
        }

        #endregion

        #region RevokeKeyAsync Tests

        [Fact]
        public async Task RevokeKeyAsync_AfterRevocation_EncryptionFails()
        {
            // Arrange
            var keyId = await _service.GenerateKeyAsync();
            var testData = Encoding.UTF8.GetBytes("Test data");

            // Ensure key works before revocation
            var encrypted = await _service.EncryptAsync(testData, keyId);
            Assert.NotNull(encrypted);

            // Act
            await _service.RevokeKeyAsync(keyId);

            // Assert - Encryption should fail after revocation
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.EncryptAsync(testData, keyId));
        }

        [Fact]
        public async Task RevokeKeyAsync_WithNonExistentKey_DoesNotThrow()
        {
            // Arrange
            var nonExistentKeyId = "non-existent-key";

            // Act & Assert - Should not throw
            await _service.RevokeKeyAsync(nonExistentKeyId);
        }

        #endregion

        #region RotateKeysAsync Tests

        [Fact]
        public async Task RotateKeysAsync_OldKeysStillWorkForDecryption()
        {
            // Arrange
            var testData = Encoding.UTF8.GetBytes("Test data for key rotation");
            var oldKeyId = await _service.GenerateKeyAsync();
            var encryptedWithOldKey = await _service.EncryptAsync(testData, oldKeyId);

            // Act
            await _service.RotateKeysAsync();
            
            // Generate new key after rotation
            var newKeyId = await _service.GenerateKeyAsync();

            // Assert - Old encrypted data should still be decryptable
            var decryptedOldData = await _service.DecryptAsync(encryptedWithOldKey, oldKeyId);
            Assert.Equal(testData, decryptedOldData);

            // New key should work
            var encryptedWithNewKey = await _service.EncryptAsync(testData, newKeyId);
            var decryptedNewData = await _service.DecryptAsync(encryptedWithNewKey, newKeyId);
            Assert.Equal(testData, decryptedNewData);
        }

        #endregion

        #region EncryptionDecryption Integration Tests

        [Theory]
        [InlineData("")]
        [InlineData("Short text")]
        [InlineData("This is a longer text that should be encrypted and decrypted correctly")]
        public async Task EncryptDecrypt_RoundTrip_WorksCorrectly(string testText)
        {
            // Arrange
            var testData = Encoding.UTF8.GetBytes(testText);
            var keyId = await _service.GenerateKeyAsync();

            // Act
            var encrypted = await _service.EncryptAsync(testData, keyId);
            var decrypted = await _service.DecryptAsync(encrypted, keyId);

            // Assert
            var decryptedText = Encoding.UTF8.GetString(decrypted);
            Assert.Equal(testText, decryptedText);
        }

        [Fact]
        public async Task EncryptDecrypt_LargeData_WorksCorrectly()
        {
            // Arrange
            var largeData = new byte[1024 * 1024]; // 1MB
            new Random().NextBytes(largeData);
            var keyId = await _service.GenerateKeyAsync();

            // Act
            var encrypted = await _service.EncryptAsync(largeData, keyId);
            var decrypted = await _service.DecryptAsync(encrypted, keyId);

            // Assert
            Assert.Equal(largeData, decrypted);
        }

        #endregion
    }
}