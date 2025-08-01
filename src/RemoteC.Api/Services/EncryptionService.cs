using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RemoteC.Api.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EncryptionService> _logger;
        private readonly ConcurrentDictionary<string, byte[]> _keyStore;

        public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _keyStore = new ConcurrentDictionary<string, byte[]>();
        }

        public byte[] Encrypt(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);
            
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            
            return ms.ToArray();
        }

        public byte[] Decrypt(byte[] encryptedData, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            
            var iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var output = new MemoryStream();
            cs.CopyTo(output);
            
            return output.ToArray();
        }

        public string GenerateKey()
        {
            var key = new byte[32]; // 256-bit key
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return Convert.ToBase64String(key);
        }

        public string HashData(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        // Legacy async methods for compatibility
        public async Task<string> GenerateKeyAsync()
        {
            await Task.CompletedTask;
            
            var keyId = Guid.NewGuid().ToString();
            var key = new byte[32]; // 256-bit key
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            
            _keyStore[keyId] = key;
            _logger.LogInformation("Generated encryption key {KeyId}", keyId);
            
            return keyId;
        }

        public async Task<byte[]> EncryptAsync(byte[] data, string keyId)
        {
            if (!_keyStore.TryGetValue(keyId, out var key))
            {
                throw new InvalidOperationException($"Encryption key {keyId} not found");
            }

            await Task.CompletedTask;
            return Encrypt(data, key);
        }

        public async Task<byte[]> DecryptAsync(byte[] data, string keyId)
        {
            if (!_keyStore.TryGetValue(keyId, out var key))
            {
                throw new InvalidOperationException($"Encryption key {keyId} not found");
            }

            await Task.CompletedTask;
            return Decrypt(data, key);
        }
        
        public async Task RevokeKeyAsync(string keyId)
        {
            _logger.LogInformation("Revoking encryption key {KeyId}", keyId);
            
            if (_keyStore.TryRemove(keyId, out _))
            {
                _logger.LogInformation("Successfully revoked key {KeyId}", keyId);
            }
            else
            {
                _logger.LogWarning("Key {KeyId} not found for revocation", keyId);
            }
            
            await Task.CompletedTask;
        }
        
        public async Task RotateKeysAsync()
        {
            _logger.LogInformation("Rotating encryption keys");
            
            // In a real implementation, this would:
            // 1. Generate new keys
            // 2. Re-encrypt data with new keys
            // 3. Mark old keys for deletion after grace period
            
            var oldKeys = _keyStore.Keys.ToList();
            foreach (var keyId in oldKeys)
            {
                // Generate new key
                var newKeyId = await GenerateKeyAsync();
                _logger.LogInformation("Rotated key {OldKeyId} to {NewKeyId}", keyId, newKeyId);
            }
            
            await Task.CompletedTask;
        }

        public async Task<byte[]> EncryptAsync(byte[] data)
        {
            // Generate a temporary key for single-use encryption
            var keyId = await GenerateKeyAsync();
            return await EncryptAsync(data, keyId);
        }

        public async Task<byte[]> DecryptAsync(byte[] data)
        {
            // This would need to extract the key ID from the encrypted data
            // For now, throw not implemented
            throw new NotImplementedException("Decrypting without key ID is not yet implemented");
        }

        public string ComputeChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyChecksum(byte[] data, string checksum)
        {
            var computedChecksum = ComputeChecksum(data);
            return computedChecksum == checksum;
        }
    }
}