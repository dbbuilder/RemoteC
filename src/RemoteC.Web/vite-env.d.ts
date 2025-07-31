/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL: string
  readonly VITE_AZURE_CLIENT_ID: string
  readonly VITE_AZURE_TENANT_ID: string
  readonly VITE_AZURE_POLICY_ID: string
  readonly VITE_AZURE_REDIRECT_URI: string
  readonly VITE_AZURE_POST_LOGOUT_REDIRECT_URI: string
  // more env variables...
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}