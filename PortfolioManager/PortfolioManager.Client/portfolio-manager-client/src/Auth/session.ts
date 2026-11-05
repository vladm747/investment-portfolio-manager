import AuthResultDto from "../DTO/AuthResultDto";
import UserDto from "../DTO/UserDto";

/**
 * Where the tokens live:
 * - the access token only in memory, so it disappears with the tab and is never written anywhere;
 * - the refresh token in localStorage, so a page reload or a new tab can get a fresh access token.
 *
 * The API also sets the refresh token as an HttpOnly cookie, but the client and the API are different
 * sites on Azure and some browsers (Safari) block such third-party cookies, so the client sends it itself.
 */
export const REFRESH_TOKEN_KEY = 'portfolioManager.refreshToken';

export interface Session {
    accessToken: string;
    user: UserDto;
}

let current: Session | null = null;
// Used only when localStorage is unavailable (blocked by the browser): the session then lasts until the tab closes.
let fallbackRefreshToken: string | null = null;
const listeners = new Set<() => void>();

function notify() {
    listeners.forEach((listener) => listener());
}

function readRefreshToken(): string | null {
    try {
        return window.localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
        return fallbackRefreshToken;
    }
}

function writeRefreshToken(token: string | null) {
    fallbackRefreshToken = token;
    try {
        if (token) {
            window.localStorage.setItem(REFRESH_TOKEN_KEY, token);
        } else {
            window.localStorage.removeItem(REFRESH_TOKEN_KEY);
        }
    } catch {
        // Storage blocked: keep the in-memory copy above.
    }
}

export const sessionStore = {
    get: (): Session | null => current,

    getAccessToken: (): string | null => current?.accessToken ?? null,

    getRefreshToken: readRefreshToken,

    start(result: AuthResultDto) {
        current = { accessToken: result.accessToken, user: result.user };
        writeRefreshToken(result.refreshToken);
        notify();
    },

    end() {
        const hadSession = current !== null || readRefreshToken() !== null;
        current = null;
        writeRefreshToken(null);
        if (hadSession) {
            notify();
        }
    },

    subscribe(listener: () => void): () => void {
        listeners.add(listener);
        return () => {
            listeners.delete(listener);
        };
    },
};

// Signing out in one tab signs out every other open tab.
window.addEventListener('storage', (event) => {
    if (event.key === REFRESH_TOKEN_KEY && event.newValue === null && current !== null) {
        current = null;
        notify();
    }
});
