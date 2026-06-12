import type { DogStatus, AdoptionStatus, VolunteerStatus } from '../types';

const dogColors: Record<DogStatus, string> = {
  Available: 'bg-primary/10 text-primary',
  Adopted: 'bg-secondary/10 text-secondary',
  Foster: 'bg-tertiary/10 text-tertiary',
  Medical: 'bg-error/10 text-error',
  Quarantine: 'bg-outline/10 text-on-surface-variant',
};

const adoptionColors: Record<AdoptionStatus, string> = {
  Applied: 'bg-primary/10 text-primary',
  Interview: 'bg-secondary/10 text-secondary',
  HomeCheck: 'bg-tertiary/10 text-tertiary',
  Approved: 'bg-primary/20 text-primary',
  Finalized: 'bg-secondary/20 text-secondary',
  Rejected: 'bg-error/10 text-error',
};

const volunteerColors: Record<VolunteerStatus, string> = {
  Active: 'bg-primary/10 text-primary',
  Inactive: 'bg-outline/10 text-on-surface-variant',
  Pending: 'bg-secondary/10 text-secondary',
};

interface Props {
  status: DogStatus | AdoptionStatus | VolunteerStatus;
  type: 'dog' | 'adoption' | 'volunteer';
}

export function StatusChip({ status, type }: Props) {
  const color =
    type === 'dog' ? dogColors[status as DogStatus] :
    type === 'adoption' ? adoptionColors[status as AdoptionStatus] :
    volunteerColors[status as VolunteerStatus];

  return (
    <span className={`inline-flex items-center px-sm py-xs rounded-full text-label-sm font-label-sm ${color}`}>
      {status}
    </span>
  );
}
