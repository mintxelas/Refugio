import { api, postAction, upload } from './client';
import type {
  DogDto, MedicalRecordDto, MedicationDto, DogPhotoDto, Page, DogStatus,
} from '../types';

const qs = (params: Record<string, string | number | undefined | null>) => {
  const parts = Object.entries(params)
    .filter(([, v]) => v !== undefined && v !== null && v !== '')
    .map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`);
  return parts.length ? '?' + parts.join('&') : '';
};

export const dogsApi = {
  list: (search?: string, status?: DogStatus) =>
    api.get<DogDto[]>(`/api/dogs${qs({ search, status })}`),

  paged: (search?: string, status?: DogStatus, page = 1, pageSize = 20) =>
    api.get<Page<DogDto>>(`/api/dogs/paged${qs({ search, status, page, pageSize })}`),

  get: (id: number) => api.get<DogDto>(`/api/dogs/${id}`),

  create: (body: {
    name: string; breed: string; ageMonths: number; gender: string;
    weightKg: number; photoUrl?: string | null; traits?: string | null; notes?: string | null;
    arrivalDate: string;
  }) => api.post<DogDto>('/api/dogs', body),

  update: (id: number, body: {
    name: string; breed: string; ageMonths: number; gender: string; status: DogStatus;
    weightKg: number; photoUrl?: string | null; traits?: string | null; notes?: string | null;
    arrivalDate: string;
  }) => api.put<DogDto>(`/api/dogs/${id}`, body),

  delete: (id: number) => api.del(`/api/dogs/${id}`),

  // Photos
  getPhotos: (dogId: number) => api.get<DogPhotoDto[]>(`/api/dogs/${dogId}/photos`),
  uploadPhotos: (dogId: number, form: FormData) =>
    upload<{ urls: string[] }>(`/api/dogs/${dogId}/photos/upload`, form),
  setDefaultPhoto: (photoId: number) =>
    postAction(`/api/dogs/photos/${photoId}/default`),
  deletePhoto: (photoId: number) =>
    postAction(`/api/dogs/photos/${photoId}/delete`),

  // Medical records
  getMedicalRecords: (dogId: number) =>
    api.get<MedicalRecordDto[]>(`/api/dogs/${dogId}/medical`),
  getMedicalRecord: (id: number) => api.get<MedicalRecordDto>(`/api/medical/${id}`),
  createMedicalRecord: (dogId: number, body: {
    vetName: string; diagnosis: string; treatment: string; notes?: string | null; nextVisitDate?: string | null;
  }) => api.post<MedicalRecordDto>(`/api/dogs/${dogId}/medical`, body),
  updateMedicalRecord: (id: number, body: {
    vetName: string; diagnosis: string; treatment: string; notes?: string | null;
    visitDate: string; nextVisitDate?: string | null;
  }) => api.put<MedicalRecordDto>(`/api/medical/${id}`, body),
  deleteMedicalRecord: (id: number) => api.del(`/api/medical/${id}`),

  // Medications
  getMedications: (dogId: number) =>
    api.get<MedicationDto[]>(`/api/dogs/${dogId}/medications`),
  getMedication: (id: number) => api.get<MedicationDto>(`/api/medications/${id}`),
  createMedication: (dogId: number, body: {
    name: string; dosage: string; frequency: string; startDate: string; endDate?: string | null;
  }) => api.post<MedicationDto>(`/api/dogs/${dogId}/medications`, body),
  updateMedication: (id: number, body: {
    name: string; dosage: string; frequency: string;
    startDate: string; endDate?: string | null; isActive: boolean;
  }) => api.put<MedicationDto>(`/api/medications/${id}`, body),

  // Deleted (admin)
  getDeleted: () => api.get<DogDto[]>('/api/dogs/deleted'),
  restore: (id: number) => postAction(`/api/dogs/${id}/restore`),
  purge: (id: number) => postAction(`/api/dogs/${id}/purge`),

  getDeletedMedicalRecords: () => api.get<MedicalRecordDto[]>('/api/medical/deleted'),
  restoreMedicalRecord: (id: number) => postAction(`/api/medical/${id}/restore`),
  purgeMedicalRecord: (id: number) => postAction(`/api/medical/${id}/purge`),

  getDeletedMedications: () => api.get<MedicationDto[]>('/api/medications/deleted'),
  restoreMedication: (id: number) => postAction(`/api/medications/${id}/restore`),
  purgeMedication: (id: number) => postAction(`/api/medications/${id}/purge`),
};
