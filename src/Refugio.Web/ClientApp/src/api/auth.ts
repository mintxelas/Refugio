import { api, resetCsrfToken } from './client';
import type { AuthUser } from '../types';

export const authApi = {
  me: () => api.get<AuthUser>('/api/auth/me'),
  login: async (email: string, password: string) => {
    const user = await api.post<AuthUser>('/api/auth/login', { email, password });
    resetCsrfToken(); // identity changed anonymous → authenticated; the cached token is now stale
    return user;
  },
  logout: async () => {
    await api.post<void>('/api/auth/logout');
    resetCsrfToken(); // identity changed authenticated → anonymous
  },
  changePassword: (currentPassword: string, newPassword: string, confirmPassword: string) =>
    api.post<{ success: boolean }>('/api/auth/change-password', {
      currentPassword, newPassword, confirmPassword,
    }),
};
