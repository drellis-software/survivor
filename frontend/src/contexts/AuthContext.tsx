import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { api } from '../lib/api';
import type { AuthResponse } from '../types';

interface StoredUser {
  userId: string;
  username: string;
  role: string;
}

interface AuthContextType {
  user: StoredUser | null;
  login: (username: string, pin: string) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

interface RegisterData {
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  pin: string;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StoredUser | null>(() => {
    const stored = localStorage.getItem('user');
    return stored ? JSON.parse(stored) : null;
  });

  const login = useCallback(async (username: string, pin: string) => {
    const { data } = await api.post<AuthResponse>('/api/auth/login', { username, pin });
    localStorage.setItem('token', data.token);
    const stored: StoredUser = { userId: data.userId, username: data.username, role: data.role };
    localStorage.setItem('user', JSON.stringify(stored));
    setUser(stored);
  }, []);

  const register = useCallback(async (data: RegisterData) => {
    const { data: res } = await api.post<AuthResponse>('/api/auth/register', data);
    localStorage.setItem('token', res.token);
    const stored: StoredUser = { userId: res.userId, username: res.username, role: res.role };
    localStorage.setItem('user', JSON.stringify(stored));
    setUser(stored);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, login, register, logout, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
