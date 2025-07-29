import { Configuration, LogLevel } from '@azure/msal-browser';

// Azure AD B2C configuration
const b2cPolicies = {
  names: {
    signUpSignIn: 'B2C_1_signupsignin',
    forgotPassword: 'B2C_1_passwordreset',
    editProfile: 'B2C_1_profileedit',
  },
  authorities: {
    signUpSignIn: {
      authority: `https://${process.env.REACT_APP_B2C_TENANT_NAME}.b2clogin.com/${process.env.REACT_APP_B2C_TENANT_NAME}.onmicrosoft.com/B2C_1_signupsignin`,
    },
    forgotPassword: {
      authority: `https://${process.env.REACT_APP_B2C_TENANT_NAME}.b2clogin.com/${process.env.REACT_APP_B2C_TENANT_NAME}.onmicrosoft.com/B2C_1_passwordreset`,
    },
    editProfile: {
      authority: `https://${process.env.REACT_APP_B2C_TENANT_NAME}.b2clogin.com/${process.env.REACT_APP_B2C_TENANT_NAME}.onmicrosoft.com/B2C_1_profileedit`,
    },
  },
  authorityDomain: `${process.env.REACT_APP_B2C_TENANT_NAME}.b2clogin.com`,
};

// MSAL configuration
export const msalConfig: Configuration = {
  auth: {
    clientId: process.env.REACT_APP_B2C_CLIENT_ID || '',
    authority: b2cPolicies.authorities.signUpSignIn.authority,
    knownAuthorities: [b2cPolicies.authorityDomain],
    redirectUri: process.env.REACT_APP_REDIRECT_URI || window.location.origin,
    postLogoutRedirectUri: process.env.REACT_APP_POST_LOGOUT_REDIRECT_URI || window.location.origin,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            return;
          case LogLevel.Info:
            console.info(message);
            return;
          case LogLevel.Verbose:
            console.debug(message);
            return;
          case LogLevel.Warning:
            console.warn(message);
            return;
          default:
            return;
        }
      },
    },
  },
};

// API scopes
export const apiConfig = {
  scopes: [`https://${process.env.REACT_APP_B2C_TENANT_NAME}.onmicrosoft.com/${process.env.REACT_APP_API_CLIENT_ID}/access_as_user`],
  uri: process.env.REACT_APP_API_URL || 'https://localhost:5001',
};

// Login request configuration
export const loginRequest = {
  scopes: ['openid', 'profile', ...apiConfig.scopes],
};

// Silent request configuration
export const silentRequest = {
  scopes: apiConfig.scopes,
};

export { b2cPolicies };