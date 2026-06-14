import type {
  DogStatus, AdoptionStatus, AdoptionType,
  VolunteerStatus, DonationCategory, ExpenseCategory, FeePaymentMethod,
} from './types';

export const DOG_STATUS_LABELS: Record<DogStatus, string> = {
  Available: 'Disponible',
  Adopted: 'Adoptado',
  Foster: 'En acogida',
  Medical: 'Médico',
  Quarantine: 'Cuarentena',
};

export const ADOPTION_STATUS_LABELS: Record<AdoptionStatus, string> = {
  Applied: 'Solicitada',
  Interview: 'Entrevista',
  HomeCheck: 'Visita domiciliaria',
  Approved: 'Aprobada',
  Finalized: 'Finalizada',
  Rejected: 'Rechazada',
};

export const ADOPTION_TYPE_LABELS: Record<AdoptionType, string> = {
  Adoption: 'Adopción',
  Foster: 'Acogida',
};

export const VOLUNTEER_STATUS_LABELS: Record<VolunteerStatus, string> = {
  Active: 'Activo',
  Inactive: 'Inactivo',
  Pending: 'Pendiente',
};

export const DONATION_CATEGORY_LABELS: Record<DonationCategory, string> = {
  Monthly: 'Mensual',
  OneTime: 'Única vez',
  InKind: 'En especie',
  Corporate: 'Empresa',
};

export const EXPENSE_CATEGORY_LABELS: Record<ExpenseCategory, string> = {
  Medical: 'Médico',
  Food: 'Alimentación',
  Facilities: 'Instalaciones',
  Supplies: 'Suministros',
  Transport: 'Transporte',
  Other: 'Otro',
};

export const FEE_PAYMENT_METHOD_LABELS: Record<FeePaymentMethod, string> = {
  Cash: 'Metálico',
  Bizum: 'Bizum',
  Transfer: 'Transferencia',
};
