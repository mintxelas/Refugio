import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { useAuth } from '../auth/AuthContext';

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

function toDateInput(iso: string | null | undefined) {
  if (!iso) return '';
  return iso.substring(0, 10);
}

export function MedicalRecordEdit() {
  const { dogId, id } = useParams<{ dogId: string; id: string }>();
  const recId = parseInt(id!, 10);
  const dId = parseInt(dogId!, 10);
  const navigate = useNavigate();
  const { user } = useAuth();
  const isManager = user?.role === 'Manager';
  const isNew = id === 'new';

  const [loading, setLoading] = useState(!isNew);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const [form, setForm] = useState({
    vetName: '', diagnosis: '', treatment: '', notes: '',
    visitDate: new Date().toISOString().substring(0, 10), nextVisitDate: '',
  });

  useEffect(() => {
    if (isNew) return;
    dogsApi.getMedicalRecord(recId)
      .then(r => {
        setForm({
          vetName: r.vetName, diagnosis: r.diagnosis, treatment: r.treatment,
          notes: r.notes ?? '', visitDate: toDateInput(r.visitDate),
          nextVisitDate: toDateInput(r.nextVisitDate),
        });
      })
      .catch(() => setErrors(['Record not found.']))
      .finally(() => setLoading(false));
  }, [recId, isNew]);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.vetName.trim()) errs.push('Vet name is required.');
    if (!form.diagnosis.trim()) errs.push('Diagnosis is required.');
    if (!form.treatment.trim()) errs.push('Treatment is required.');
    if (errs.length) { setErrors(errs); return; }
    setSaving(true);
    try {
      if (isNew) {
        await dogsApi.createMedicalRecord(dId, {
          vetName: form.vetName.trim(), diagnosis: form.diagnosis.trim(),
          treatment: form.treatment.trim(), notes: form.notes.trim() || null,
          nextVisitDate: form.nextVisitDate || null,
        });
      } else {
        await dogsApi.updateMedicalRecord(recId, {
          vetName: form.vetName.trim(), diagnosis: form.diagnosis.trim(),
          treatment: form.treatment.trim(), notes: form.notes.trim() || null,
          visitDate: form.visitDate, nextVisitDate: form.nextVisitDate || null,
        });
      }
      navigate(`/dogs/${dId}`);
    } catch {
      setErrors(['Failed to save.']);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Delete this record?')) return;
    await dogsApi.deleteMedicalRecord(recId);
    navigate(`/dogs/${dId}`);
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to={`/dogs/${dId}`} className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary flex-1">
          {isNew ? 'Add Medical Record' : 'Edit Medical Record'}
        </h2>
        {!isNew && isManager && (
          <button onClick={handleDelete} className="text-error hover:bg-error-container/20 px-md py-2 rounded-lg text-label-md transition-all flex items-center gap-xs">
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>delete</span>
            Delete
          </button>
        )}
      </div>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        <form onSubmit={handleSubmit} className="space-y-md">
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Visit Date</label><input type="date" value={form.visitDate} onChange={e => set('visitDate', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Next Visit Date</label><input type="date" value={form.nextVisitDate} onChange={e => set('nextVisitDate', e.target.value)} className={INPUT} /></div>
          </div>
          <div><label className={LBL}>Vet Name *</label><input value={form.vetName} onChange={e => set('vetName', e.target.value)} className={INPUT} /></div>
          <div><label className={LBL}>Diagnosis *</label><input value={form.diagnosis} onChange={e => set('diagnosis', e.target.value)} className={INPUT} /></div>
          <div><label className={LBL}>Treatment *</label><textarea value={form.treatment} onChange={e => set('treatment', e.target.value)} rows={3} className={INPUT} /></div>
          <div><label className={LBL}>Notes</label><textarea value={form.notes} onChange={e => set('notes', e.target.value)} rows={2} className={INPUT} /></div>
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
