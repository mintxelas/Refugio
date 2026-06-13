import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { adoptionsApi } from '../api/adoptions';
import { dogsApi } from '../api/dogs';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { useAuth } from '../auth/AuthContext';
import type { AdoptionDto, AdoptionStatus, AdoptionType, DogDto, FeePaymentMethod } from '../types';

const STATUSES: AdoptionStatus[] = ['Applied', 'Interview', 'HomeCheck', 'Approved', 'Finalized', 'Rejected'];
const PAYMENT_METHOD_LABELS: Record<FeePaymentMethod, string> = { Cash: 'Metálico', Bizum: 'Bizum', Transfer: 'Transferencia' };
const PAYMENT_METHODS: FeePaymentMethod[] = ['Cash', 'Bizum', 'Transfer'];
const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

export function AdoptionEdit() {
  const { id } = useParams<{ id: string }>();
  const adoptionId = parseInt(id!, 10);
  const navigate = useNavigate();
  const { user } = useAuth();
  const isManager = user?.role === 'Manager';

  const [adoption, setAdoption] = useState<AdoptionDto | null>(null);
  const [dogs, setDogs] = useState<DogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const [form, setForm] = useState({
    applicantName: '', applicantEmail: '', applicantPhone: '',
    type: 'Adoption' as AdoptionType, status: 'Applied' as AdoptionStatus, notes: '',
    preAdoptionDate: '', adoptionDate: '',
    preAdoptionFeeCharged: false, adoptionFeeCharged: false,
    preAdoptionFeePaymentMethod: null as FeePaymentMethod | null,
    adoptionFeePaymentMethod: null as FeePaymentMethod | null,
  });

  useEffect(() => {
    Promise.all([adoptionsApi.get(adoptionId), dogsApi.list()])
      .then(([a, d]) => {
        setAdoption(a);
        setDogs(d);
        setForm({
          applicantName: a.applicantName, applicantEmail: a.applicantEmail ?? '',
          applicantPhone: a.applicantPhone ?? '', type: a.type,
          status: a.status, notes: a.notes ?? '',
          preAdoptionDate: a.preAdoptionDate ? a.preAdoptionDate.substring(0, 10) : '',
          adoptionDate: a.adoptionDate ? a.adoptionDate.substring(0, 10) : '',
          preAdoptionFeeCharged: a.preAdoptionFeeCharged,
          adoptionFeeCharged: a.adoptionFeeCharged,
          preAdoptionFeePaymentMethod: a.preAdoptionFeePaymentMethod,
          adoptionFeePaymentMethod: a.adoptionFeePaymentMethod,
        });
      })
      .catch(() => setErrors(['Adoption not found.']))
      .finally(() => setLoading(false));
  }, [adoptionId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.applicantName.trim()) { setErrors(['Applicant name is required.']); return; }
    setSaving(true);
    try {
      await adoptionsApi.update(adoptionId, {
        applicantName: form.applicantName.trim(),
        applicantEmail: form.applicantEmail.trim() || null,
        applicantPhone: form.applicantPhone.trim() || null,
        type: form.type, status: form.status,
        notes: form.notes.trim() || null,
        preAdoptionDate: form.preAdoptionDate || null,
        adoptionDate: form.adoptionDate || null,
        preAdoptionFeeCharged: form.preAdoptionFeeCharged,
        adoptionFeeCharged: form.adoptionFeeCharged,
        preAdoptionFeePaymentMethod: form.preAdoptionFeePaymentMethod,
        adoptionFeePaymentMethod: form.adoptionFeePaymentMethod,
      });
      navigate('/adoptions');
    } catch {
      setErrors(['Failed to save.']);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Delete this adoption?')) return;
    await adoptionsApi.delete(adoptionId);
    navigate('/adoptions');
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to="/adoptions" className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary flex-1">Edit Adoption</h2>
        {isManager && (
          <button onClick={handleDelete} className="text-error hover:bg-error-container/20 px-md py-2 rounded-lg text-label-md transition-all flex items-center gap-xs">
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>delete</span>
            Delete
          </button>
        )}
      </div>

      {adoption?.dog && (
        <div className="flex items-center gap-sm bg-surface-container-lowest rounded-xl border border-outline-variant/30 p-md shadow-soft">
          {adoption.dog.photoUrl && <img src={adoption.dog.photoUrl} alt={adoption.dog.name} className="w-12 h-12 rounded-lg object-cover" />}
          <div>
            <p className="font-label-md text-label-md text-on-surface">{adoption.dog.name}</p>
            <p className="text-body-sm text-on-surface-variant">{adoption.dog.breed}</p>
          </div>
        </div>
      )}

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        <form onSubmit={handleSubmit} className="space-y-md">
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Applicant Name *</label><input value={form.applicantName} onChange={e => setForm(f => ({ ...f, applicantName: e.target.value }))} className={INPUT} /></div>
            <div><label className={LBL}>Email</label><input type="email" value={form.applicantEmail} onChange={e => setForm(f => ({ ...f, applicantEmail: e.target.value }))} className={INPUT} /></div>
          </div>
          <div className="grid grid-cols-3 gap-md">
            <div><label className={LBL}>Phone</label><input value={form.applicantPhone} onChange={e => setForm(f => ({ ...f, applicantPhone: e.target.value }))} className={INPUT} /></div>
            <div><label className={LBL}>Type</label>
              <select value={form.type} onChange={e => setForm(f => ({ ...f, type: e.target.value as AdoptionType }))} className={INPUT}>
                <option value="Adoption">Adoption</option>
                <option value="Foster">Foster</option>
              </select>
            </div>
            <div><label className={LBL}>Status</label>
              <select value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value as AdoptionStatus }))} className={INPUT}>
                {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
            </div>
          </div>
          <div><label className={LBL}>Notes</label><textarea value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} rows={3} className={INPUT} /></div>
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Pre-Adoption Date</label><input type="date" value={form.preAdoptionDate} onChange={e => setForm(f => ({ ...f, preAdoptionDate: e.target.value }))} className={INPUT} /></div>
            <div><label className={LBL}>Adoption Date</label><input type="date" value={form.adoptionDate} onChange={e => setForm(f => ({ ...f, adoptionDate: e.target.value }))} className={INPUT} /></div>
          </div>
          <div className="space-y-sm">
            <div className="flex items-center gap-md">
              <label className="flex items-center gap-sm cursor-pointer min-w-[200px]">
                <input type="checkbox" checked={form.preAdoptionFeeCharged} onChange={e => setForm(f => ({ ...f, preAdoptionFeeCharged: e.target.checked }))} className="w-4 h-4 accent-primary" />
                <span className="text-body-md text-on-surface">Pre-Adoption Fee Charged</span>
              </label>
              <select
                value={form.preAdoptionFeePaymentMethod ?? ''}
                onChange={e => setForm(f => ({ ...f, preAdoptionFeePaymentMethod: (e.target.value as FeePaymentMethod) || null }))}
                className={INPUT}
              >
                <option value="">— Forma de pago —</option>
                {PAYMENT_METHODS.map(m => <option key={m} value={m}>{PAYMENT_METHOD_LABELS[m]}</option>)}
              </select>
            </div>
            <div className="flex items-center gap-md">
              <label className="flex items-center gap-sm cursor-pointer min-w-[200px]">
                <input type="checkbox" checked={form.adoptionFeeCharged} onChange={e => setForm(f => ({ ...f, adoptionFeeCharged: e.target.checked }))} className="w-4 h-4 accent-primary" />
                <span className="text-body-md text-on-surface">Adoption Fee Charged</span>
              </label>
              <select
                value={form.adoptionFeePaymentMethod ?? ''}
                onChange={e => setForm(f => ({ ...f, adoptionFeePaymentMethod: (e.target.value as FeePaymentMethod) || null }))}
                className={INPUT}
              >
                <option value="">— Forma de pago —</option>
                {PAYMENT_METHODS.map(m => <option key={m} value={m}>{PAYMENT_METHOD_LABELS[m]}</option>)}
              </select>
            </div>
          </div>
          <div className="flex gap-sm justify-end pt-sm">
            <Link to="/adoptions" className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancel</Link>
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
