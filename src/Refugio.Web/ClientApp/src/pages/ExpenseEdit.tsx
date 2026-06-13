import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { financeApi } from '../api/finance';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import type { ExpensePhotoDto } from '../types';

const CATEGORIES = ['Medical', 'Food', 'Facilities', 'Supplies', 'Transport', 'Other'];
const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';
const SMALL_INPUT = 'w-full px-2 py-1.5 bg-surface-container border border-outline-variant rounded-md text-body-sm focus:ring-1 focus:ring-primary focus:border-primary transition-all';

type TaxRow = { ivaPercent: string; base: string; importe: string };

const emptyRow = (): TaxRow => ({ ivaPercent: '', base: '', importe: '' });

function validateRow(row: TaxRow): string | null {
  const iva = parseFloat(row.ivaPercent);
  const base = parseFloat(row.base);
  const imp = parseFloat(row.importe);
  if (isNaN(iva) || isNaN(base) || isNaN(imp)) return 'All fields required.';
  if (base <= 0) return 'Base must be > 0.';
  const expected = Math.round(iva / 100 * base * 100) / 100;
  if (expected !== imp) return `Importe must equal ${iva}% × Base = ${expected.toFixed(2)}.`;
  return null;
}

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
  const [taxLines, setTaxLines] = useState<TaxRow[]>([emptyRow()]);
  const [rowErrors, setRowErrors] = useState<(string | null)[]>([null]);

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
        if (e.taxLines?.length) {
          setTaxLines(e.taxLines.map(t => ({
            ivaPercent: String(t.ivaPercent),
            base: String(t.base),
            importe: String(t.importe),
          })));
          setRowErrors(e.taxLines.map(() => null));
        }
      })
      .catch(() => setErrors(['Expense not found.']))
      .finally(() => setLoading(false));
  }, [expenseId, isNew]);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const setRow = (i: number, field: keyof TaxRow, value: string) => {
    setTaxLines(prev => prev.map((r, idx) => idx === i ? { ...r, [field]: value } : r));
  };

  const addRow = () => {
    if (taxLines.length >= 5) return;
    setTaxLines(prev => [...prev, emptyRow()]);
    setRowErrors(prev => [...prev, null]);
  };

  const removeRow = (i: number) => {
    if (taxLines.length <= 1) return;
    setTaxLines(prev => prev.filter((_, idx) => idx !== i));
    setRowErrors(prev => prev.filter((_, idx) => idx !== i));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.description.trim()) errs.push('Description is required.');
    if (isNaN(parseFloat(form.amount)) || parseFloat(form.amount) <= 0) errs.push('Amount must be > 0.');

    const newRowErrors = taxLines.map(validateRow);
    setRowErrors(newRowErrors);
    if (newRowErrors.some(e => e !== null)) errs.push('Fix tax line errors before saving.');

    if (errs.length) { setErrors(errs); return; }
    setSaving(true);
    try {
      const body = {
        description: form.description.trim(),
        amount: parseFloat(form.amount),
        category: form.category,
        notes: form.notes.trim() || null,
        taxLines: taxLines.map(r => ({
          ivaPercent: parseFloat(r.ivaPercent),
          base: parseFloat(r.base),
          importe: parseFloat(r.importe),
        })),
      };
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

          {/* Tax lines */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <label className={LBL + ' mb-0'}>IVA Breakdown *</label>
              <button
                type="button"
                onClick={addRow}
                disabled={taxLines.length >= 5}
                className="text-label-sm text-primary hover:underline disabled:text-on-surface-variant disabled:no-underline disabled:cursor-not-allowed"
              >
                + Add line
              </button>
            </div>
            <div className="rounded-lg border border-outline-variant overflow-hidden">
              <table className="w-full text-body-sm">
                <thead className="bg-surface-container text-on-surface-variant">
                  <tr>
                    <th className="px-3 py-2 text-left font-label-sm">IVA %</th>
                    <th className="px-3 py-2 text-left font-label-sm">Base</th>
                    <th className="px-3 py-2 text-left font-label-sm">Importe</th>
                    <th className="w-8" />
                  </tr>
                </thead>
                <tbody>
                  {taxLines.map((row, i) => (
                    <tr key={i} className="border-t border-outline-variant/50">
                      <td className="px-2 py-1.5">
                        <input
                          type="number" step="0.01" min="0"
                          value={row.ivaPercent}
                          onChange={e => setRow(i, 'ivaPercent', e.target.value)}
                          className={SMALL_INPUT}
                          placeholder="21"
                        />
                      </td>
                      <td className="px-2 py-1.5">
                        <input
                          type="number" step="0.01" min="0.01"
                          value={row.base}
                          onChange={e => setRow(i, 'base', e.target.value)}
                          className={SMALL_INPUT}
                          placeholder="0.00"
                        />
                      </td>
                      <td className="px-2 py-1.5">
                        <input
                          type="number" step="0.01" min="0"
                          value={row.importe}
                          onChange={e => setRow(i, 'importe', e.target.value)}
                          className={SMALL_INPUT}
                          placeholder="0.00"
                        />
                      </td>
                      <td className="px-2 py-1.5 text-center">
                        <button
                          type="button"
                          onClick={() => removeRow(i)}
                          disabled={taxLines.length <= 1}
                          className="text-error hover:opacity-70 disabled:text-on-surface-variant/40 disabled:cursor-not-allowed"
                          aria-label="Remove line"
                        >
                          <span className="material-symbols-outlined" style={{ fontSize: 16 }}>close</span>
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {rowErrors.map((err, i) => err && (
              <p key={i} className="text-error text-body-sm mt-1">Row {i + 1}: {err}</p>
            ))}
          </div>

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
