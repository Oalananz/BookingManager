"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import { authApi } from "@/lib/api/auth";
import { authStorage } from "@/lib/auth/storage";
import { User } from "@/lib/types/auth";

interface AuthContextValue {
  user: User | null;
  /** True until localStorage has been read on the client. */
  hydrated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (fullName: string, email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    setUser(authStorage.getUser());
    setHydrated(true);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await authApi.login(email, password);
    authStorage.save(data.accessToken, data.user);
    setUser(data.user);
  }, []);

  const register = useCallback(async (fullName: string, email: string, password: string) => {
    const { data } = await authApi.register(fullName, email, password);
    authStorage.save(data.accessToken, data.user);
    setUser(data.user);
  }, []);

  const logout = useCallback(() => {
    authApi.logout().catch(() => undefined); // best-effort; JWT logout is client-side
    authStorage.clear();
    setUser(null);
    window.location.href = "/login";
  }, []);

  return (
    <AuthContext.Provider value={{ user, hydrated, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
