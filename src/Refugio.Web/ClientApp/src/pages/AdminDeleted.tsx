import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { adoptionsApi } from '../api/adoptions';
import { financeApi } from '../api/finance';
import { volunteersApi } from '../api/volunteers';
import { LoadingSpinner } from '../components/LoadingSpinner';
import type {
  DogDto, MedicalRecordDto, MedicationDto, AdoptionDto,
  DonationDto, ExpenseDto, GoalDto, VolunteerDto,
} from '../types';

type Tab = 'dogs' | 'medical' | 'medications' | 'adoptions' | 'donations' | 'expenses' | 'goals' | 'volunteers';

const TABS: { key: Tab; label: string }[] = [
  { key: 'dogs', label: 'Perros' },
  { key: 'medical', label: 'Historiales médicos' },
  { key: 'medications', label: 'Medicamentos' },
  { key: 'adoptions', label: 'Adopciones' },
  { key: 'donations', label: 'Donaciones' },
  { key: 'expenses', label: 'Gastos' },
  { key: 'goals', label: 'Objetivos' },
  { key: 'volunteers', label: 'Voluntarios' },
];

export function AdminDeleted() {
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = (searchParams.get('tab') ?? 'dogs') as Tab;

  const [dogs, setDogs] = useState<DogDto[]>([]);
  const [medical, setMedical] = useState<MedicalRecordDto[]>([]);
  const [medications, setMedications] = useState<MedicationDto[]>([]);
  const [adoptions, setAdoptions] = useState<AdoptionDto[]>([]);
  const [donations, setDonations] = useState<DonationDto[]>([]);
  const [expenses, setExpenses] = useState<ExpenseDto[]>([]);
  const [goals, setGoals] = useState<GoalDto[]>([]);
  const [volunteers, setVolunteers] = useState<VolunteerDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setLoading(true);
    const loaders: Record<Tab, () => Promise<void>> = {
      dogs: () => dogsApi.getDeleted().then(setDogs),
      medical: () => dogsApi.getDeletedMedicalRecords().then(setMedical),
      medications: () => dogsApi.getDeletedMedications().then(setMedications),
      adoptions: () => adoptionsApi.getDeleted().then(setAdoptions),
      donations: () => financeApi.getDeletedDonations().then(setDonations),
      expenses: () => financeApi.getDeletedExpenses().then(setExpenses),
      goals: () => financeApi.getDeletedGoals().then(setGoals),
      volunteers: () => volunteersApi.getDeleted().then(setVolunteers),
    };
    loaders[tab]().finally(() => setLoading(false));
  }, [tab]);

  const setTab = (t: Tab) => {
    const p = new URLSearchParams(searchParams);
    p.set('tab', t);
    setSearchParams(p, { replace: true });
  };

  // Generic action handlers
  const restoreDog = async (id: number) => { await dogsApi.restore(id); setDogs(ds => ds.filter(d => d.id !== id)); };
  const purgeDog = async (id: number) => { if (!confirm('¿Eliminar permanentemente?')) return; await dogsApi.purge(id); setDogs(ds => ds.filter(d => d.id !== id)); };

  const restoreMedical = async (id: number) => { await dogsApi.restoreMedicalRecord(id); setMedical(ms => ms.filter(m => m.id !== id)); };
  const purgeMedical = async (id: number) => { if (!confirm('¿Eliminar permanentemente?')) return; await dogsApi.purgeMedicalRecord(id); setMedical(ms => ms.filter(m => m.id !== id)); };

  const restoreMedication = async (id: number) => { await dogsApi.restoreMedication(id); setMedications(ms => ms.filter(m => m.id !== id)); };
  const purgeMedication = async (id: number) => { if (!confirm('¿Eliminar permanentemente?')) return; await dogsApi.purgeMedication(id); setMedications(ms => ms.filter(m => m.id !== id)); };

  const restoreAdoption = async (id: number) => { await adoptionsApi.restore(id); setAdoptions(as => as.filter(a => a.id !== id)); };
  const purgeAdoption = async (id: number) => { if (!confirm('¿Eliminar permanentemente?')) return; await adoptionsApi.purge(id); setAdoptions(as => as.filter(a => a.id !== id)); };

  const restoreDonation = async (id: number) => { await financeApi.restoreDonation(id); setDonations(ds => ds.filter(d => d.id !== id)); };
  const purgeDonation = async (id: number) => { if (!confirm('¿Eliminar permanentemente?')) return; await financeApi.purgeDonation(id); setDonations(ds => ds.filter(d => d.id !== id)); };

  const restoreExpense = async (id: number) => { await financeApi.restoreExpense(id); setExpenses(es => es.filter(e => e.id !== id)); };
  const purgeExpense = async (id: number) => { if (!confirm('¿Eliminar permanentemente?')) return; await financeApi.purgeExpense(id); setExpenses(es => es.filter(e => e.id !== id)); };

  const restoreGoal = async (id: number) => { await financeApi.restoreGoal(id); setGoals(gs => gs.filter(g => g.id !== id)); };
  const purgeGoal = async (id: number) => { if (!confirm('¿Eliminar permanentemente?')) return; await financeApi.purgeGoal(id); setGoals(gs => gs.filter(g => g.id !== id)); };

  const restoreVolunteer = async (id: number) => { await volunteersApi.restore(id); setVolunteers(vs => vs.filter(v => v.id !== id)); };
  const purgeVolunteer = async (id: number) => { if (!confirm('¿Eliminar permanentemente?')) return; await volunteersApi.purge(id); setVolunteers(vs => vs.filter(v => v.id !== id)); };

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      <div className="flex items-center gap-sm">
        <span className="material-symbols-outlined text-error">admin_panel_settings</span>
        <h2 className="font-headline-xl text-headline-xl text-primary">Admin — Registros eliminados</h2>
      </div>

      {/* Tabs */}
      <div className="flex gap-xs border-b border-outline-variant overflow-x-auto">
        {TABS.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`px-md py-3 font-label-md text-label-md transition-all border-b-2 -mb-px whitespace-nowrap ${tab === t.key ? 'border-primary text-primary' : 'border-transparent text-on-surface-variant hover:text-on-surface'}`}>
            {t.label}
          </button>
        ))}
      </div>

      {loading ? <LoadingSpinner /> : (
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 overflow-hidden">
          {tab === 'dogs' && (
            <DeletedTable
              items={dogs}
              columns={['Nombre', 'Raza', 'Estado', 'Eliminado']}
              renderRow={d => [d.name, d.breed, d.status, new Date(d.deletedAt!).toLocaleDateString()]}
              onRestore={restoreDog}
              onPurge={purgeDog}
            />
          )}
          {tab === 'medical' && (
            <DeletedTable
              items={medical}
              columns={['Diagnóstico', 'Veterinario', 'Visita', 'Eliminado']}
              renderRow={r => [r.diagnosis, r.vetName, new Date(r.visitDate).toLocaleDateString(), new Date(r.deletedAt!).toLocaleDateString()]}
              onRestore={restoreMedical}
              onPurge={purgeMedical}
            />
          )}
          {tab === 'medications' && (
            <DeletedTable
              items={medications}
              columns={['Nombre', 'Dosis', 'Frecuencia', 'Eliminado']}
              renderRow={m => [m.name, m.dosage, m.frequency, new Date(m.deletedAt!).toLocaleDateString()]}
              onRestore={restoreMedication}
              onPurge={purgeMedication}
            />
          )}
          {tab === 'adoptions' && (
            <DeletedTable
              items={adoptions}
              columns={['Solicitante', 'Tipo', 'Estado', 'Eliminado']}
              renderRow={a => [a.applicantName, a.type, a.status, new Date(a.deletedAt!).toLocaleDateString()]}
              onRestore={restoreAdoption}
              onPurge={purgeAdoption}
            />
          )}
          {tab === 'donations' && (
            <DeletedTable
              items={donations}
              columns={['Donante', 'Importe', 'Categoría', 'Eliminado']}
              renderRow={d => [d.donorName, `$${d.amount}`, d.category, new Date(d.deletedAt!).toLocaleDateString()]}
              onRestore={restoreDonation}
              onPurge={purgeDonation}
            />
          )}
          {tab === 'expenses' && (
            <DeletedTable
              items={expenses}
              columns={['Descripción', 'Importe', 'Categoría', 'Eliminado']}
              renderRow={e => [e.description, `$${e.amount}`, e.category, new Date(e.deletedAt!).toLocaleDateString()]}
              onRestore={restoreExpense}
              onPurge={purgeExpense}
            />
          )}
          {tab === 'goals' && (
            <DeletedTable
              items={goals}
              columns={['Título', 'Objetivo', 'Actual', 'Eliminado']}
              renderRow={g => [g.title, `$${g.targetAmount}`, `$${g.currentAmount}`, new Date(g.deletedAt!).toLocaleDateString()]}
              onRestore={restoreGoal}
              onPurge={purgeGoal}
            />
          )}
          {tab === 'volunteers' && (
            <DeletedTable
              items={volunteers}
              columns={['Nombre', 'Rol', 'Email', 'Eliminado']}
              renderRow={v => [v.name, v.role, v.email, new Date(v.deletedAt!).toLocaleDateString()]}
              onRestore={restoreVolunteer}
              onPurge={purgeVolunteer}
            />
          )}
        </div>
      )}
    </section>
  );
}

