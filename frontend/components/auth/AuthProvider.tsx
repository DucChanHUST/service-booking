"use client";

import React, { createContext, useContext, useEffect, useState } from "react";

import type { User } from "@/types/auth";
import { getCurrentUser, getToken, logout as clearAuth } from "@/lib/auth";

interface AuthContextValue {
  user: User | null;
  isLoading: boolean;
  login: (user: User, accessToken: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const token = getToken();
    const currentUser = getCurrentUser();

    if (token && currentUser) {
      setUser(currentUser);
    }

    setIsLoading(false);
  }, []);

  function login(newUser: User, accessToken: string) {
    localStorage.setItem("accessToken", accessToken);
    localStorage.setItem("user", JSON.stringify(newUser));

    setUser(newUser);
  }

  function logout() {
    clearAuth();
    setUser(null);
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider");
  }

  return context;
}
