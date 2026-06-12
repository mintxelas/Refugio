import { api, upload } from './client';
import type { ShelterSettingsDto } from '../types';

export const settingsApi = {
  get: () => api.get<ShelterSettingsDto>('/api/settings'),

  update: (name: string, phrase?: string | null) =>
    api.put<ShelterSettingsDto>('/api/settings', { name, phrase }),

  uploadLogo: (form: FormData) =>
    upload<{ url: string }>('/api/settings/logo/upload', form),
};