interface DeletedTableProps<T extends { id: number }> {
  items: T[];
  columns: string[];
  renderRow: (item: T) => string[];
  onRestore: (id: number) => void;
  onPurge: (id: number) => void;
}

function DeletedTable<T extends { id: number }>({
  items, columns, renderRow, onRestore, onPurge,
}: DeletedTableProps<T>) {
  if (items.length === 0) {
    return <p className="p-md text-body-sm text-on-surface-variant text-center">No hay registros eliminados.</p>;
  }

  return (
    <table className="w-full">
      <thead className="bg-surface-container">
        <tr>
          {columns.map(c => (
            <th key={c} className="px-md py-3 text-left text-label-md font-label-md text-on-surface-variant">{c}</th>
          ))}
          <th className="px-md py-3 text-right text-label-md font-label-md text-on-surface-variant">Acciones</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-outline-variant/20">
        {items.map(item => (
          <tr key={item.id} className="hover:bg-surface-container/50 transition-colors">
            {renderRow(item).map((cell, i) => (
              <td key={i} className="px-md py-3 text-body-sm">{cell}</td>
            ))}
            <td className="px-md py-3 text-right">
              <button onClick={() => onRestore(item.id)}
                className="text-primary hover:underline text-label-sm mr-3">
                Restaurar
              </button>
              <button onClick={() => onPurge(item.id)}
                className="text-error hover:underline text-label-sm">
                Purgar
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
