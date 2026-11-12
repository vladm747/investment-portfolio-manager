import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import UserDto from "../DTO/UserDto";
import { SignInWithGoogleAsync, SignOutAsync } from "../Services/AuthService";
import { refreshSession } from "../Services/http";
import { sessionStore } from "./session";

/** 'loading' only while a session saved by an earlier visit is being restored. */
export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';

interface AuthContextValue {
    status: AuthStatus;
    user: UserDto | null;
    /** `credential` is the ID token from the "Sign in with Google" button. The first sign-in creates the account. */
    signInWithGoogle: (credential: string) => Promise<void>;
    signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [session, setSession] = useState(sessionStore.get);
    const [restoring, setRestoring] = useState(
        () => sessionStore.get() === null && sessionStore.getRefreshToken() !== null
    );

    useEffect(() => sessionStore.subscribe(() => setSession(sessionStore.get())), []);

    useEffect(() => {
        if (!restoring) {
            return;
        }
        refreshSession()
            .catch(() => {
                // Expired, revoked or the server is unreachable: the user signs in again.
            })
            .finally(() => setRestoring(false));
        // Runs once on start-up only.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const signInWithGoogle = useCallback(async (credential: string) => {
        sessionStore.start(await SignInWithGoogleAsync(credential));
    }, []);

    const signOut = useCallback(() => {
        const refreshToken = sessionStore.getRefreshToken();
        const accessToken = sessionStore.getAccessToken();

        // Sign out here at once; revoking the refresh token on the server can finish in the background.
        sessionStore.end();
        SignOutAsync(refreshToken, accessToken).catch(() => {
            // Nothing to do: the token is gone from this browser and expires on the server anyway.
        });
    }, []);

    const value = useMemo<AuthContextValue>(() => ({
        status: restoring ? 'loading' : session ? 'authenticated' : 'anonymous',
        user: session?.user ?? null,
        signInWithGoogle,
        signOut,
    }), [restoring, session, signInWithGoogle, signOut]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used inside <AuthProvider>');
    }
    return context;
}
