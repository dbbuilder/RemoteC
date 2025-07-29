using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace RemoteC.Api.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly string _signingCert;
        private readonly string _encryptionCert;

        public CertificateService()
        {
            // For POC, generate self-signed certificates
            _signingCert = GenerateSelfSignedCertificate("CN=RemoteC Signing Certificate");
            _encryptionCert = GenerateSelfSignedCertificate("CN=RemoteC Encryption Certificate");
        }

        public Task<string> GetSigningCertificateAsync()
        {
            return Task.FromResult(_signingCert);
        }

        public Task<string> GetEncryptionCertificateAsync()
        {
            return Task.FromResult(_encryptionCert);
        }

        public Task<bool> ValidateCertificateAsync(string certificate)
        {
            try
            {
                var certBytes = Convert.FromBase64String(certificate);
                var cert = new X509Certificate2(certBytes);
                
                // Basic validation
                return Task.FromResult(cert.NotAfter > DateTime.UtcNow && cert.NotBefore < DateTime.UtcNow);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private string GenerateSelfSignedCertificate(string subjectName)
        {
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    subjectName,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Add extensions
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                        true));

                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                        true));

                // Create certificate
                var cert = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddYears(1));

                // Export without private key
                return Convert.ToBase64String(cert.Export(X509ContentType.Cert));
            }
        }
    }
}