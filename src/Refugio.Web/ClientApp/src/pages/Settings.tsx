import { useEffect, useRef, useState } from 'react';
import { settingsApi } from '../api/settings';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import type { ShelterSettingsDto } from '../types';

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

export function Settings() {
  const [settings, setSettings] = useState<ShelterSettingsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [success, setSuccess] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [form, setForm] = useState({ name: '', phrase: '' });
  const logoRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    settingsApi.get()
      .then(s => { setSettings(s); setForm({ name: s.name, phrase: s.phrase ?? '' }); })
      .catch(() => setErrors(['Failed to load settings.']))
      .finally(() => setLoading(false));
  }, []);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim()) { setErrors(['Shelter name is required.']); return; }
    setSaving(true);
    setErrors([]);
    try {
      const updated = await settingsApi.update(form.name.trim(), form.phrase.trim() || null);
      setSettings(updated);
      setSuccess(true);
      setTimeout(() => setSuccess(false), 3000);
    } catch {
      setErrors(['Failed to save settings.']);
    } finally {
      setSaving(false);
    }
  };

  const handleLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const fd = new FormData();
    fd.append('Logo', file);
    try {
      const result = await settingsApi.uploadLogo(fd);
      setSettings(s => s ? { ...s, logoUrl: result.url } : s);
    } catch {
      setErrors(['Failed to upload logo. PNG files only, max 5MB.']);
    }
    e.target.value = '';
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <h2 className="font-headline-xl text-headline-xl text-primary">Settings</h2>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        {success && (
          <div className="mb-md bg-primary/10 border border-primary/30 rounded-lg px-md py-3 text-body-sm text-primary">
            Settings saved successfully.
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-md">
          <div><label className={LBL}>Shelter Name *</label><input value={form.name} onChange={e => set('name', e.target.value)} className={INPUT} placeholder="Haven Sanctuary" /></div>
          <div><label className={LBL}>Tagline / Phrase</label><input value={form.phrase} onChange={e => set('phrase', e.target.value)} className={INPUT} placeholder="Where every tail finds a home" /></div>
          <div className="flex gap-sm justify-end pt-sm">
            <button type="submit" disabled={saving} className="bg-primary text-on-primary px-lg py-3 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60 flex items-center gap-sm">
              {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
              Save Settings
            </button>
          </div>
        </form>
      </div>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
        <h3 className="font-headline-md text-headline-md text-on-surface mb-md">Shelter Logo</h3>
        <div className="flex items-center gap-md">
          {settings?.logoUrl ? (
            <img src={settings.logoUrl} alt="logo" className="w-24 h-24 rounded-xl object-cover border border-outline-variant" />
          ) : (
            <div className="w-24 h-24 rounded-xl bg-primary-fixed flex items-center justify-center">
              <span className="material-symbols-outlined text-on-primary-fixed text-4xl" style={{ fontVariationSettings: "'FILL' 1" }}>pets</span>
            </div>
          )}
          <div>
            <p className="text-body-sm text-on-surface-variant mb-sm">PNG format, max 5MB. Will be resized to 100×100.</p>
            <button onClick={() => logoRef.current?.click()}
              className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all flex items-center gap-sm">
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>upload</span>
              Upload Logo
            </button>
            <input ref={logoRef} type="file" accept="image/png" hidden onChange={handleLogoUpload} />
          </div>
        </div>
      </div>
    </section>
  );
}
