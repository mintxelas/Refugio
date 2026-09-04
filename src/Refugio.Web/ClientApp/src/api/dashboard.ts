import { api } from './client';
import type { DashboardStats, AdoptionConversionStats, ShelterStayStats, UpcomingVisit, UrgentMedicationDto } from '../types';

export const dashboardApi = {
  getStats: () => api.get<DashboardStats>('/api/dashboard'),
  getAdoptionConversion: (year: number) =>
    api.get<AdoptionConversionStats>(`/api/reports/adoption-conversion?year=${year}`),
  getShelterStay: () => api.get<ShelterStayStats>('/api/reports/shelter-stay'),
  getUpcomingVisits: () => api.get<UpcomingVisit[]>('/api/reports/upcoming-visits'),
  getUrgentMedications: () => api.get<UrgentMedicationDto[]>('/api/reports/urgent-medications'),
};
