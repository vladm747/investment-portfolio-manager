/// <reference types="vite/client" />

interface ImportMetaEnv {
    /** Base URL of the API, with a trailing slash. */
    readonly VITE_API_URL: string;
    /** OAuth client ID of the Google Cloud web application. Empty until it is set; then sign-in says so. */
    readonly VITE_GOOGLE_CLIENT_ID?: string;
}

interface ImportMeta {
    readonly env: ImportMetaEnv;
}
