import { Configuration, PopupRequest } from '@azure/msal-browser';

// MSAL configuration
export const msalConfig: Configuration = {
  auth: {
    clientId: process.env.REACT_APP_AZURE_CLIENT_ID || 'YOUR_CLIENT_ID',
    authority: process.env.REACT_APP_AZURE_AUTHORITY || 'https://YOUR_TENANT.b2clogin.com/YOUR_TENANT.onmicrosoft.com/B2C_1_signupsignin1',
    knownAuthorities: [process.env.REACT_APP_AZURE_KNOWN_AUTHORITY || 'YOUR_TENANT.b2clogin.com'],
    redirectUri: process.env.REACT_APP_REDIRECT_URI || window.location.origin,
    postLogoutRedirectUri: process.env.REACT_APP_POST_LOGOUT_REDIRECT_URI || window.location.origin,
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case 0: // LogLevel.Error
            console.error('[MSAL Error]', message);
            break;
          case 1: // LogLevel.Warning
            console.warn('[MSAL Warning]', message);
            break;
          case 2: // LogLevel.Info
            console.info('[MSAL Info]', message);
            break;
          case 3: // LogLevel.Verbose
            console.debug('[MSAL Debug]', message);
            break;
          default:
            console.log('[MSAL]', message);
            break;
        }
      },
      piiLoggingEnabled: false,
    },
  },
};

// Scopes for token requests
export const loginRequest: PopupRequest = {
  scopes: ['openid', 'profile', 'email'],
  prompt: 'select_account',
};

// Additional scopes for API access
export const apiScopes = {
  read: [process.env.REACT_APP_API_SCOPE_READ || 'https://YOUR_TENANT.onmicrosoft.com/remotec-api/read'],
  write: [process.env.REACT_APP_API_SCOPE_WRITE || 'https://YOUR_TENANT.onmicrosoft.com/remotec-api/write'],
  admin: [process.env.REACT_APP_API_SCOPE_ADMIN || 'https://YOUR_TENANT.onmicrosoft.com/remotec-api/admin'],
};

// Graph API configuration (if needed)
export const graphConfig = {
  graphMeEndpoint: 'https://graph.microsoft.com/v1.0/me',
  graphScopes: ['User.Read'],
};