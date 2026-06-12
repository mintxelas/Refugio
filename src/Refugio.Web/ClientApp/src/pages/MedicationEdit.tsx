import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

function toDateInput(iso: string | null | undefined) {
  if (!iso) return '';
  return iso.substring(0, 10);
}

export function MedicationEdit() {
  const { dogId, id } = useParams<{ dogId: string; id: string }>();
  const medId = parseInt(id!, 10);
  const dId = parseInt(dogId!, 10);
  const navigate = useNavigate();
  const isNew = id === 'new';

  const [loading, setLoading] = useState(!isNew);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const [form, setForm] = useState({
    name: '', dosage: '', frequency: '',
    startDate: new Date().toISOString().substring(0, 10),
    endDate: '', isActive: true,
  });

  useEffect(() => {
    if (isNew) return;
    dogsApi.getMedication(medId)
      .then(m => setForm({
        name: m.name, dosage: m.dosage, frequency: m.frequency,
        startDate: toDateInput(m.startDate), endDate: toDateInput(m.endDate),
        isActive: m.isActive,
      }))
      .catch(() => setErrors(['Medication not found.']))
      .finally(() => setLoading(false));
  }, [medId, isNew]);

  const set = (f: string, v: string | boolean) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.name.trim()) errs.push('Name is required.');
    if (!form.dosage.trim()) errs.push('Dosage is required.');
    if (!form.frequency.trim()) errs.push('Frequency is required.');
    if (errs.length) { setErrors(errs); return; }
    setSaving(true);
    try {
      if (isNew) {
        await dogsApi.createMedication(dId, {
          name: form.name.trim(), dosage: form.dosage.trim(),
          frequency: form.frequency.trim(), startDate: form.startDate,
          endDate: form.endDate || null,
        });
      } else {
        await dogsApi.updateMedication(medId, {
          name: form.name.trim(), dosage: form.dosage.trim(),
          frequency: form.frequency.trim(), startDate: form.startDate,
          endDate: form.endDate || null, isActive: form.isActive,
        });
      }
      navigate(`/dogs/${dId}`);
    } catch {
      setErrors(['Failed to save.']);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to={`/dogs/${dId}`} className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary">{isNew ? 'Add Medication' : 'Edit Medication'}</h2>
      </div>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        <form onSubmit={handleSubmit} className="space-y-md">
          <div><label className={LBL}>Name *</label><input value={form.name} onChange={e => set('name', e.target.value)} className={INPUT} /></div>
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Dosage *</label><input value={form.dosage} onChange={e => set('dosage', e.target.value)} className={INPUT} placeholder="e.g. 10mg" /></div>
            <div><label className={LBL}>Frequency *</label><input value={form.frequency} onChange={e => set('frequency', e.target.value)} className={INPUT} placeholder="e.g. Once daily" /></div>
          </div>
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Start Date</label><input type="date" value={form.startDate} onChange={e => set('startDate', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>End Date</label><input type="date" value={form.endDate} onChange={e => set('endDate', e.target.value)} className={INPUT} /></div>
          </div>
          {!isNew && (
            <label className="flex items-center gap-sm cursor-pointer">
              <input type="checkbox" checked={form.isActive} onChange={e => set('isActive', e.target.checked)}
                className="w-4 h-4 accent-primary" />
              <span className="text-body-md text-on-surface">Active</span>
            </label>
          )}
          <div className="flex gap-sm justify-end pt-sm">
            <Link to={`/dogs/${dId}`} className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancel</Link>
            <button type="submit" disabled={saving} className="bg-primary text-on-primary px-lg py-3 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60 flex items-center gap-sm">
              {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
              Save
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}
