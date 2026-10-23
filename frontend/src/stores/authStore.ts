import { create } from "zustand";
import type { AuthTokenResponse, AuthUser } from "../types/auth";

interface AuthState {
  accessToken: string | null;
  expiresAtUtc: string | null;
  user: AuthUser | null;
  mustChangePassword: boolean;
  isAuthenticated: boolean;
  isInitialized: boolean;
  setSession: (session: AuthTokenResponse) => void;
  markPasswordChanged: () => void;
  clearSession: () => void;
  finishInitialization: () => void;
}

const emptySession = {
  accessToken: null,
  expiresAtUtc: null,
  user: null,
  mustChangePassword: false,
  isAuthenticated: false,
  isInitialized: false,
} as const;

export const useAuthStore = create<AuthState>((set) => ({
  ...emptySession,
  setSession: (session) =>
    set({
      accessToken: session.accessToken,
      expiresAtUtc: session.expiresAtUtc,
      user: {
        userId: session.userId,
        username: session.username,
        role: session.role,
      },
      mustChangePassword: session.mustChangePassword,
      isAuthenticated: Boolean(session.accessToken),
    }),
  markPasswordChanged: () => set({ mustChangePassword: false }),
  clearSession: () => set({ ...emptySession, isInitialized: true }),
  finishInitialization: () => set({ isInitialized: true }),
}));
