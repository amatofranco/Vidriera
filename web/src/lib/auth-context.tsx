"use client";

import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import type { LoginResult } from "./api";

export interface AuthState {
  token: string;
  userId: string;
  companyId: string;
  companyName: string;
  name: string;
  email: string;
  plan: string | null;
  showCode: boolean;
  showPrice: boolean;
  showOrders: boolean;
}

interface AuthContextValue {
  auth: AuthState | null;
  isLoading: boolean;
  setAuth: (result: LoginResult) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);
const STORAGE_KEY = "vidriera.auth";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuthState] = useState<AuthState | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      setAuthState(JSON.parse(stored));
    }
    setIsLoading(false);
  }, []);
  /* eslint-enable react-hooks/set-state-in-effect */

  const setAuth = (result: LoginResult) => {
    const state: AuthState = {
      token: result.token,
      userId: result.userId,
      companyId: result.companyId,
      companyName: result.companyName,
      name: result.name,
      email: result.email,
      plan: result.plan,
      showCode: result.showCode,
      showPrice: result.showPrice,
      showOrders: result.showOrders,
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    setAuthState(state);
  };

  const logout = () => {
    localStorage.removeItem(STORAGE_KEY);
    setAuthState(null);
  };

  return (
    <AuthContext.Provider value={{ auth, isLoading, setAuth, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth debe usarse dentro de <AuthProvider>");
  return ctx;
}
