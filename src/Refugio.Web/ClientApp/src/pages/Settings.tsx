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
      .catch(() => setErrors(['Error al cargar la configuración.']))
      .finally(() => setLoading(false));
  }, []);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim()) { setErrors(['El nombre del refugio es obligatorio.']); return; }
    setSaving(true);
    setErrors([]);
    try {
      const updated = await settingsApi.update(form.name.trim(), form.phrase.trim() || null);
      setSettings(updated);
      setSuccess(true);
      setTimeout(() => setSuccess(false), 3000);
    } catch {
      setErrors(['Error al guardar la configuración.']);
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
      setErrors(['Error al subir el logotipo. Solo PNG, máximo 5 MB.']);
    }
    e.target.value = '';
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <h2 className="font-headline-xl text-headline-xl text-primary">Configuración</h2>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        {success && (
          <div className="mb-md bg-primary/10 border border-primary/30 rounded-lg px-md py-3 text-body-sm text-primary">
            Configuración guardada correctamente.
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-md">
          <div><label className={LBL}>Nombre del refugio *</label><input value={form.name} onChange={e => set('name', e.target.value)} className={INPUT} placeholder="Haven Sanctuary" /></div>
          <div><label className={LBL}>Lema / Frase</label><input value={form.phrase} onChange={e => set('phrase', e.target.value)} className={INPUT} placeholder="Donde cada cola encuentra un hogar" /></div>
          <div className="flex gap-sm justify-end pt-sm">
            <button type="submit" disabled={saving} className="bg-primary text-on-primary px-lg py-3 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60 flex items-center gap-sm">
              {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
              Guardar configuración
            </button>
          </div>
        </form>
      </div>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
        <h3 className="font-headline-md text-headline-md text-on-surface mb-md">Logotipo del refugio</h3>
        <div className="flex items-center gap-md">
          {settings?.logoUrl ? (
            <img src={settings.logoUrl} alt="logo" className="w-24 h-24 rounded-xl object-cover border border-outline-variant" />
          ) : (
            <div className="w-24 h-24 rounded-xl bg-primary-fixed flex items-center justify-center">
              <span className="material-symbols-outlined text-on-primary-fixed text-4xl" style={{ fontVariationSettings: "'FILL' 1" }}>pets</span>
            </div>
          )}
          <div>
            <p className="text-body-sm text-on-surface-variant mb-sm">Formato PNG, máximo 5 MB. Se redimensionará a 100×100.</p>
            <button onClick={() => logoRef.current?.click()}
              className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all flex items-center gap-sm">
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>upload</span>
              Subir logotipo
            </button>
            <input ref={logoRef} type="file" accept="image/png" hidden onChange={handleLogoUpload} />
          </div>
        </div>
      </div>
    </section>
  );
}
