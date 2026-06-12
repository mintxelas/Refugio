import { api } from './client';
import type { ShelterTaskDto } from '../types';

export const tasksApi = {
  list: (includeCompleted = false) =>
    api.get<ShelterTaskDto[]>(`/api/tasks?includeCompleted=${includeCompleted}`),

  create: (body: {
    title: string; dueDateTime: string; notes?: string | null;
    assignedVolunteerId?: number | null; location?: string | null;
  }) => api.post<ShelterTaskDto>('/api/tasks', body),

  complete: (id: number) => api.put<void>(`/api/tasks/${id}/complete`),
  delete: (id: number) => api.del(`/api/tasks/${id}`),
};
