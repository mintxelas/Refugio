// TypeScript mirrors of the backend DTO contracts.
// Enum string values must match C# member names exactly (stored as strings by the API).

export type DogStatus = 'Available' | 'Adopted' | 'Foster' | 'Medical' | 'Quarantine';
export type AdoptionType = 'Adoption' | 'Foster';
export type AdoptionStatus = 'Applied' | 'Interview' | 'HomeCheck' | 'Approved' | 'Finalized' | 'Rejected';
export type FeePaymentMethod = 'Cash' | 'Bizum' | 'Transfer';
export type VolunteerStatus = 'Active' | 'Inactive' | 'Pending';
export type DonationCategory = 'Monthly' | 'OneTime' | 'InKind' | 'Corporate';
export type ExpenseCategory = 'Medical' | 'Food' | 'Facilities' | 'Supplies' | 'Transport' | 'Other';

export interface Page<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface DogDto {
  id: number;
  name: string;
  breed: string;
  ageMonths: number;
  gender: string;
  status: DogStatus;
  photoUrl: string | null;
  traits: string | null;
  notes: string | null;
  arrivalDate: string;
  weightKg: number;
  deletedAt: string | null;
  medicalRecords?: MedicalRecordDto[];
  medications?: MedicationDto[];
  photos?: DogPhotoDto[];
}

export interface MedicalRecordDto {
  id: number;
  dogId: number;
  visitDate: string;
  vetName: string;
  diagnosis: string;
  treatment: string;
  notes: string | null;
  nextVisitDate: string | null;
  deletedAt: string | null;
  dog?: DogDto;
}

export interface MedicationDto {
  id: number;
  dogId: number;
  name: string;
  dosage: string;
  frequency: string;
  startDate: string;
  endDate: string | null;
  isActive: boolean;
  deletedAt: string | null;
  dog?: DogDto;
}

export interface DogPhotoDto {
  id: number;
  dogId: number;
  url: string;
  isDefault: boolean;
  uploadedAt: string;
}

export interface AdoptionDto {
  id: number;
  dogId: number;
  applicantName: string;
  applicantEmail: string | null;
  applicantPhone: string | null;
  type: AdoptionType;
  status: AdoptionStatus;
  notes: string | null;
  preAdoptionDate: string | null;
  adoptionDate: string | null;
  preAdoptionFeeCharged: boolean;
  adoptionFeeCharged: boolean;
  preAdoptionFeePaymentMethod: FeePaymentMethod | null;
  adoptionFeePaymentMethod: FeePaymentMethod | null;
  createdAt: string;
  updatedAt: string | null;
  deletedAt: string | null;
  dog?: DogDto;
}

export interface DonationDto {
  id: number;
  donorName: string;
  amount: number;
  date: string;
  category: DonationCategory;
  notes: string | null;
  deletedAt: string | null;
  taxId: string | null;
}

export interface ExpenseTaxLineDto {
  id: number;
  ivaPercent: number;
  base: number;
  importe: number;
}

export interface ExpenseDto {
  id: number;
  description: string;
  amount: number;
  date: string;
  category: string;
  notes: string | null;
  deletedAt: string | null;
  photos?: ExpensePhotoDto[];
  taxLines?: ExpenseTaxLineDto[];
}

export interface ExpensePhotoDto {
  id: number;
  expenseId: number;
  url: string;
  uploadedAt: string;
}

export interface GoalDto {
  id: number;
  title: string;
  description: string | null;
  targetAmount: number;
  currentAmount: number;
  deadline: string | null;
  createdAt: string;
  deletedAt: string | null;
}

export interface VolunteerDto {
  id: number;
  name: string;
  email: string;
  phone: string | null;
  role: string;
  status: VolunteerStatus;
  joinDate: string;
  notes: string | null;
  canLogin: boolean;
  preferredLanguage: string | null;
  photoUrl: string | null;
  deletedAt: string | null;
}

export interface ShelterTaskDto {
  id: number;
  title: string;
  notes: string | null;
  dueDateTime: string;
  isCompleted: boolean;
  location: string | null;
  assignedVolunteerId: number | null;
  assignedVolunteer?: VolunteerDto;
}

export interface ShelterEventDto {
  id: number;
  title: string;
  startDateTime: string;
  endDateTime: string;
  location: string | null;
  description: string | null;
  eventType: string;
  assignedVolunteers: number | null;
}

export interface ShelterSettingsDto {
  id: number;
  name: string;
  phrase: string | null;
  logoUrl: string | null;
}

// Stats / read-model shapes
export interface DashboardStats {
  totalDogs: number;
  newAdoptions: number;
  urgentMeds: number;
  totalDonations: number;
  donationGoal: number;
}

export interface MonthSummary {
  month: number;
  income: number;
  expenses: number;
}

export interface FinanceSummary {
  totalIncome: number;
  totalExpenses: number;
  monthly: MonthSummary[];
}

export interface MonthlyConversionData {
  month: number;
  applied: number;
  finalized: number;
}

export interface AdoptionConversionStats {
  monthly: MonthlyConversionData[];
  totalApplied: number;
  totalFinalized: number;
}

export interface BreedStayData {
  breed: string;
  avgDays: number;
  count: number;
}

export interface ShelterStayStats {
  byBreed: BreedStayData[];
}

export interface VolunteerCounts {
  total: number;
  active: number;
  pending: number;
}

export interface UpcomingVisit {
  dogName: string;
  nextVisitDate: string;
  vetName: string;
  diagnosis: string;
}

// Auth
export interface AuthUser {
  id: number;
  name: string;
  email: string;
  role: 'Manager' | 'Volunteer';
}
