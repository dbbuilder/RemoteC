using System;
using System.IO;
using System.Threading.Tasks;

namespace RemoteC.Api.Services
{
    public interface IE2EEncryptionService
    {
        // Key Exchange
        Task<E2EKeyPair> GenerateKeyPairAsync();
        Task<byte[]> PerformKeyExchangeAsync(byte[] privateKey, byte[] remotePublicKey);
        Task<SessionKeys> EstablishSessionKeysAsync(Guid sessionId, E2EKeyPair localKeyPair, byte[] remotePublicKey);
        
        // Encryption/Decryption
        Task<EncryptedData> EncryptAsync(byte[] plaintext, SessionKeys sessionKeys);
        Task<byte[]> DecryptAsync(EncryptedData encryptedData, SessionKeys sessionKeys);
        
        // Stream Encryption
        Task<StreamEncryptionMetadata> EncryptStreamAsync(Stream inputStream, Stream outputStream, SessionKeys sessionKeys);
        Task DecryptStreamAsync(Stream inputStream, Stream outputStream, SessionKeys sessionKeys, StreamEncryptionMetadata metadata);
        
        // Digital Signatures
        Task<SigningKeyPair> GenerateSigningKeyPairAsync();
        Task<byte[]> SignDataAsync(byte[] data, byte[] privateKey);
        Task<bool> VerifySignatureAsync(byte[] data, byte[] signature, byte[] publicKey);
        
        // Perfect Forward Secrecy
        Task<SessionKeys> RotateSessionKeysAsync(Guid sessionId, SessionKeys currentKeys);
        Task<bool> IsKeyRotationRequiredAsync(SessionKeys sessionKeys);
        
        // Certificate Management
        Task<DeviceCertificate> GenerateCertificateAsync(Guid deviceId, string deviceName, SigningKeyPair signingKeys);
        Task<bool> ValidateCertificateAsync(DeviceCertificate certificate);
    }
}