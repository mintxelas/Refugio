import { api } from './client';
import type { AuthUser } from '../types';

export const authApi = {
  me: () => api.get<AuthUser>('/api/auth/me'),
  login: (email: string, password: string) =>
    api.post<AuthUser>('/api/auth/login', { email, password }),
  logout: () => api.post<void>('/api/auth/logout'),
  changePassword: (currentPassword: string, newPassword: string, confirmPassword: string) =>
    api.post<{ success: boolean }>('/api/auth/change-password', {
      currentPassword, newPassword, confirmPassword,
    }),
};
