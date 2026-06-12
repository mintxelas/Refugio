import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { financeApi } from '../api/finance';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import type { ExpensePhotoDto } from '../types';

const CATEGORIES = ['Medical', 'Food', 'Facilities', 'Supplies', 'Transport', 'Other'];
const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

export function ExpenseEdit() {
  const { id } = useParams<{ id: string }>();
  const isNew = id === 'new';
  const expenseId = isNew ? 0 : parseInt(id!, 10);
  const navigate = useNavigate();
  const photoRef = useRef<HTMLInputElement>(null);

  const [loading, setLoading] = useState(!isNew);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [photos, setPhotos] = useState<ExpensePhotoDto[]>([]);

  const [form, setForm] = useState({
    description: '', amount: '', category: 'Other', notes: '',
  });

  useEffect(() => {
    if (isNew) return;
    Promise.all([
      financeApi.getExpense(expenseId),
      financeApi.getExpensePhotos(expenseId),
    ])
      .then(([e, p]) => {
        setForm({ description: e.description, amount: String(e.amount), category: e.category, notes: e.notes ?? '' });
        setPhotos(p);
      })
      .catch(() => setErrors(['Expense not found.']))
      .finally(() => setLoading(false));
  }, [expenseId, isNew]);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.description.trim()) errs.push('Description is required.');
    if (isNaN(parseFloat(form.amount)) || parseFloat(form.amount) <= 0) errs.push('Amount must be > 0.');
    if (errs.length) { setErrors(errs); return; }
    setSaving(true);
    try {
      const body = { description: form.description.trim(), amount: parseFloat(form.amount), category: form.category, notes: form.notes.trim() || null };
      if (isNew) await financeApi.createExpense(body);
      else await financeApi.updateExpense(expenseId, body);
      navigate('/funds?tab=expenses');
    } catch {
      setErrors(['Failed to save.']);
    } finally {
      setSaving(false);
    }
  };

  const handlePhotoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files?.length) return;
    const fd = new FormData();
    Array.from(files).forEach(f => fd.append('Photos', f));
    try {
      await financeApi.uploadExpensePhotos(expenseId, fd);
      const updated = await financeApi.getExpensePhotos(expenseId);
      setPhotos(updated);
    } catch {
      setErrors(['Failed to upload photos.']);
    }
    e.target.value = '';
  };

  const deletePhoto = async (photoId: number) => {
    if (!confirm('Delete this photo?')) return;
    await financeApi.deleteExpensePhoto(photoId);
    setPhotos(ps => ps.filter(p => p.id !== photoId));
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to="/funds?tab=expenses" className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary">{isNew ? 'Add Expense' : 'Edit Expense'}</h2>
      </div>
      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        <form onSubmit={handleSubmit} className="space-y-md">
          <div><label className={LBL}>Description *</label><input value={form.description} onChange={e => set('description', e.target.value)} className={INPUT} /></div>
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
            <Link to="/funds?tab=expenses" className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancel</Link>
            <button type="submit" disabled={saving} className="bg-primary text-on-primary px-lg py-3 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60 flex items-center gap-sm">
              {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
              Save
            </button>
          </div>
        </form>
      </div>

      {/* Photos */}
      {!isNew && (
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
          <div className="flex items-center justify-between mb-md">
            <h3 className="font-headline-md text-headline-md text-on-surface">Receipts</h3>
            <button onClick={() => photoRef.current?.click()} className="bg-primary text-on-primary px-sm py-1.5 rounded-lg text-label-sm hover:brightness-110 flex items-center gap-xs">
              <span className="material-symbols-outlined" style={{ fontSize: 16 }}>upload</span>Upload
            </button>
            <input ref={photoRef} type="file" accept="image/*" multiple hidden onChange={handlePhotoUpload} />
          </div>
          <div className="flex gap-2 flex-wrap">
            {photos.map(p => (
              <div key={p.id} className="relative group">
                <img src={p.url} alt="" className="w-24 h-24 rounded-lg object-cover" />
                <div className="absolute inset-0 bg-black/40 rounded-lg opacity-0 group-hover:opacity-100 flex items-center justify-center">
                  <button onClick={() => deletePhoto(p.id)} className="w-6 h-6 bg-error rounded flex items-center justify-center text-on-error">
                    <span className="material-symbols-outlined" style={{ fontSize: 14 }}>delete</span>
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </section>
  );
}
