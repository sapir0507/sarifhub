import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { api } from '../api/client';
import type { CurrentUser } from '../api/types';

/**
 * Phase 1: mock session. Only the display user is kept (sessionStorage), never a token.
 * Phase 6 replaces this with real JWT handling; token storage is decided there.
 */
const SESSION_KEY = 'sarifhub.demoUser';

function readSession(): CurrentUser | null {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    return raw ? (JSON.parse(raw) as CurrentUser) : null;
  } catch {
    return null;
  }
}

interface AuthContextValue {
  user: CurrentUser | null;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(readSession);

  const signIn = useCallback(async (email: string, password: string) => {
    const u = await api.login(email, password);
    setUser(u);
    try {
      sessionStorage.setItem(SESSION_KEY, JSON.stringify(u));
    } catch {
      /* storage unavailable: session lasts until reload */
    }
  }, []);

  const signOut = useCallback(() => {
    setUser(null);
    try {
      sessionStorage.removeItem(SESSION_KEY);
    } catch {
      /* ignore */
    }
  }, []);

  const value = useMemo(() => ({ user, signIn, signOut }), [user, signIn, signOut]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}

export function RequireAuth() {
  const { user } = useAuth();
  const location = useLocation();
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return <Outlet />;
}
