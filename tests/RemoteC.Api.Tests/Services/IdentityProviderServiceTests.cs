using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RemoteC.Api.Services;
using RemoteC.Data;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class IdentityProviderServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<ILogger<IdentityProviderService>> _loggerMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly Mock<ICertificateService> _certMock;
        private readonly IdentityProviderService _service;
        private readonly IdentityProviderOptions _options;
        private readonly RSA _rsaKey;

        public IdentityProviderServiceTests()
        {
            // Setup mocks
            _loggerMock = new Mock<ILogger<IdentityProviderService>>();
            _auditMock = new Mock<IAuditService>();
            _certMock = new Mock<ICertificateService>();

            // Generate RSA key for testing
            _rsaKey = RSA.Create(2048);

            // Setup options
            _options = new IdentityProviderOptions
            {
                Issuer = "https://remotec.example.com",
                TokenLifetime = TimeSpan.FromHours(1),
                RefreshTokenLifetime = TimeSpan.FromDays(30),
                EnableOAuth2 = true,
                EnableSAML2 = true,
                EnableOpenIDConnect = true,
                SigningKey = new RsaSecurityKey(_rsaKey),
                AllowedScopes = new[] { "openid", "profile", "email", "api" }
            };

            // Create service
            _service = new IdentityProviderService(
                _loggerMock.Object,
                _auditMock.Object,
                _certMock.Object,
                Options.Create(_options));
        }

        #region OAuth 2.0 Tests

        [Fact]
        public async Task AuthorizeAsync_ValidRequest_ReturnsAuthorizationCode()
        {
            // Arrange
            var request = new OAuth2AuthorizationRequest
            {
                ClientId = "test-client",
                RedirectUri = "https://app.example.com/callback",
                ResponseType = "code",
                Scope = "openid profile",
                State = "random-state",
                UserId = Guid.NewGuid()
            };

            // Act
            var response = await _service.AuthorizeAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Code);
            Assert.Equal(request.State, response.State);
            Assert.Null(response.Error);
            Assert.True(response.ExpiresIn > 0);
        }

        [Fact]
        public async Task AuthorizeAsync_InvalidClient_ReturnsError()
        {
            // Arrange
            var request = new OAuth2AuthorizationRequest
            {
                ClientId = "invalid-client",
                RedirectUri = "https://app.example.com/callback",
                ResponseType = "code",
                Scope = "openid"
            };

            // Act
            var response = await _service.AuthorizeAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Null(response.Code);
            Assert.Equal("invalid_client", response.Error);
            Assert.NotNull(response.ErrorDescription);
        }

        [Fact]
        public async Task TokenAsync_ValidAuthorizationCode_ReturnsTokens()
        {
            // Arrange
            var authRequest = new OAuth2AuthorizationRequest
            {
                ClientId = "test-client",
                RedirectUri = "https://app.example.com/callback",
                ResponseType = "code",
                Scope = "openid profile email",
                UserId = Guid.NewGuid()
            };

            var authResponse = await _service.AuthorizeAsync(authRequest);

            var tokenRequest = new OAuth2TokenRequest
            {
                GrantType = "authorization_code",
                Code = authResponse.Code,
                RedirectUri = authRequest.RedirectUri,
                ClientId = authRequest.ClientId,
                ClientSecret = "test-secret"
            };

            // Act
            var tokenResponse = await _service.TokenAsync(tokenRequest);

            // Assert
            Assert.NotNull(tokenResponse);
            Assert.NotNull(tokenResponse.AccessToken);
            Assert.NotNull(tokenResponse.RefreshToken);
            Assert.NotNull(tokenResponse.IdToken);
            Assert.Equal("Bearer", tokenResponse.TokenType);
            Assert.True(tokenResponse.ExpiresIn > 0);
            Assert.Contains("openid", tokenResponse.Scope);
            Assert.Contains("profile", tokenResponse.Scope);
            Assert.Contains("email", tokenResponse.Scope);
        }

        [Fact]
        public async Task TokenAsync_RefreshToken_ReturnsNewTokens()
        {
            // Arrange
            // First get initial tokens
            var initialTokens = await GetTestTokens();

            var refreshRequest = new OAuth2TokenRequest
            {
                GrantType = "refresh_token",
                RefreshToken = initialTokens.RefreshToken,
                ClientId = "test-client",
                ClientSecret = "test-secret"
            };

            // Act
            var newTokens = await _service.TokenAsync(refreshRequest);

            // Assert
            Assert.NotNull(newTokens);
            Assert.NotNull(newTokens.AccessToken);
            Assert.NotNull(newTokens.RefreshToken);
            Assert.NotEqual(initialTokens.AccessToken, newTokens.AccessToken);
            Assert.NotEqual(initialTokens.RefreshToken, newTokens.RefreshToken);
        }

        [Fact]
        public async Task TokenAsync_ClientCredentials_ReturnsAccessToken()
        {
            // Arrange
            var request = new OAuth2TokenRequest
            {
                GrantType = "client_credentials",
                ClientId = "service-client",
                ClientSecret = "service-secret",
                Scope = "api"
            };

            // Act
            var response = await _service.TokenAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.AccessToken);
            Assert.Null(response.RefreshToken); // No refresh token for client credentials
            Assert.Null(response.IdToken); // No ID token for client credentials
            Assert.Equal("api", response.Scope);
        }

        [Fact]
        public async Task RevokeTokenAsync_ValidToken_SuccessfullyRevokes()
        {
            // Arrange
            var tokens = await GetTestTokens();

            var revokeRequest = new TokenRevocationRequest
            {
                Token = tokens.AccessToken,
                TokenTypeHint = "access_token",
                ClientId = "test-client",
                ClientSecret = "test-secret"
            };

            // Act
            var result = await _service.RevokeTokenAsync(revokeRequest);

            // Assert
            Assert.True(result.Success);
            
            // Verify token is revoked
            var introspection = await _service.IntrospectTokenAsync(tokens.AccessToken);
            Assert.False(introspection.Active);
        }

        [Fact]
        public async Task IntrospectTokenAsync_ValidToken_ReturnsTokenInfo()
        {
            // Arrange
            var tokens = await GetTestTokens();

            // Act
            var introspection = await _service.IntrospectTokenAsync(tokens.AccessToken);

            // Assert
            Assert.NotNull(introspection);
            Assert.True(introspection.Active);
            Assert.Equal("test-client", introspection.ClientId);
            Assert.NotNull(introspection.Subject);
            Assert.Contains("openid", introspection.Scope);
            Assert.True(introspection.Exp > DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        #endregion

        #region OpenID Connect Tests

        [Fact]
        public async Task GetOpenIDConfigurationAsync_ReturnsValidConfiguration()
        {
            // Act
            var config = await _service.GetOpenIDConfigurationAsync();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(_options.Issuer, config.Issuer);
            Assert.NotNull(config.AuthorizationEndpoint);
            Assert.NotNull(config.TokenEndpoint);
            Assert.NotNull(config.UserinfoEndpoint);
            Assert.NotNull(config.JwksUri);
            Assert.Contains("code", config.ResponseTypesSupported);
            Assert.Contains("RS256", config.IdTokenSigningAlgValuesSupported);
            Assert.Contains("openid", config.ScopesSupported);
        }

        [Fact]
        public async Task GetJWKSAsync_ReturnsPublicKeys()
        {
            // Act
            var jwks = await _service.GetJWKSAsync();

            // Assert
            Assert.NotNull(jwks);
            Assert.NotEmpty(jwks.Keys);
            
            var key = jwks.Keys.First();
            Assert.Equal("RSA", key.Kty);
            Assert.Equal("sig", key.Use);
            Assert.NotNull(key.Kid);
            Assert.NotNull(key.N); // RSA modulus
            Assert.NotNull(key.E); // RSA exponent
        }

        [Fact]
        public async Task GetUserInfoAsync_ValidToken_ReturnsUserClaims()
        {
            // Arrange
            var tokens = await GetTestTokens();

            // Act
            var userInfo = await _service.GetUserInfoAsync(tokens.AccessToken);

            // Assert
            Assert.NotNull(userInfo);
            Assert.NotNull(userInfo.Sub);
            Assert.NotNull(userInfo.Name);
            Assert.NotNull(userInfo.Email);
            Assert.Equal(userInfo.Sub, tokens.UserId.ToString());
        }

        [Fact]
        public async Task ValidateIdTokenAsync_ValidToken_PassesValidation()
        {
            // Arrange
            var tokens = await GetTestTokens();

            // Act
            var validation = await _service.ValidateIdTokenAsync(tokens.IdToken, "test-client");

            // Assert
            Assert.True(validation.IsValid);
            Assert.NotNull(validation.Claims);
            Assert.Contains(validation.Claims, c => c.Type == "sub");
            Assert.Contains(validation.Claims, c => c.Type == "aud" && c.Value == "test-client");
            Assert.Contains(validation.Claims, c => c.Type == "iss" && c.Value == _options.Issuer);
        }

        #endregion

        #region SAML 2.0 Tests

        [Fact]
        public async Task CreateSAMLResponseAsync_ValidRequest_ReturnsSignedResponse()
        {
            // Arrange
            var request = new SAML2AuthnRequest
            {
                Id = "_" + Guid.NewGuid().ToString("N"),
                Issuer = "https://sp.example.com",
                AssertionConsumerServiceURL = "https://sp.example.com/saml/acs",
                RequestedAuthnContext = "PasswordProtectedTransport",
                UserId = Guid.NewGuid(),
                UserEmail = "user@example.com",
                UserName = "Test User"
            };

            // Act
            var response = await _service.CreateSAMLResponseAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.SAMLResponse);
            Assert.NotNull(response.RelayState);
            
            // Decode and verify structure
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(response.SAMLResponse));
            Assert.Contains("<samlp:Response", decoded);
            Assert.Contains("<saml:Assertion", decoded);
            Assert.Contains("<ds:Signature", decoded);
        }

        [Fact]
        public async Task ValidateSAMLRequestAsync_ValidRequest_PassesValidation()
        {
            // Arrange
            var samlRequest = CreateTestSAMLRequest();

            // Act
            var validation = await _service.ValidateSAMLRequestAsync(samlRequest);

            // Assert
            Assert.True(validation.IsValid);
            Assert.NotNull(validation.RequestId);
            Assert.NotNull(validation.Issuer);
            Assert.NotNull(validation.AssertionConsumerServiceURL);
        }

        [Fact]
        public async Task ProcessSAMLLogoutRequestAsync_ValidRequest_CreatesLogoutResponse()
        {
            // Arrange
            var logoutRequest = new SAML2LogoutRequest
            {
                Id = "_" + Guid.NewGuid().ToString("N"),
                Issuer = "https://sp.example.com",
                NameId = "user@example.com",
                SessionIndex = "session-123"
            };

            // Act
            var response = await _service.ProcessSAMLLogoutRequestAsync(logoutRequest);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Success", response.Status);
            Assert.NotNull(response.LogoutResponse);
            
            // Verify logout was processed
            _auditMock.Verify(a => a.LogAsync(
                It.Is<AuditLogEntry>(e => e.Action == "SAMLLogout"),
                default), 
                Times.Once);
        }

        [Fact]
        public async Task GetSAMLMetadataAsync_ReturnsValidMetadata()
        {
            // Act
            var metadata = await _service.GetSAMLMetadataAsync();

            // Assert
            Assert.NotNull(metadata);
            
            // Parse XML
            var doc = new XmlDocument();
            doc.LoadXml(metadata);
            
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("md", "urn:oasis:names:tc:SAML:2.0:metadata");
            nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
            
            // Verify structure
            var entityDescriptor = doc.SelectSingleNode("//md:EntityDescriptor", nsmgr);
            Assert.NotNull(entityDescriptor);
            Assert.Equal(_options.Issuer, entityDescriptor.Attributes["entityID"]?.Value);
            
            var idpSSODescriptor = doc.SelectSingleNode("//md:IDPSSODescriptor", nsmgr);
            Assert.NotNull(idpSSODescriptor);
            
            var ssoService = doc.SelectSingleNode("//md:SingleSignOnService", nsmgr);
            Assert.NotNull(ssoService);
        }

        #endregion

        #region Multi-Factor Authentication Tests

        [Fact]
        public async Task InitiateMFAAsync_CreatesChallenge()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mfaType = MFAType.TOTP;

            // Act
            var challenge = await _service.InitiateMFAAsync(userId, mfaType);

            // Assert
            Assert.NotNull(challenge);
            Assert.Equal(mfaType, challenge.Type);
            Assert.NotNull(challenge.ChallengeId);
            
            if (mfaType == MFAType.TOTP)
            {
                Assert.NotNull(challenge.Secret);
                Assert.NotNull(challenge.QRCodeUrl);
            }
        }

        [Fact]
        public async Task VerifyMFAAsync_ValidCode_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var challenge = await _service.InitiateMFAAsync(userId, MFAType.TOTP);
            
            // Generate valid TOTP code (in real test would use actual TOTP algorithm)
            var validCode = "123456";

            // Act
            var result = await _service.VerifyMFAAsync(challenge.ChallengeId, validCode);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.BackupCodes);
            Assert.Equal(8, result.BackupCodes.Count);
        }

        #endregion

        #region Federation Tests

        [Fact]
        public async Task ConfigureFederationAsync_AddsIdentityProvider()
        {
            // Arrange
            var config = new FederationConfiguration
            {
                ProviderType = FederationProviderType.SAML,
                EntityId = "https://idp.partner.com",
                MetadataUrl = "https://idp.partner.com/metadata",
                DisplayName = "Partner IDP",
                Enabled = true
            };

            // Act
            var result = await _service.ConfigureFederationAsync(config);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(config.EntityId, result.EntityId);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetFederatedLoginUrlAsync_GeneratesCorrectUrl()
        {
            // Arrange
            var providerId = Guid.NewGuid();
            var returnUrl = "https://app.example.com/dashboard";

            // Act
            var loginUrl = await _service.GetFederatedLoginUrlAsync(providerId, returnUrl);

            // Assert
            Assert.NotNull(loginUrl);
            Assert.Contains("SAMLRequest", loginUrl);
            Assert.Contains("RelayState", loginUrl);
        }

        #endregion

        #region Session Management Tests

        [Fact]
        public async Task CreateSSOSessionAsync_EstablishesSession()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var clientId = "test-client";

            // Act
            var session = await _service.CreateSSOSessionAsync(userId, clientId);

            // Assert
            Assert.NotNull(session);
            Assert.NotNull(session.SessionId);
            Assert.Equal(userId, session.UserId);
            Assert.Contains(clientId, session.ParticipatingClients);
            Assert.True(session.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task ValidateSSOSessionAsync_ValidSession_ReturnsTrue()
        {
            // Arrange
            var session = await _service.CreateSSOSessionAsync(Guid.NewGuid(), "test-client");

            // Act
            var isValid = await _service.ValidateSSOSessionAsync(session.SessionId);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task TerminateSSOSessionAsync_EndsAllClientSessions()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var session = await _service.CreateSSOSessionAsync(userId, "client1");
            await _service.AddClientToSSOSessionAsync(session.SessionId, "client2");
            await _service.AddClientToSSOSessionAsync(session.SessionId, "client3");

            // Act
            var result = await _service.TerminateSSOSessionAsync(session.SessionId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.ClientsNotified.Count);
            
            // Verify session is invalid
            var isValid = await _service.ValidateSSOSessionAsync(session.SessionId);
            Assert.False(isValid);
        }

        #endregion

        #region Helper Methods

        private async Task<OAuth2TokenResponse> GetTestTokens()
        {
            var authRequest = new OAuth2AuthorizationRequest
            {
                ClientId = "test-client",
                RedirectUri = "https://app.example.com/callback",
                ResponseType = "code",
                Scope = "openid profile email",
                UserId = Guid.NewGuid()
            };

            var authResponse = await _service.AuthorizeAsync(authRequest);

            var tokenRequest = new OAuth2TokenRequest
            {
                GrantType = "authorization_code",
                Code = authResponse.Code,
                RedirectUri = authRequest.RedirectUri,
                ClientId = authRequest.ClientId,
                ClientSecret = "test-secret"
            };

            var tokens = await _service.TokenAsync(tokenRequest);
            tokens.UserId = authRequest.UserId; // For test convenience
            return tokens;
        }

        private string CreateTestSAMLRequest()
        {
            var request = $@"
                <samlp:AuthnRequest 
                    xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" 
                    xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
                    ID=""_{Guid.NewGuid():N}""
                    Version=""2.0""
                    IssueInstant=""{DateTime.UtcNow:O}""
                    AssertionConsumerServiceURL=""https://sp.example.com/saml/acs"">
                    <saml:Issuer>https://sp.example.com</saml:Issuer>
                </samlp:AuthnRequest>";

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(request));
        }

        public void Dispose()
        {
            _rsaKey?.Dispose();
        }

        #endregion
    }

    // Test models for identity provider
    public class OAuth2AuthorizationRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string? State { get; set; }
        public Guid UserId { get; set; }
    }

    public class OAuth2AuthorizationResponse
    {
        public string? Code { get; set; }
        public string? State { get; set; }
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class OAuth2TokenRequest
    {
        public string GrantType { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? RedirectUri { get; set; }
        public string? RefreshToken { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string? Scope { get; set; }
    }

    public class OAuth2TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
        public string? IdToken { get; set; }
        public string Scope { get; set; } = string.Empty;
        public Guid UserId { get; set; } // For testing
    }

    public class SAML2AuthnRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string AssertionConsumerServiceURL { get; set; } = string.Empty;
        public string? RequestedAuthnContext { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    public enum MFAType
    {
        TOTP,
        SMS,
        Email,
        WebAuthn
    }

    public enum FederationProviderType
    {
        SAML,
        OAuth2,
        OpenIDConnect
    }

    public class IdentityProviderOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public TimeSpan TokenLifetime { get; set; }
        public TimeSpan RefreshTokenLifetime { get; set; }
        public bool EnableOAuth2 { get; set; }
        public bool EnableSAML2 { get; set; }
        public bool EnableOpenIDConnect { get; set; }
        public SecurityKey SigningKey { get; set; } = null!;
        public string[] AllowedScopes { get; set; } = Array.Empty<string>();
    }
}