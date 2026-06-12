import { api, postAction } from './client';
import type { AdoptionDto, Page, AdoptionStatus, AdoptionType } from '../types';

const qs = (params: Record<string, string | number | undefined | null>) => {
  const parts = Object.entries(params)
    .filter(([, v]) => v !== undefined && v !== null && v !== '')
    .map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`);
  return parts.length ? '?' + parts.join('&') : '';
};

export const adoptionsApi = {
  list: (status?: AdoptionStatus) =>
    api.get<AdoptionDto[]>(`/api/adoptions${qs({ status })}`),

  paged: (status?: AdoptionStatus, page = 1, pageSize = 25) =>
    api.get<Page<AdoptionDto>>(`/api/adoptions/paged${qs({ status, page, pageSize })}`),

  get: (id: number) => api.get<AdoptionDto>(`/api/adoptions/${id}`),

  create: (body: {
    dogId: number; applicantName: string; applicantEmail?: string | null;
    applicantPhone?: string | null; type: AdoptionType; notes?: string | null;
  }) => api.post<AdoptionDto>('/api/adoptions', body),

  update: (id: number, body: {
    applicantName: string; applicantEmail?: string | null; applicantPhone?: string | null;
    type: AdoptionType; status: AdoptionStatus; notes?: string | null;
  }) => api.put<AdoptionDto>(`/api/adoptions/${id}`, body),

  updateStatus: (id: number, newStatus: AdoptionStatus) =>
    api.put<AdoptionDto>(`/api/adoptions/${id}/status`, { id, newStatus, notes: null }),

  delete: (id: number) => api.del(`/api/adoptions/${id}`),

  getDeleted: () => api.get<AdoptionDto[]>('/api/adoptions/deleted'),
  restore: (id: number) => postAction(`/api/adoptions/${id}/restore`),
  purge: (id: number) => postAction(`/api/adoptions/${id}/purge`),
};
