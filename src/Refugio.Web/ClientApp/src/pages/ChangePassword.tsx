import { useState } from 'react';
import { authApi } from '../api/auth';
import { ApiError } from '../api/client';
import { ErrorMessage } from '../components/ErrorMessage';

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

export function ChangePassword() {
  const [form, setForm] = useState({ current: '', newPw: '', confirm: '' });
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.current.trim()) errs.push('La contraseña actual es obligatoria.');
    if (form.newPw.length < 6) errs.push('La nueva contraseña debe tener al menos 6 caracteres.');
    if (form.newPw !== form.confirm) errs.push('Las contraseñas no coinciden.');
    if (errs.length) { setErrors(errs); return; }
    setErrors([]);
    setSaving(true);
    try {
      await authApi.changePassword(form.current, form.newPw, form.confirm);
      setSuccess(true);
      setForm({ current: '', newPw: '', confirm: '' });
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) {
        setErrors(['La contraseña actual es incorrecta.']);
      } else {
        setErrors(['Error al cambiar la contraseña.']);
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="p-margin-desktop max-w-md mx-auto space-y-lg">
      <h2 className="font-headline-xl text-headline-xl text-primary">Cambiar contraseña</h2>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        {success && (
          <div className="mb-md bg-primary/10 border border-primary/30 rounded-lg px-md py-3 text-body-sm text-primary">
            Contraseña cambiada correctamente.
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-md">
          <div><label className={LBL}>Contraseña actual</label><input type="password" value={form.current} onChange={e => set('current', e.target.value)} className={INPUT} required /></div>
          <div><label className={LBL}>Nueva contraseña</label><input type="password" value={form.newPw} onChange={e => set('newPw', e.target.value)} className={INPUT} required /></div>
          <div><label className={LBL}>Confirmar nueva contraseña</label><input type="password" value={form.confirm} onChange={e => set('confirm', e.target.value)} className={INPUT} required /></div>
          <button type="submit" disabled={saving} className="w-full bg-primary text-on-primary py-3 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60 flex items-center justify-center gap-sm">
            {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
            Actualizar contraseña
          </button>
        </form>
      </div>
    </section>
  );
}
