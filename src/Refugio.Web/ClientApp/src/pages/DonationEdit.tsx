import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { financeApi } from '../api/finance';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import type { DonationCategory } from '../types';

const CATEGORIES: DonationCategory[] = ['Monthly', 'OneTime', 'InKind', 'Corporate'];
const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

export function DonationEdit() {
  const { id } = useParams<{ id: string }>();
  const isNew = id === 'new';
  const donationId = isNew ? 0 : parseInt(id!, 10);
  const navigate = useNavigate();

  const [loading, setLoading] = useState(!isNew);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const [form, setForm] = useState({
    donorName: '', amount: '', category: 'OneTime' as DonationCategory, notes: '',
    date: new Date().toISOString().substring(0, 10),
  });

  useEffect(() => {
    if (isNew) return;
    financeApi.getDonation(donationId)
      .then(d => setForm({
        donorName: d.donorName, amount: String(d.amount), category: d.category,
        notes: d.notes ?? '', date: d.date.substring(0, 10),
      }))
      .catch(() => setErrors(['Donation not found.']))
      .finally(() => setLoading(false));
  }, [donationId, isNew]);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.donorName.trim()) errs.push('Donor name is required.');
    if (isNaN(parseFloat(form.amount)) || parseFloat(form.amount) <= 0) errs.push('Amount must be > 0.');
    if (errs.length) { setErrors(errs); return; }
    setSaving(true);
    try {
      const body = { donorName: form.donorName.trim(), amount: parseFloat(form.amount), category: form.category, notes: form.notes.trim() || null };
      if (isNew) await financeApi.createDonation(body);
      else await financeApi.updateDonation(donationId, body);
      navigate('/funds?tab=donations');
    } catch {
      setErrors(['Failed to save.']);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to="/funds?tab=donations" className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary">{isNew ? 'Add Donation' : 'Edit Donation'}</h2>
      </div>
      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        <form onSubmit={handleSubmit} className="space-y-md">
          <div><label className={LBL}>Donor Name *</label><input value={form.donorName} onChange={e => set('donorName', e.target.value)} className={INPUT} /></div>
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Amount *</label><input type="number" step="0.01" min="0" value={form.amount} onChange={e => set('amount', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Category</label>
              <select value={form.category} onChange={e => set('category', e.target.value)} className={INPUT}>
                {CATEGORIES.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
          </div>
          <div><label className={LBL}>Notes</label><textarea value={form.notes} onChange={e => set('notes', e.target.value)} rows={2} className={INPUT} /></div>
          <div className="flex gap-sm justify-end pt-sm">
            <Link to="/funds?tab=donations" className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancel</Link>
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
