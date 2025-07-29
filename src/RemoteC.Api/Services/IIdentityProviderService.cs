using System;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public interface IIdentityProviderService
    {
        // OAuth 2.0
        Task<OAuth2AuthorizationResponse> AuthorizeAsync(OAuth2AuthorizationRequest request);
        Task<OAuth2TokenResponse> TokenAsync(OAuth2TokenRequest request);
        Task<TokenRevocationResponse> RevokeTokenAsync(TokenRevocationRequest request);
        Task<TokenIntrospectionResponse> IntrospectTokenAsync(string token);
        
        // OpenID Connect
        Task<OpenIDConfiguration> GetOpenIDConfigurationAsync();
        Task<JWKSet> GetJWKSAsync();
        Task<UserInfoResponse> GetUserInfoAsync(string accessToken);
        Task<IdTokenValidationResult> ValidateIdTokenAsync(string idToken, string clientId);
        
        // SAML 2.0
        Task<SAML2Response> CreateSAMLResponseAsync(SAML2AuthnRequest request);
        Task<SAMLRequestValidation> ValidateSAMLRequestAsync(string samlRequest);
        Task<SAML2LogoutResponse> ProcessSAMLLogoutRequestAsync(SAML2LogoutRequest request);
        Task<string> GetSAMLMetadataAsync();
        
        // Multi-Factor Authentication
        Task<MFAChallenge> InitiateMFAAsync(Guid userId, MFAType type);
        Task<MFAVerificationResult> VerifyMFAAsync(string challengeId, string code);
        Task<bool> DisableMFAAsync(Guid userId);
        Task<List<string>> GenerateBackupCodesAsync(Guid userId);
        
        // Federation
        Task<FederationProvider> ConfigureFederationAsync(FederationConfiguration config);
        Task<string> GetFederatedLoginUrlAsync(Guid providerId, string returnUrl);
        Task<OAuth2TokenResponse> ProcessFederatedCallbackAsync(string providerId, string code, string state);
        Task<List<FederationProvider>> GetFederationProvidersAsync();
        
        // SSO Session Management
        Task<SSOSession> CreateSSOSessionAsync(Guid userId, string clientId);
        Task<bool> ValidateSSOSessionAsync(string sessionId);
        Task<bool> AddClientToSSOSessionAsync(string sessionId, string clientId);
        Task<SSOTerminationResult> TerminateSSOSessionAsync(string sessionId);
        
        // Client Management
        Task<OAuth2Client> RegisterClientAsync(OAuth2Client client);
        Task<OAuth2Client?> GetClientAsync(string clientId);
        Task<bool> UpdateClientAsync(OAuth2Client client);
        Task<bool> DeleteClientAsync(string clientId);
    }

    public interface ICertificateService
    {
        Task<string> GetSigningCertificateAsync();
        Task<string> GetEncryptionCertificateAsync();
        Task<bool> ValidateCertificateAsync(string certificate);
    }
}