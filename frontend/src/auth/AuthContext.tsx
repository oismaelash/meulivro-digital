import React from 'react';
import { clearAccessToken, getAccessToken, setAccessToken } from './token';
import { authApi, AuthTokenResponse, UserViewModel } from '../services/api';

type AuthState = {
  token: string | null;
  user: UserViewModel | null;
  initializing: boolean;
};

type AuthContextValue = AuthState & {
  setSession: (data: AuthTokenResponse) => void;
  logout: () => void;
};

const AuthContext = React.createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = React.useState<AuthState>(() => ({
    token: getAccessToken(),
    user: null,
    initializing: true,
  }));

  React.useEffect(() => {
    let cancelled = false;
    async function init() {
      const token = getAccessToken();
      if (!token) {
        if (!cancelled) setState({ token: null, user: null, initializing: false });
        return;
      }

      try {
        const res = await authApi.me();
        if (!cancelled) setState({ token, user: res.data!, initializing: false });
      } catch {
        clearAccessToken();
        if (!cancelled) setState({ token: null, user: null, initializing: false });
      }
    }
    init();
    return () => {
      cancelled = true;
    };
  }, []);

  const setSession = React.useCallback((data: AuthTokenResponse) => {
    setAccessToken(data.accessToken);
    setState({ token: data.accessToken, user: data.user, initializing: false });
  }, []);

  const logout = React.useCallback(() => {
    clearAccessToken();
    setState({ token: null, user: null, initializing: false });
  }, []);

  const value: AuthContextValue = React.useMemo(
    () => ({ ...state, setSession, logout }),
    [state, setSession, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = React.useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
