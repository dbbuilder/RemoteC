using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RemoteC.Api.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly ILogger<EncryptionService> _logger;
        private readonly ConcurrentDictionary<string, byte[]> _keyStore;

        public EncryptionService(ILogger<EncryptionService> logger)
        {
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
    }
}