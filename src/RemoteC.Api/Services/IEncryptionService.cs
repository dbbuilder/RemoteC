using System.Threading.Tasks;

namespace RemoteC.Api.Services
{
    public interface IEncryptionService
    {
        Task<string> GenerateKeyAsync();
        Task<byte[]> EncryptAsync(byte[] data, string keyId);
        Task<byte[]> DecryptAsync(byte[] data, string keyId);
        Task RevokeKeyAsync(string keyId);
        Task RotateKeysAsync();
    }
}