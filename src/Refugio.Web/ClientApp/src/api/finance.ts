import { api, postAction, upload } from './client';
import type {
  DonationDto, ExpenseDto, ExpensePhotoDto, GoalDto, FinanceSummary, Page,
  DonationCategory,
} from '../types';

export const financeApi = {
  // Donations
  getDonations: () => api.get<DonationDto[]>('/api/donations'),
  getDonationsPaged: (page = 1, pageSize = 25) =>
    api.get<Page<DonationDto>>(`/api/donations/paged?page=${page}&pageSize=${pageSize}`),
  getDonation: (id: number) => api.get<DonationDto>(`/api/donations/${id}`),
  createDonation: (body: {
    donorName: string; amount: number; category: DonationCategory; notes?: string | null; taxId?: string | null;
  }) => api.post<DonationDto>('/api/donations', body),
  updateDonation: (id: number, body: {
    donorName: string; amount: number; category: DonationCategory; notes?: string | null; taxId?: string | null;
  }) => api.put<DonationDto>(`/api/donations/${id}`, body),
  deleteDonation: (id: number) => api.del(`/api/donations/${id}`),

  // Expenses
  getExpenses: () => api.get<ExpenseDto[]>('/api/expenses'),
  getExpensesPaged: (page = 1, pageSize = 25) =>
    api.get<Page<ExpenseDto>>(`/api/expenses/paged?page=${page}&pageSize=${pageSize}`),
  getExpense: (id: number) => api.get<ExpenseDto>(`/api/expenses/${id}`),
  createExpense: (body: {
    description: string; amount: number; category: string; notes?: string | null;
  }) => api.post<ExpenseDto>('/api/expenses', body),
  updateExpense: (id: number, body: {
    description: string; amount: number; category: string; notes?: string | null;
  }) => api.put<ExpenseDto>(`/api/expenses/${id}`, body),
  deleteExpense: (id: number) => api.del(`/api/expenses/${id}`),

  // Expense photos
  getExpensePhotos: (expenseId: number) =>
    api.get<ExpensePhotoDto[]>(`/api/expenses/${expenseId}/photos`),
  uploadExpensePhotos: (expenseId: number, form: FormData) =>
    upload<{ urls: string[] }>(`/api/expenses/${expenseId}/photos/upload`, form),
  // expenseId only needed for Blazor redirect; operation uses photoId only
  deleteExpensePhoto: (photoId: number) =>
    postAction(`/api/expenses/photos/${photoId}/delete`),

  // Goals
  getGoals: () => api.get<GoalDto[]>('/api/goals'),
  getGoal: (id: number) => api.get<GoalDto>(`/api/goals/${id}`),
  createGoal: (body: {
    title: string; description?: string | null; targetAmount: number;
    currentAmount: number; deadline?: string | null;
  }) => api.post<GoalDto>('/api/goals', body),
  updateGoal: (id: number, body: {
    title: string; description?: string | null; targetAmount: number;
    currentAmount: number; deadline?: string | null;
  }) => api.put<GoalDto>(`/api/goals/${id}`, body),
  deleteGoal: (id: number) => api.del(`/api/goals/${id}`),

  // Summary
  getSummary: (year: number) =>
    api.get<FinanceSummary>(`/api/finances/summary?year=${year}`),

  // Deleted (admin)
  getDeletedDonations: () => api.get<DonationDto[]>('/api/donations/deleted'),
  restoreDonation: (id: number) => postAction(`/api/donations/${id}/restore`),
  purgeDonation: (id: number) => postAction(`/api/donations/${id}/purge`),

  getDeletedExpenses: () => api.get<ExpenseDto[]>('/api/expenses/deleted'),
  restoreExpense: (id: number) => postAction(`/api/expenses/${id}/restore`),
  purgeExpense: (id: number) => postAction(`/api/expenses/${id}/purge`),

  getDeletedGoals: () => api.get<GoalDto[]>('/api/goals/deleted'),
  restoreGoal: (id: number) => postAction(`/api/goals/${id}/restore`),
  purgeGoal: (id: number) => postAction(`/api/goals/${id}/purge`),
};
