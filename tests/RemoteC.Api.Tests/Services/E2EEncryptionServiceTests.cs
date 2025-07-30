using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class E2EEncryptionServiceTests
    {
        private readonly Mock<ILogger<E2EEncryptionService>> _loggerMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly E2EEncryptionService _service;
        private readonly E2EEncryptionOptions _options;

        public E2EEncryptionServiceTests()
        {
            _loggerMock = new Mock<ILogger<E2EEncryptionService>>();
            _auditMock = new Mock<IAuditService>();
            
            _options = new E2EEncryptionOptions
            {
                KeyDerivationIterations = 100000,
                EnablePerfectForwardSecrecy = true,
                KeyRotationIntervalDays = 30,
                MaxKeyAgeHours = 24
            };

            _service = new E2EEncryptionService(
                _loggerMock.Object,
                _auditMock.Object,
                Options.Create(_options));
        }

        #region Key Exchange Tests

        [Fact]
        public async Task GenerateKeyPairAsync_CreatesValidX25519KeyPair()
        {
            // Act
            var keyPair = await _service.GenerateKeyPairAsync();

            // Assert
            Assert.NotNull(keyPair);
            Assert.NotNull(keyPair.PublicKey);
            Assert.NotNull(keyPair.PrivateKey);
            Assert.Equal(32, keyPair.PublicKey.Length); // X25519 keys are 32 bytes
            Assert.Equal(32, keyPair.PrivateKey.Length);
            Assert.NotEqual(keyPair.PublicKey, keyPair.PrivateKey);
            Assert.True(keyPair.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task PerformKeyExchangeAsync_GeneratesSharedSecret()
        {
            // Arrange
            var aliceKeyPair = await _service.GenerateKeyPairAsync();
            var bobKeyPair = await _service.GenerateKeyPairAsync();

            // Act
            var aliceSharedSecret = await _service.PerformKeyExchangeAsync(
                aliceKeyPair.PrivateKey, 
                bobKeyPair.PublicKey);
            
            var bobSharedSecret = await _service.PerformKeyExchangeAsync(
                bobKeyPair.PrivateKey, 
                aliceKeyPair.PublicKey);

            // Assert
            Assert.NotNull(aliceSharedSecret);
            Assert.NotNull(bobSharedSecret);
            Assert.Equal(32, aliceSharedSecret.Length);
            Assert.Equal(aliceSharedSecret, bobSharedSecret); // Both parties derive same secret
        }

        [Fact]
        public async Task EstablishSessionKeysAsync_CreatesValidSessionKeys()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var aliceKeyPair = await _service.GenerateKeyPairAsync();
            var bobKeyPair = await _service.GenerateKeyPairAsync();

            // Act
            var sessionKeys = await _service.EstablishSessionKeysAsync(
                sessionId,
                aliceKeyPair,
                bobKeyPair.PublicKey);

            // Assert
            Assert.NotNull(sessionKeys);
            Assert.Equal(sessionId, sessionKeys.SessionId);
            Assert.NotNull(sessionKeys.EncryptionKey);
            Assert.NotNull(sessionKeys.AuthenticationKey);
            Assert.Equal(32, sessionKeys.EncryptionKey.Length); // ChaCha20 key
            Assert.Equal(32, sessionKeys.AuthenticationKey.Length); // Poly1305 key
            Assert.NotEqual(sessionKeys.EncryptionKey, sessionKeys.AuthenticationKey);
        }

        #endregion

        #region Encryption/Decryption Tests

        [Fact]
        public async Task EncryptAsync_ProducesValidCiphertext()
        {
            // Arrange
            var sessionKeys = await CreateTestSessionKeys();
            var plaintext = Encoding.UTF8.GetBytes("Hello, E2EE World!");

            // Act
            var encrypted = await _service.EncryptAsync(plaintext, sessionKeys);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotNull(encrypted.Ciphertext);
            Assert.NotNull(encrypted.Nonce);
            Assert.NotNull(encrypted.Tag);
            Assert.Equal(12, encrypted.Nonce.Length); // ChaCha20-Poly1305 uses 12-byte nonce
            Assert.Equal(16, encrypted.Tag.Length); // Poly1305 produces 16-byte tag
            Assert.NotEqual(plaintext, encrypted.Ciphertext);
            Assert.True(encrypted.Timestamp > 0);
        }

        [Fact]
        public async Task DecryptAsync_RecoverOriginalPlaintext()
        {
            // Arrange
            var sessionKeys = await CreateTestSessionKeys();
            var plaintext = Encoding.UTF8.GetBytes("Sensitive data for E2EE");
            var encrypted = await _service.EncryptAsync(plaintext, sessionKeys);

            // Act
            var decrypted = await _service.DecryptAsync(encrypted, sessionKeys);

            // Assert
            Assert.NotNull(decrypted);
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public async Task DecryptAsync_FailsWithWrongKey()
        {
            // Arrange
            var sessionKeys1 = await CreateTestSessionKeys();
            var sessionKeys2 = await CreateTestSessionKeys(); // Different keys
            var plaintext = Encoding.UTF8.GetBytes("Secret message");
            var encrypted = await _service.EncryptAsync(plaintext, sessionKeys1);

            // Act & Assert
            await Assert.ThrowsAsync<CryptographicException>(
                () => _service.DecryptAsync(encrypted, sessionKeys2));
        }

        [Fact]
        public async Task DecryptAsync_FailsWithTamperedCiphertext()
        {
            // Arrange
            var sessionKeys = await CreateTestSessionKeys();
            var plaintext = Encoding.UTF8.GetBytes("Original message");
            var encrypted = await _service.EncryptAsync(plaintext, sessionKeys);
            
            // Tamper with ciphertext
            encrypted.Ciphertext[0] ^= 0xFF;

            // Act & Assert
            await Assert.ThrowsAsync<CryptographicException>(
                () => _service.DecryptAsync(encrypted, sessionKeys));
        }

        [Fact]
        public async Task DecryptAsync_FailsWithTamperedTag()
        {
            // Arrange
            var sessionKeys = await CreateTestSessionKeys();
            var plaintext = Encoding.UTF8.GetBytes("Authenticated message");
            var encrypted = await _service.EncryptAsync(plaintext, sessionKeys);
            
            // Tamper with authentication tag
            encrypted.Tag[0] ^= 0xFF;

            // Act & Assert
            await Assert.ThrowsAsync<CryptographicException>(
                () => _service.DecryptAsync(encrypted, sessionKeys));
        }

        [Fact]
        public async Task EncryptAsync_UniqueNoncePerMessage()
        {
            // Arrange
            var sessionKeys = await CreateTestSessionKeys();
            var plaintext = Encoding.UTF8.GetBytes("Same message");

            // Act - Encrypt same message multiple times
            var encrypted1 = await _service.EncryptAsync(plaintext, sessionKeys);
            var encrypted2 = await _service.EncryptAsync(plaintext, sessionKeys);
            var encrypted3 = await _service.EncryptAsync(plaintext, sessionKeys);

            // Assert - Each encryption uses unique nonce
            Assert.NotEqual(encrypted1.Nonce, encrypted2.Nonce);
            Assert.NotEqual(encrypted2.Nonce, encrypted3.Nonce);
            Assert.NotEqual(encrypted1.Nonce, encrypted3.Nonce);
            
            // Ciphertexts should also be different due to unique nonces
            Assert.NotEqual(encrypted1.Ciphertext, encrypted2.Ciphertext);
        }

        #endregion

        #region Stream Encryption Tests

        [Fact]
        public async Task EncryptStreamAsync_HandlesLargeData()
        {
            // Arrange
            var sessionKeys = await CreateTestSessionKeys();
            var largeData = new byte[10 * 1024 * 1024]; // 10MB
            new Random().NextBytes(largeData);
            
            using var inputStream = new System.IO.MemoryStream(largeData);
            using var outputStream = new System.IO.MemoryStream();

            // Act
            var metadata = await _service.EncryptStreamAsync(
                inputStream, 
                outputStream, 
                sessionKeys);

            // Assert
            Assert.NotNull(metadata);
            Assert.True(outputStream.Length > 0);
            Assert.NotEqual(largeData.Length, outputStream.Length); // Size changes due to encryption
            Assert.True(metadata.ChunkCount > 0);
            Assert.Equal(largeData.Length, metadata.OriginalSize);
        }

        [Fact]
        public async Task DecryptStreamAsync_RecoverLargeData()
        {
            // Arrange
            var sessionKeys = await CreateTestSessionKeys();
            var originalData = new byte[5 * 1024 * 1024]; // 5MB
            new Random().NextBytes(originalData);
            
            using var inputStream = new System.IO.MemoryStream(originalData);
            using var encryptedStream = new System.IO.MemoryStream();
            
            var metadata = await _service.EncryptStreamAsync(
                inputStream, 
                encryptedStream, 
                sessionKeys);
            
            encryptedStream.Position = 0;
            using var decryptedStream = new System.IO.MemoryStream();

            // Act
            await _service.DecryptStreamAsync(
                encryptedStream, 
                decryptedStream, 
                sessionKeys,
                metadata);

            // Assert
            var decryptedData = decryptedStream.ToArray();
            Assert.Equal(originalData, decryptedData);
        }

        #endregion

        #region Digital Signature Tests

        [Fact]
        public async Task GenerateSigningKeyPairAsync_CreatesValidEd25519KeyPair()
        {
            // Act
            var signingKeys = await _service.GenerateSigningKeyPairAsync();

            // Assert
            Assert.NotNull(signingKeys);
            Assert.NotNull(signingKeys.PublicKey);
            Assert.NotNull(signingKeys.PrivateKey);
            Assert.Equal(32, signingKeys.PublicKey.Length); // Ed25519 public key
            Assert.Equal(32, signingKeys.PrivateKey.Length); // Ed25519 private key (seed)
        }

        [Fact]
        public async Task SignDataAsync_ProducesValidSignature()
        {
            // Arrange
            var signingKeys = await _service.GenerateSigningKeyPairAsync();
            var data = Encoding.UTF8.GetBytes("Document to sign");

            // Act
            var signature = await _service.SignDataAsync(data, signingKeys.PrivateKey);

            // Assert
            Assert.NotNull(signature);
            Assert.Equal(64, signature.Length); // Ed25519 signature is 64 bytes
        }

        [Fact]
        public async Task VerifySignatureAsync_ValidatesCorrectSignature()
        {
            // Arrange
            var signingKeys = await _service.GenerateSigningKeyPairAsync();
            var data = Encoding.UTF8.GetBytes("Authentic document");
            var signature = await _service.SignDataAsync(data, signingKeys.PrivateKey);

            // Act
            var isValid = await _service.VerifySignatureAsync(
                data, 
                signature, 
                signingKeys.PublicKey);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task VerifySignatureAsync_RejectsTamperedData()
        {
            // Arrange
            var signingKeys = await _service.GenerateSigningKeyPairAsync();
            var data = Encoding.UTF8.GetBytes("Original document");
            var signature = await _service.SignDataAsync(data, signingKeys.PrivateKey);
            
            // Tamper with data
            data[0] ^= 0xFF;

            // Act
            var isValid = await _service.VerifySignatureAsync(
                data, 
                signature, 
                signingKeys.PublicKey);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task VerifySignatureAsync_RejectsWrongPublicKey()
        {
            // Arrange
            var signingKeys1 = await _service.GenerateSigningKeyPairAsync();
            var signingKeys2 = await _service.GenerateSigningKeyPairAsync();
            var data = Encoding.UTF8.GetBytes("Signed document");
            var signature = await _service.SignDataAsync(data, signingKeys1.PrivateKey);

            // Act - Verify with wrong public key
            var isValid = await _service.VerifySignatureAsync(
                data, 
                signature, 
                signingKeys2.PublicKey);

            // Assert
            Assert.False(isValid);
        }

        #endregion

        #region Perfect Forward Secrecy Tests

        [Fact]
        public async Task RotateSessionKeysAsync_GeneratesNewKeys()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var initialKeys = await CreateTestSessionKeys(sessionId);

            // Act
            var rotatedKeys = await _service.RotateSessionKeysAsync(sessionId, initialKeys);

            // Assert
            Assert.NotNull(rotatedKeys);
            Assert.Equal(sessionId, rotatedKeys.SessionId);
            Assert.NotEqual(initialKeys.EncryptionKey, rotatedKeys.EncryptionKey);
            Assert.NotEqual(initialKeys.AuthenticationKey, rotatedKeys.AuthenticationKey);
            Assert.True(rotatedKeys.Version > initialKeys.Version);
        }

        [Fact]
        public async Task IsKeyRotationRequiredAsync_DetectsExpiredKeys()
        {
            // Arrange
            var oldKeys = await CreateTestSessionKeys();
            oldKeys.CreatedAt = DateTime.UtcNow.AddHours(-25); // Older than max age
            
            var newKeys = await CreateTestSessionKeys();

            // Act
            var oldKeysNeedRotation = await _service.IsKeyRotationRequiredAsync(oldKeys);
            var newKeysNeedRotation = await _service.IsKeyRotationRequiredAsync(newKeys);

            // Assert
            Assert.True(oldKeysNeedRotation);
            Assert.False(newKeysNeedRotation);
        }

        #endregion

        #region Certificate Tests

        [Fact]
        public async Task GenerateCertificateAsync_CreatesValidCertificate()
        {
            // Arrange
            var signingKeys = await _service.GenerateSigningKeyPairAsync();
            var deviceId = Guid.NewGuid();
            var deviceName = "Test Device";

            // Act
            var certificate = await _service.GenerateCertificateAsync(
                deviceId,
                deviceName,
                signingKeys);

            // Assert
            Assert.NotNull(certificate);
            Assert.Equal(deviceId, certificate.DeviceId);
            Assert.Equal(deviceName, certificate.DeviceName);
            Assert.Equal(signingKeys.PublicKey, certificate.PublicKey);
            Assert.NotNull(certificate.Signature);
            Assert.True(certificate.ValidFrom <= DateTime.UtcNow);
            Assert.True(certificate.ValidTo > DateTime.UtcNow);
        }

        [Fact]
        public async Task ValidateCertificateAsync_AcceptsValidCertificate()
        {
            // Arrange
            var signingKeys = await _service.GenerateSigningKeyPairAsync();
            var certificate = await _service.GenerateCertificateAsync(
                Guid.NewGuid(),
                "Valid Device",
                signingKeys);

            // Act
            var isValid = await _service.ValidateCertificateAsync(certificate);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateCertificateAsync_RejectsExpiredCertificate()
        {
            // Arrange
            var signingKeys = await _service.GenerateSigningKeyPairAsync();
            var certificate = await _service.GenerateCertificateAsync(
                Guid.NewGuid(),
                "Expired Device",
                signingKeys);
            
            // Make certificate expired
            certificate.ValidTo = DateTime.UtcNow.AddDays(-1);

            // Act
            var isValid = await _service.ValidateCertificateAsync(certificate);

            // Assert
            Assert.False(isValid);
        }

        #endregion

        #region Helper Methods

        private async Task<SessionKeys> CreateTestSessionKeys(Guid? sessionId = null)
        {
            var keyPair1 = await _service.GenerateKeyPairAsync();
            var keyPair2 = await _service.GenerateKeyPairAsync();
            
            return await _service.EstablishSessionKeysAsync(
                sessionId ?? Guid.NewGuid(),
                keyPair1,
                keyPair2.PublicKey);
        }

        #endregion
    }


}