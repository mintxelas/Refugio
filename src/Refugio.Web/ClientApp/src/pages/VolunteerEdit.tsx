import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { volunteersApi } from '../api/volunteers';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { useAuth } from '../auth/AuthContext';
import type { VolunteerStatus } from '../types';

const STATUSES: VolunteerStatus[] = ['Active', 'Inactive', 'Pending'];
const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

export function VolunteerEdit() {
  const { id } = useParams<{ id: string }>();
  const isNew = id === 'new';
  const volunteerId = isNew ? 0 : parseInt(id!, 10);
  const navigate = useNavigate();
  const { user } = useAuth();
  const isManager = user?.role === 'Manager';

  const [loading, setLoading] = useState(!isNew);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const [form, setForm] = useState({
    name: '', email: '', phone: '', role: 'Volunteer', status: 'Active' as VolunteerStatus,
    notes: '', canLogin: false, newPassword: '', preferredLanguage: '',
  });

  useEffect(() => {
    if (isNew) return;
    volunteersApi.get(volunteerId)
      .then(v => setForm({
        name: v.name, email: v.email, phone: v.phone ?? '', role: v.role,
        status: v.status, notes: v.notes ?? '', canLogin: v.canLogin,
        newPassword: '', preferredLanguage: v.preferredLanguage ?? '',
      }))
      .catch(() => setErrors(['Volunteer not found.']))
      .finally(() => setLoading(false));
  }, [volunteerId, isNew]);

  const set = (f: string, v: string | boolean) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.name.trim()) errs.push('Name is required.');
    if (!form.email.trim()) errs.push('Email is required.');
    if (errs.length) { setErrors(errs); return; }
    setSaving(true);
    try {
      if (isNew) {
        await volunteersApi.create({
          name: form.name.trim(), email: form.email.trim(),
          phone: form.phone.trim() || null, role: form.role,
          notes: form.notes.trim() || null, canLogin: form.canLogin,
          password: form.newPassword.trim() || null,
          preferredLanguage: form.preferredLanguage.trim() || null,
        });
      } else {
        await volunteersApi.update(volunteerId, {
          name: form.name.trim(), email: form.email.trim(),
          phone: form.phone.trim() || null, role: form.role,
          notes: form.notes.trim() || null, canLogin: form.canLogin,
          status: form.status,
          newPassword: form.newPassword.trim() || null,
          preferredLanguage: form.preferredLanguage.trim() || null,
        });
      }
      navigate('/volunteers');
    } catch {
      setErrors(['Failed to save.']);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Delete this volunteer?')) return;
    await volunteersApi.delete(volunteerId);
    navigate('/volunteers');
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to="/volunteers" className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary flex-1">{isNew ? 'Add Volunteer' : 'Edit Volunteer'}</h2>
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
            <div><label className={LBL}>Name *</label><input value={form.name} onChange={e => set('name', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Email *</label><input type="email" value={form.email} onChange={e => set('email', e.target.value)} className={INPUT} /></div>
          </div>
          <div className="grid grid-cols-3 gap-md">
            <div><label className={LBL}>Phone</label><input value={form.phone} onChange={e => set('phone', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Role</label>
              <select value={form.role} onChange={e => set('role', e.target.value)} className={INPUT}>
                <option value="Volunteer">Volunteer</option>
                <option value="Manager">Manager</option>
              </select>
            </div>
            {!isNew && (
              <div><label className={LBL}>Status</label>
                <select value={form.status} onChange={e => set('status', e.target.value)} className={INPUT}>
                  {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
            )}
          </div>
          <div><label className={LBL}>Notes</label><textarea value={form.notes} onChange={e => set('notes', e.target.value)} rows={2} className={INPUT} /></div>
          <div><label className={LBL}>Preferred Language</label><input value={form.preferredLanguage} onChange={e => set('preferredLanguage', e.target.value)} className={INPUT} placeholder="en-US, es-ES..." /></div>

          <div className="border-t border-outline-variant/30 pt-md">
            <h4 className="font-label-md text-label-md text-on-surface mb-sm">Login Access</h4>
            <label className="flex items-center gap-sm cursor-pointer mb-sm">
              <input type="checkbox" checked={form.canLogin} onChange={e => set('canLogin', e.target.checked)} className="w-4 h-4 accent-primary" />
              <span className="text-body-md text-on-surface">Can login</span>
            </label>
            {form.canLogin && (
              <div><label className={LBL}>{isNew ? 'Password' : 'New Password (leave blank to keep current)'}</label>
                <input type="password" value={form.newPassword} onChange={e => set('newPassword', e.target.value)} className={INPUT} placeholder="••••••••" />
              </div>
            )}
          </div>

          <div className="flex gap-sm justify-end pt-sm">
            <Link to="/volunteers" className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancel</Link>
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
