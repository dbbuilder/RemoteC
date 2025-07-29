using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace RemoteC.Shared.Models
{
    // OAuth 2.0 Models
    public class OAuth2AuthorizationRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? CodeChallenge { get; set; }
        public string? CodeChallengeMethod { get; set; }
        public string? Nonce { get; set; }
        public Guid UserId { get; set; }
    }

    public class OAuth2AuthorizationResponse
    {
        public string? Code { get; set; }
        public string? State { get; set; }
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
        public int ExpiresIn { get; set; } = 600; // 10 minutes
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
        public string? CodeVerifier { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class OAuth2TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
        public string? IdToken { get; set; }
        public string Scope { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public class TokenRevocationRequest
    {
        public string Token { get; set; } = string.Empty;
        public string? TokenTypeHint { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    public class TokenRevocationResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class TokenIntrospectionResponse
    {
        public bool Active { get; set; }
        public string? Scope { get; set; }
        public string? ClientId { get; set; }
        public string? Username { get; set; }
        public string? Subject { get; set; }
        public long? Exp { get; set; }
        public long? Iat { get; set; }
        public string? TokenType { get; set; }
    }

    // OpenID Connect Models
    public class OpenIDConfiguration
    {
        public string Issuer { get; set; } = string.Empty;
        public string AuthorizationEndpoint { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = string.Empty;
        public string UserinfoEndpoint { get; set; } = string.Empty;
        public string JwksUri { get; set; } = string.Empty;
        public string RegistrationEndpoint { get; set; } = string.Empty;
        public string IntrospectionEndpoint { get; set; } = string.Empty;
        public string RevocationEndpoint { get; set; } = string.Empty;
        public string[] ScopesSupported { get; set; } = Array.Empty<string>();
        public string[] ResponseTypesSupported { get; set; } = Array.Empty<string>();
        public string[] ResponseModesSupported { get; set; } = Array.Empty<string>();
        public string[] GrantTypesSupported { get; set; } = Array.Empty<string>();
        public string[] SubjectTypesSupported { get; set; } = Array.Empty<string>();
        public string[] IdTokenSigningAlgValuesSupported { get; set; } = Array.Empty<string>();
        public string[] TokenEndpointAuthMethodsSupported { get; set; } = Array.Empty<string>();
        public string[] ClaimsSupported { get; set; } = Array.Empty<string>();
    }

    public class JWKSet
    {
        public List<JWK> Keys { get; set; } = new();
    }

    public class JWK
    {
        public string Kty { get; set; } = string.Empty; // Key type (RSA, EC)
        public string Use { get; set; } = string.Empty; // Key use (sig, enc)
        public string Kid { get; set; } = string.Empty; // Key ID
        public string Alg { get; set; } = string.Empty; // Algorithm
        public string N { get; set; } = string.Empty; // RSA modulus
        public string E { get; set; } = string.Empty; // RSA exponent
        public string? X { get; set; } // EC x coordinate
        public string? Y { get; set; } // EC y coordinate
        public string? Crv { get; set; } // EC curve
    }

    public class UserInfoResponse
    {
        public string Sub { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? MiddleName { get; set; }
        public string? Nickname { get; set; }
        public string? PreferredUsername { get; set; }
        public string? Profile { get; set; }
        public string? Picture { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }
        public bool? EmailVerified { get; set; }
        public string? Gender { get; set; }
        public string? Birthdate { get; set; }
        public string? Zoneinfo { get; set; }
        public string? Locale { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? PhoneNumberVerified { get; set; }
        public AddressClaimResponse? Address { get; set; }
        public long? UpdatedAt { get; set; }
    }

    public class AddressClaimResponse
    {
        public string? Formatted { get; set; }
        public string? StreetAddress { get; set; }
        public string? Locality { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }

    public class IdTokenValidationResult
    {
        public bool IsValid { get; set; }
        public List<Claim>? Claims { get; set; }
        public string? Error { get; set; }
    }

    // SAML 2.0 Models
    public class SAML2AuthnRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string AssertionConsumerServiceURL { get; set; } = string.Empty;
        public string? RequestedAuthnContext { get; set; }
        public string? ProtocolBinding { get; set; }
        public string? NameIdPolicy { get; set; }
        public DateTime IssueInstant { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    public class SAML2Response
    {
        public string SAMLResponse { get; set; } = string.Empty;
        public string? RelayState { get; set; }
    }

    public class SAML2LogoutRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string NameId { get; set; } = string.Empty;
        public string? SessionIndex { get; set; }
        public string? Destination { get; set; }
        public DateTime IssueInstant { get; set; }
    }

    public class SAML2LogoutResponse
    {
        public string Status { get; set; } = string.Empty;
        public string? StatusMessage { get; set; }
        public string LogoutResponse { get; set; } = string.Empty;
        public string? RelayState { get; set; }
    }

    public class SAMLRequestValidation
    {
        public bool IsValid { get; set; }
        public string? RequestId { get; set; }
        public string? Issuer { get; set; }
        public string? AssertionConsumerServiceURL { get; set; }
        public string? Error { get; set; }
    }

    // Multi-Factor Authentication Models
    public enum MFAType
    {
        TOTP,
        SMS,
        Email,
        WebAuthn,
        FaceID,
        Fingerprint
    }

    public class MFAChallenge
    {
        public string ChallengeId { get; set; } = string.Empty;
        public MFAType Type { get; set; }
        public string? Secret { get; set; }
        public string? QRCodeUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class MFAVerificationResult
    {
        public bool Success { get; set; }
        public List<string>? BackupCodes { get; set; }
        public string? Error { get; set; }
    }

    // Federation Models
    public enum FederationProviderType
    {
        SAML,
        OAuth2,
        OpenIDConnect,
        WsFederation,
        LDAP,
        ActiveDirectory
    }

    public class FederationConfiguration
    {
        public FederationProviderType ProviderType { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string? MetadataUrl { get; set; }
        public string? MetadataXml { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public Dictionary<string, string> AttributeMappings { get; set; } = new();
        public string? SigningCertificate { get; set; }
        public string? EncryptionCertificate { get; set; }
    }

    public class FederationProvider
    {
        public Guid Id { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public FederationProviderType Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // SSO Session Models
    public class SSOSession
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public List<string> ParticipatingClients { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class SSOTerminationResult
    {
        public bool Success { get; set; }
        public List<string> ClientsNotified { get; set; } = new();
        public List<string> FailedClients { get; set; } = new();
    }

    // Options
    public class IdentityProviderOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
        public TimeSpan AuthorizationCodeLifetime { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan SSOSessionLifetime { get; set; } = TimeSpan.FromHours(8);
        public bool EnableOAuth2 { get; set; } = true;
        public bool EnableSAML2 { get; set; } = true;
        public bool EnableOpenIDConnect { get; set; } = true;
        public bool RequirePKCE { get; set; } = true;
        public SecurityKey SigningKey { get; set; } = null!;
        public string SigningAlgorithm { get; set; } = "RS256";
        public string[] AllowedScopes { get; set; } = new[] { "openid", "profile", "email", "api" };
        public string[] AllowedGrantTypes { get; set; } = new[] { "authorization_code", "refresh_token", "client_credentials", "password" };
        public bool EnableMFA { get; set; } = true;
        public bool RequireMFAForAdmins { get; set; } = true;
    }

    // Client Models
    public class OAuth2Client
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public List<string> RedirectUris { get; set; } = new();
        public List<string> AllowedScopes { get; set; } = new();
        public List<string> AllowedGrantTypes { get; set; } = new();
        public bool RequirePKCE { get; set; }
        public bool AllowPlainTextPKCE { get; set; }
        public bool RequireClientSecret { get; set; }
        public bool AllowRememberConsent { get; set; }
        public TimeSpan? AccessTokenLifetime { get; set; }
        public TimeSpan? RefreshTokenLifetime { get; set; }
        public bool Enabled { get; set; }
    }
}