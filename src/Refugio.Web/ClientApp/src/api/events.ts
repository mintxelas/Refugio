import { api } from './client';
import type { ShelterEventDto } from '../types';

export const eventsApi = {
  list: (from?: string, to?: string) => {
    const params = new URLSearchParams();
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    const q = params.toString();
    return api.get<ShelterEventDto[]>(`/api/events${q ? '?' + q : ''}`);
  },

  get: (id: number) => api.get<ShelterEventDto>(`/api/events/${id}`),

  create: (body: {
    title: string; startDateTime: string; endDateTime: string;
    location?: string | null; description?: string | null;
    eventType: string; assignedVolunteers?: number | null;
  }) => api.post<ShelterEventDto>('/api/events', body),

  update: (id: number, body: {
    title: string; startDateTime: string; endDateTime: string;
    location?: string | null; description?: string | null;
    eventType: string; assignedVolunteers?: number | null;
  }) => api.put<ShelterEventDto>(`/api/events/${id}`, body),

  delete: (id: number) => api.del(`/api/events/${id}`),
};
