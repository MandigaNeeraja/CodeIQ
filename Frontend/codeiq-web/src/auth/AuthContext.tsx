import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import type { LoginResponse } from '../api/types';

interface AuthState {
  token: string;
  email: string;
  displayName: string;
  expiresAtUtc: string;
}

interface AuthContextValue {
  auth: AuthState | null;
  login: (response: LoginResponse) => void;
  logout: () => void;
  isAuthenticated: boolean;
}

const STORAGE_KEY = 'codeiq.admin.auth';

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function loadStoredAuth(): AuthState | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as AuthState;
    if (new Date(parsed.expiresAtUtc) <= new Date()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState | null>(() => loadStoredAuth());

  const login = useCallback((response: LoginResponse) => {
    const state: AuthState = {
      token: response.token,
      email: response.email,
      displayName: response.displayName,
      expiresAtUtc: response.expiresAtUtc,
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    setAuth(state);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setAuth(null);
  }, []);

  const value = useMemo(
    () => ({
      auth,
      login,
      logout,
      isAuthenticated: auth !== null,
    }),
    [auth, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
