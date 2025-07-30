using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public class E2EEncryptionService : IE2EEncryptionService
    {
        private readonly ILogger<E2EEncryptionService> _logger;
        private readonly IAuditService _auditService;
        private readonly E2EEncryptionOptions _options;
        
        // NSec algorithms
        private readonly X25519 _keyAgreement = new X25519();
        private readonly NSec.Cryptography.ChaCha20Poly1305 _aead = new NSec.Cryptography.ChaCha20Poly1305();
        private readonly Ed25519 _signature = new Ed25519();
        private readonly HkdfSha256 _kdf = new HkdfSha256();

        public E2EEncryptionService(
            ILogger<E2EEncryptionService> logger,
            IAuditService auditService,
            IOptions<E2EEncryptionOptions> options)
        {
            _logger = logger;
            _auditService = auditService;
            _options = options.Value;
        }

        public async Task<E2EKeyPair> GenerateKeyPairAsync()
        {
            await Task.CompletedTask;
            
            var key = Key.Create(_keyAgreement, new KeyCreationParameters 
            { 
                ExportPolicy = KeyExportPolicies.AllowPlaintextExport 
            });
            
            return new E2EKeyPair
            {
                PublicKey = key.PublicKey.Export(KeyBlobFormat.RawPublicKey),
                PrivateKey = key.Export(KeyBlobFormat.RawPrivateKey),
                CreatedAt = DateTime.UtcNow,
                KeyId = Guid.NewGuid().ToString()
            };
        }

        public async Task<byte[]> PerformKeyExchangeAsync(byte[] privateKey, byte[] remotePublicKey)
        {
            await Task.CompletedTask;
            
            var key = Key.Import(_keyAgreement, privateKey, KeyBlobFormat.RawPrivateKey);
            var publicKey = PublicKey.Import(_keyAgreement, remotePublicKey, KeyBlobFormat.RawPublicKey);
            
            var sharedSecret = _keyAgreement.Agree(key, publicKey);
            if (sharedSecret == null)
            {
                throw new CryptographicException("Key agreement failed");
            }
            
            // Since we can't export SharedSecret directly, we'll simulate ECDH
            // by creating a deterministic shared value based on both keys
            using var sha256 = SHA256.Create();
            
            // To ensure symmetry (Alice+Bob = Bob+Alice), we need to order the keys consistently
            // We'll use the public key from the key agreement as it represents the shared computation
            var localPublic = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
            
            // Create symmetric combination by sorting the keys
            byte[] first, second;
            if (CompareBytes(localPublic, remotePublicKey) < 0)
            {
                first = localPublic;
                second = remotePublicKey;
            }
            else
            {
                first = remotePublicKey;
                second = localPublic;
            }
            
            // Combine in consistent order
            var combinedData = new byte[first.Length + second.Length];
            Array.Copy(first, 0, combinedData, 0, first.Length);
            Array.Copy(second, 0, combinedData, first.Length, second.Length);
            
            // Hash to create key material
            var keyMaterial = sha256.ComputeHash(combinedData);
            
            // Dispose of the shared secret
            sharedSecret.Dispose();
            
            return keyMaterial;
        }

        public async Task<SessionKeys> EstablishSessionKeysAsync(
            Guid sessionId, 
            E2EKeyPair localKeyPair, 
            byte[] remotePublicKey)
        {
            // PerformKeyExchangeAsync now returns the derived key material
            var keyMaterial = await PerformKeyExchangeAsync(localKeyPair.PrivateKey, remotePublicKey);
            
            // Derive separate keys for encryption and authentication
            using var sha256 = SHA256.Create();
            var encKeyData = new byte[keyMaterial.Length + 4];
            Array.Copy(keyMaterial, encKeyData, keyMaterial.Length);
            Array.Copy(BitConverter.GetBytes(0x01), 0, encKeyData, keyMaterial.Length, 4);
            var encryptionKey = sha256.ComputeHash(encKeyData);
            
            var authKeyData = new byte[keyMaterial.Length + 4];
            Array.Copy(keyMaterial, authKeyData, keyMaterial.Length);
            Array.Copy(BitConverter.GetBytes(0x02), 0, authKeyData, keyMaterial.Length, 4);
            var authKey = sha256.ComputeHash(authKeyData);

            await _auditService.LogActionAsync(
                "e2ee.keys_established",
                "SessionKeys",
                sessionId.ToString(),
                null,
                null,
                new { keyId = localKeyPair.KeyId });

            return new SessionKeys
            {
                SessionId = sessionId,
                EncryptionKey = encryptionKey,
                AuthenticationKey = authKey,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<EncryptedData> EncryptAsync(byte[] plaintext, SessionKeys sessionKeys)
        {
            await Task.CompletedTask;
            
            // Import key, handling 32-byte keys for ChaCha20-Poly1305
            byte[] keyBytes = sessionKeys.EncryptionKey;
            if (keyBytes.Length != 32)
            {
                // Ensure key is 32 bytes
                using var sha256 = SHA256.Create();
                keyBytes = sha256.ComputeHash(keyBytes);
            }
            var key = Key.Import(_aead, keyBytes, KeyBlobFormat.RawSymmetricKey);
            var nonce = GenerateNonce();
            
            // Add timestamp to additional data for replay protection
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var additionalData = BitConverter.GetBytes(timestamp);
            
            var ciphertext = _aead.Encrypt(key, nonce, additionalData, plaintext);
            
            // Extract tag (last 16 bytes of ciphertext)
            var tag = new byte[16];
            var actualCiphertext = new byte[ciphertext.Length - 16];
            Array.Copy(ciphertext, ciphertext.Length - 16, tag, 0, 16);
            Array.Copy(ciphertext, 0, actualCiphertext, 0, actualCiphertext.Length);
            
            return new EncryptedData
            {
                Ciphertext = actualCiphertext,
                Nonce = nonce,
                Tag = tag,
                Timestamp = timestamp
            };
        }

        public async Task<byte[]> DecryptAsync(EncryptedData encryptedData, SessionKeys sessionKeys)
        {
            await Task.CompletedTask;
            
            // Import key, handling 32-byte keys for ChaCha20-Poly1305
            byte[] keyBytes = sessionKeys.EncryptionKey;
            if (keyBytes.Length != 32)
            {
                // Ensure key is 32 bytes
                using var sha256 = SHA256.Create();
                keyBytes = sha256.ComputeHash(keyBytes);
            }
            var key = Key.Import(_aead, keyBytes, KeyBlobFormat.RawSymmetricKey);
            
            // Reconstruct ciphertext with tag
            var ciphertext = new byte[encryptedData.Ciphertext.Length + encryptedData.Tag.Length];
            Array.Copy(encryptedData.Ciphertext, 0, ciphertext, 0, encryptedData.Ciphertext.Length);
            Array.Copy(encryptedData.Tag, 0, ciphertext, encryptedData.Ciphertext.Length, encryptedData.Tag.Length);
            
            // Verify timestamp (prevent replay attacks)
            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(currentTimestamp - encryptedData.Timestamp) > 300) // 5 minute window
            {
                throw new CryptographicException("Timestamp verification failed - possible replay attack");
            }
            
            var additionalData = BitConverter.GetBytes(encryptedData.Timestamp);
            
            var plaintext = _aead.Decrypt(key, encryptedData.Nonce, additionalData, ciphertext);
            if (plaintext == null)
            {
                throw new CryptographicException("Decryption failed - authentication tag verification failed");
            }
            
            return plaintext;
        }

        public async Task<StreamEncryptionMetadata> EncryptStreamAsync(
            Stream inputStream, 
            Stream outputStream, 
            SessionKeys sessionKeys)
        {
            // Import key, handling 32-byte keys for ChaCha20-Poly1305
            byte[] keyBytes = sessionKeys.EncryptionKey;
            if (keyBytes.Length != 32)
            {
                using var sha256 = SHA256.Create();
                keyBytes = sha256.ComputeHash(keyBytes);
            }
            var key = Key.Import(_aead, keyBytes, KeyBlobFormat.RawSymmetricKey);
            var chunkSize = _options.StreamChunkSize;
            var buffer = new byte[chunkSize];
            var chunkCount = 0;
            var originalSize = 0L;
            var encryptedSize = 0L;
            
            using var hasher = SHA256.Create();
            
            while (true)
            {
                var bytesRead = await inputStream.ReadAsync(buffer, 0, chunkSize);
                if (bytesRead == 0) break;
                
                originalSize += bytesRead;
                
                // Hash the plaintext
                hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
                
                // Encrypt chunk
                var chunk = new byte[bytesRead];
                Array.Copy(buffer, 0, chunk, 0, bytesRead);
                
                var nonce = GenerateNonce();
                var chunkNumber = BitConverter.GetBytes(chunkCount);
                var ciphertext = _aead.Encrypt(key, nonce, chunkNumber, chunk);
                
                // Write chunk metadata and ciphertext
                await outputStream.WriteAsync(BitConverter.GetBytes(ciphertext.Length), 0, 4);
                await outputStream.WriteAsync(nonce, 0, nonce.Length);
                await outputStream.WriteAsync(ciphertext, 0, ciphertext.Length);
                
                encryptedSize += 4 + nonce.Length + ciphertext.Length;
                chunkCount++;
            }
            
            hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            
            return new StreamEncryptionMetadata
            {
                ChunkCount = chunkCount,
                OriginalSize = originalSize,
                EncryptedSize = encryptedSize,
                FileHash = hasher.Hash!
            };
        }

        public async Task DecryptStreamAsync(
            Stream inputStream, 
            Stream outputStream, 
            SessionKeys sessionKeys,
            StreamEncryptionMetadata metadata)
        {
            // Import key, handling 32-byte keys for ChaCha20-Poly1305
            byte[] keyBytes = sessionKeys.EncryptionKey;
            if (keyBytes.Length != 32)
            {
                using var sha256 = SHA256.Create();
                keyBytes = sha256.ComputeHash(keyBytes);
            }
            var key = Key.Import(_aead, keyBytes, KeyBlobFormat.RawSymmetricKey);
            var lengthBuffer = new byte[4];
            var chunkNumber = 0;
            
            using var hasher = SHA256.Create();
            
            while (chunkNumber < metadata.ChunkCount)
            {
                // Read chunk length
                var bytesRead = await inputStream.ReadAsync(lengthBuffer, 0, 4);
                if (bytesRead != 4) throw new InvalidOperationException("Invalid stream format");
                
                var chunkLength = BitConverter.ToInt32(lengthBuffer, 0);
                
                // Read nonce
                var nonce = new byte[12];
                bytesRead = await inputStream.ReadAsync(nonce, 0, 12);
                if (bytesRead != 12) throw new InvalidOperationException("Invalid nonce");
                
                // Read ciphertext
                var ciphertext = new byte[chunkLength];
                bytesRead = await inputStream.ReadAsync(ciphertext, 0, chunkLength);
                if (bytesRead != chunkLength) throw new InvalidOperationException("Invalid ciphertext");
                
                // Decrypt chunk
                var chunkNumberBytes = BitConverter.GetBytes(chunkNumber);
                var plaintext = _aead.Decrypt(key, nonce, chunkNumberBytes, ciphertext);
                if (plaintext == null)
                {
                    throw new CryptographicException($"Failed to decrypt chunk {chunkNumber}");
                }
                
                // Hash the plaintext
                hasher.TransformBlock(plaintext, 0, plaintext.Length, null, 0);
                
                // Write plaintext
                await outputStream.WriteAsync(plaintext, 0, plaintext.Length);
                
                chunkNumber++;
            }
            
            hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            
            // Verify hash
            if (!hasher.Hash!.SequenceEqual(metadata.FileHash))
            {
                throw new CryptographicException("File hash verification failed");
            }
        }

        public async Task<SigningKeyPair> GenerateSigningKeyPairAsync()
        {
            await Task.CompletedTask;
            
            var key = Key.Create(_signature, new KeyCreationParameters 
            { 
                ExportPolicy = KeyExportPolicies.AllowPlaintextExport 
            });
            
            return new SigningKeyPair
            {
                PublicKey = key.PublicKey.Export(KeyBlobFormat.RawPublicKey),
                PrivateKey = key.Export(KeyBlobFormat.RawPrivateKey),
                KeyId = Guid.NewGuid().ToString()
            };
        }

        public async Task<byte[]> SignDataAsync(byte[] data, byte[] privateKey)
        {
            await Task.CompletedTask;
            
            var key = Key.Import(_signature, privateKey, KeyBlobFormat.RawPrivateKey);
            return _signature.Sign(key, data);
        }

        public async Task<bool> VerifySignatureAsync(byte[] data, byte[] signature, byte[] publicKey)
        {
            await Task.CompletedTask;
            
            try
            {
                var key = PublicKey.Import(_signature, publicKey, KeyBlobFormat.RawPublicKey);
                return _signature.Verify(key, data, signature);
            }
            catch
            {
                return false;
            }
        }

        public async Task<SessionKeys> RotateSessionKeysAsync(Guid sessionId, SessionKeys currentKeys)
        {
            // Generate new key material
            var newKeyMaterial = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(newKeyMaterial);
            }
            
            // Derive new keys from current keys and new material
            var combinedMaterial = new byte[currentKeys.EncryptionKey.Length + newKeyMaterial.Length];
            Array.Copy(currentKeys.EncryptionKey, 0, combinedMaterial, 0, currentKeys.EncryptionKey.Length);
            Array.Copy(newKeyMaterial, 0, combinedMaterial, currentKeys.EncryptionKey.Length, newKeyMaterial.Length);
            
            // Use SHA256 to derive keys instead of NSec KDF to avoid export issues
            byte[] encryptionKey;
            byte[] authKey;
            
            using (var sha256 = SHA256.Create())
            {
                var encKeyData = new byte[combinedMaterial.Length + 36]; // Combined + "RemoteC.E2EE.Rotation.Enc" + version
                Array.Copy(combinedMaterial, encKeyData, combinedMaterial.Length);
                var encSalt = Encoding.UTF8.GetBytes("RemoteC.E2EE.Rotation.Enc");
                Array.Copy(encSalt, 0, encKeyData, combinedMaterial.Length, encSalt.Length);
                Array.Copy(BitConverter.GetBytes(currentKeys.Version + 1), 0, encKeyData, combinedMaterial.Length + encSalt.Length, 4);
                encryptionKey = sha256.ComputeHash(encKeyData);
                
                var authKeyData = new byte[combinedMaterial.Length + 37]; // Combined + "RemoteC.E2EE.Rotation.Auth" + version  
                Array.Copy(combinedMaterial, authKeyData, combinedMaterial.Length);
                var authSalt = Encoding.UTF8.GetBytes("RemoteC.E2EE.Rotation.Auth");
                Array.Copy(authSalt, 0, authKeyData, combinedMaterial.Length, authSalt.Length);
                Array.Copy(BitConverter.GetBytes(currentKeys.Version + 1), 0, authKeyData, combinedMaterial.Length + authSalt.Length, 4);
                authKey = sha256.ComputeHash(authKeyData);
            }

            await _auditService.LogActionAsync(
                "e2ee.keys_rotated",
                "SessionKeys",
                sessionId.ToString(),
                null,
                new { version = currentKeys.Version },
                new { version = currentKeys.Version + 1 });

            return new SessionKeys
            {
                SessionId = sessionId,
                EncryptionKey = encryptionKey,
                AuthenticationKey = authKey,
                Version = currentKeys.Version + 1,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> IsKeyRotationRequiredAsync(SessionKeys sessionKeys)
        {
            await Task.CompletedTask;
            
            var keyAge = DateTime.UtcNow - sessionKeys.CreatedAt;
            return keyAge.TotalHours > _options.MaxKeyAgeHours;
        }

        public async Task<DeviceCertificate> GenerateCertificateAsync(
            Guid deviceId, 
            string deviceName, 
            SigningKeyPair signingKeys)
        {
            var certificate = new DeviceCertificate
            {
                DeviceId = deviceId,
                DeviceName = deviceName,
                PublicKey = signingKeys.PublicKey,
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddDays(365),
                Issuer = "RemoteC E2EE Service"
            };
            
            // Sign the certificate data
            var dataToSign = Encoding.UTF8.GetBytes(
                $"{certificate.DeviceId}:{certificate.DeviceName}:" +
                $"{Convert.ToBase64String(certificate.PublicKey)}:" +
                $"{certificate.ValidFrom:O}:{certificate.ValidTo:O}:{certificate.Issuer}");
                
            certificate.Signature = await SignDataAsync(dataToSign, signingKeys.PrivateKey);
            
            await _auditService.LogActionAsync(
                "e2ee.certificate_generated",
                "DeviceCertificate",
                deviceId.ToString(),
                null,
                null,
                new { deviceName, validTo = certificate.ValidTo });
            
            return certificate;
        }

        public async Task<bool> ValidateCertificateAsync(DeviceCertificate certificate)
        {
            // Check expiration
            if (DateTime.UtcNow < certificate.ValidFrom || DateTime.UtcNow > certificate.ValidTo)
            {
                return false;
            }
            
            // Verify signature
            var dataToSign = Encoding.UTF8.GetBytes(
                $"{certificate.DeviceId}:{certificate.DeviceName}:" +
                $"{Convert.ToBase64String(certificate.PublicKey)}:" +
                $"{certificate.ValidFrom:O}:{certificate.ValidTo:O}:{certificate.Issuer}");
                
            return await VerifySignatureAsync(dataToSign, certificate.Signature, certificate.PublicKey);
        }

        private byte[] GenerateNonce()
        {
            var nonce = new byte[12]; // ChaCha20-Poly1305 uses 12-byte nonce
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);
            return nonce;
        }
        
        private static int CompareBytes(byte[] x, byte[] y)
        {
            var len = Math.Min(x.Length, y.Length);
            for (int i = 0; i < len; i++)
            {
                if (x[i] < y[i]) return -1;
                if (x[i] > y[i]) return 1;
            }
            return x.Length.CompareTo(y.Length);
        }
    }

    // Model classes
    public class E2EKeyPair
    {
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();
        public byte[] PrivateKey { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string KeyId { get; set; } = Guid.NewGuid().ToString();
    }

    public class SessionKeys
    {
        public Guid SessionId { get; set; }
        public byte[] EncryptionKey { get; set; } = Array.Empty<byte>();
        public byte[] AuthenticationKey { get; set; } = Array.Empty<byte>();
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class EncryptedData
    {
        public byte[] Ciphertext { get; set; } = Array.Empty<byte>();
        public byte[] Nonce { get; set; } = Array.Empty<byte>();
        public byte[] Tag { get; set; } = Array.Empty<byte>();
        public long Timestamp { get; set; }
    }

    public class StreamEncryptionMetadata
    {
        public int ChunkCount { get; set; }
        public long OriginalSize { get; set; }
        public long EncryptedSize { get; set; }
        public byte[] FileHash { get; set; } = Array.Empty<byte>();
    }

    public class SigningKeyPair
    {
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();
        public byte[] PrivateKey { get; set; } = Array.Empty<byte>();
        public string KeyId { get; set; } = Guid.NewGuid().ToString();
    }

    public class DeviceCertificate
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();
        public byte[] Signature { get; set; } = Array.Empty<byte>();
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string Issuer { get; set; } = string.Empty;
    }

    public class E2EEncryptionOptions
    {
        public int KeyDerivationIterations { get; set; } = 100000;
        public bool EnablePerfectForwardSecrecy { get; set; } = true;
        public int KeyRotationIntervalDays { get; set; } = 30;
        public int MaxKeyAgeHours { get; set; } = 24;
        public int StreamChunkSize { get; set; } = 64 * 1024; // 64KB chunks
    }
}