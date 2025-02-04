import { create } from "zustand";
import { persist, PersistStorage, StorageValue } from "zustand/middleware";
import { jwtDecode } from "jwt-decode";
import { encrypt, decrypt } from '../utils/auth/cryptoUtils.ts';
import { LoginResponse } from "../common/LoginResponse";
import { validateTokenIssuer, validateTokenStructure } from "../utils/auth/authUtils.ts";
import { clearAuthCookies } from "../utils/auth/cookieUtils.ts";

interface AuthState {
    accessToken: string | null;
    refreshToken: string | null;
    setTokens: (tokens: LoginResponse) => void;
    resetTokens: () => void;
    isTokenValid: () => boolean;
}

const sessionStorageStore: PersistStorage<AuthState> = {
    getItem: async (name) => {
        const item = sessionStorage.getItem(name);
        if (!item) return null;

        const decryptedData = await decrypt(item);
        return JSON.parse(decryptedData) as StorageValue<AuthState>;
    },
    setItem: async (name, value) => {
        const encryptedValue = await encrypt(JSON.stringify(value));
        sessionStorage.setItem(name, encryptedValue);
    },
    removeItem: (name) => {
        sessionStorage.removeItem(name);
    },
};

const useAuthStore = create<AuthState>()(
    persist(
        (set, get) => ({
            accessToken: null,
            refreshToken: null,

            setTokens: (tokens: LoginResponse) => {
                if (!tokens.accessToken) {
                    throw new Error('Invalid empty access token');
                }
                
                if (!validateTokenStructure(tokens.accessToken)) {
                    throw new Error('Invalid token format');
                }

                const decoded = jwtDecode(tokens.accessToken);
                if (!validateTokenIssuer(decoded.iss)) {
                    throw new Error('Untrusted token issuer');
                }

                set({
                    accessToken: tokens.accessToken,
                    refreshToken: tokens.refreshToken
                });
            },

            resetTokens: () => {
                set({ accessToken: null, refreshToken: null });
                clearAuthCookies();
            },

            isTokenValid: () => {
                try {
                    const token = get().accessToken;
                    if (!token) return false;
                    
                    const decoded = jwtDecode(token);
                    return !!decoded.exp && 
                           decoded.exp * 1000 > Date.now() && 
                           validateTokenIssuer(decoded.iss);
                } catch {
                    return false;
                }
            }
        }),
        {
            name: "auth-store",
            storage: sessionStorageStore
        }
    )
);

export default useAuthStore;