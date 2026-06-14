import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { financeApi } from '../api/finance';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

export function GoalEdit() {
  const { id } = useParams<{ id: string }>();
  const isNew = id === 'new';
  const goalId = isNew ? 0 : parseInt(id!, 10);
  const navigate = useNavigate();

  const [loading, setLoading] = useState(!isNew);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const [form, setForm] = useState({
    title: '', description: '', targetAmount: '', currentAmount: '0', deadline: '',
  });

  useEffect(() => {
    if (isNew) return;
    financeApi.getGoal(goalId)
      .then(g => setForm({
        title: g.title, description: g.description ?? '', targetAmount: String(g.targetAmount),
        currentAmount: String(g.currentAmount), deadline: g.deadline ? g.deadline.substring(0, 10) : '',
      }))
      .catch(() => setErrors(['Objetivo no encontrado.']))
      .finally(() => setLoading(false));
  }, [goalId, isNew]);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.title.trim()) errs.push('El título es obligatorio.');
    if (isNaN(parseFloat(form.targetAmount)) || parseFloat(form.targetAmount) <= 0) errs.push('El importe objetivo debe ser mayor que 0.');
    if (errs.length) { setErrors(errs); return; }
    setSaving(true);
    try {
      const body = {
        title: form.title.trim(), description: form.description.trim() || null,
        targetAmount: parseFloat(form.targetAmount), currentAmount: parseFloat(form.currentAmount) || 0,
        deadline: form.deadline || null,
      };
      if (isNew) await financeApi.createGoal(body);
      else await financeApi.updateGoal(goalId, body);
      navigate('/funds?tab=goals');
    } catch {
      setErrors(['Error al guardar.']);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to="/funds?tab=goals" className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary">{isNew ? 'Añadir objetivo' : 'Editar objetivo'}</h2>
      </div>
      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        <form onSubmit={handleSubmit} className="space-y-md">
          <div><label className={LBL}>Título *</label><input value={form.title} onChange={e => set('title', e.target.value)} className={INPUT} /></div>
          <div><label className={LBL}>Descripción</label><textarea value={form.description} onChange={e => set('description', e.target.value)} rows={2} className={INPUT} /></div>
          <div className="grid grid-cols-3 gap-md">
            <div><label className={LBL}>Importe objetivo *</label><input type="number" step="0.01" min="0" value={form.targetAmount} onChange={e => set('targetAmount', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Importe actual</label><input type="number" step="0.01" min="0" value={form.currentAmount} onChange={e => set('currentAmount', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Fecha límite</label><input type="date" value={form.deadline} onChange={e => set('deadline', e.target.value)} className={INPUT} /></div>
          </div>
          <div className="flex gap-sm justify-end pt-sm">
            <Link to="/funds?tab=goals" className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancelar</Link>
            <button type="submit" disabled={saving} className="bg-primary text-on-primary px-lg py-3 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60 flex items-center gap-sm">
              {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
              Guardar
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}
