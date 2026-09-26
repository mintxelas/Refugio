import { api, postAction, qs } from './client';
import type { VolunteerDto, Page, VolunteerStatus, VolunteerCounts } from '../types';

export const volunteersApi = {
  list: (status?: VolunteerStatus) =>
    api.get<VolunteerDto[]>(`/api/volunteers${qs({ status })}`),

  paged: (status?: VolunteerStatus, page = 1, pageSize = 25) =>
    api.get<Page<VolunteerDto>>(`/api/volunteers/paged${qs({ status, page, pageSize })}`),

  get: (id: number) => api.get<VolunteerDto>(`/api/volunteers/${id}`),
  counts: () => api.get<VolunteerCounts>('/api/volunteers/counts'),

  create: (body: {
    name: string; email: string; phone?: string | null; role: string; notes?: string | null;
    canLogin?: boolean; password?: string | null; preferredLanguage?: string | null;
  }) => api.post<VolunteerDto>('/api/volunteers', body),

  update: (id: number, body: {
    name: string; email: string; phone?: string | null; role: string; notes?: string | null;
    canLogin: boolean; status: VolunteerStatus; newPassword?: string | null;
    preferredLanguage?: string | null;
  }) => api.put<VolunteerDto>(`/api/volunteers/${id}`, body),

  delete: (id: number) => api.del(`/api/volunteers/${id}`),

  getDeleted: () => api.get<VolunteerDto[]>('/api/volunteers/deleted'),
  restore: (id: number) => postAction(`/api/volunteers/${id}/restore`),
  purge: (id: number) => postAction(`/api/volunteers/${id}/purge`),
};
