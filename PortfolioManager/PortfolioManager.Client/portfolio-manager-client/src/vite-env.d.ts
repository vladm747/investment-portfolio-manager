/// <reference types="vite/client" />

interface ImportMetaEnv {
    /** Base URL of the API, with a trailing slash. */
    readonly VITE_API_URL: string;
}

interface ImportMeta {
    readonly env: ImportMetaEnv;
}
