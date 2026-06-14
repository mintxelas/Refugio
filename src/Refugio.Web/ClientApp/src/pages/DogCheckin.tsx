import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { ErrorMessage } from '../components/ErrorMessage';

export function DogCheckin() {
  const navigate = useNavigate();
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const today = new Date().toISOString().slice(0, 10);

  const [form, setForm] = useState({
    name: '', breed: '', ageMonths: '', gender: 'Male',
    weightKg: '', traits: '', notes: '', arrivalDate: today,
  });

  const set = (field: string, value: string) =>
    setForm(f => ({ ...f, [field]: value }));

  const validate = () => {
    const e: string[] = [];
    if (!form.name.trim()) e.push('El nombre es obligatorio.');
    if (!form.breed.trim()) e.push('La raza es obligatoria.');
    const age = parseInt(form.ageMonths, 10);
    if (isNaN(age) || age < 0) e.push('La edad debe ser un número no negativo.');
    const wt = parseFloat(form.weightKg);
    if (isNaN(wt) || wt <= 0) e.push('El peso debe ser mayor que 0.');
    if (!form.arrivalDate) e.push('La fecha de entrada es obligatoria.');
    return e;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs = validate();
    if (errs.length) { setErrors(errs); return; }
    setErrors([]);
    setSaving(true);
    try {
      const dog = await dogsApi.create({
        name: form.name.trim(),
        breed: form.breed.trim(),
        ageMonths: parseInt(form.ageMonths, 10),
        gender: form.gender,
        weightKg: parseFloat(form.weightKg),
        traits: form.traits.trim() || null,
        notes: form.notes.trim() || null,
        arrivalDate: form.arrivalDate,
      });
      navigate(`/dogs/${dog.id}`);
    } catch {
      setErrors(['Error al registrar el perro. Inténtalo de nuevo.']);
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to="/dogs" className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary">Registrar Perro</h2>
      </div>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && (
          <div className="mb-md space-y-1">
            {errors.map((e, i) => <ErrorMessage key={i} message={e} />)}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-md">
          <div className="grid grid-cols-2 gap-md">
            <Field label="Nombre *" required>
              <input value={form.name} onChange={e => set('name', e.target.value)}
                className={INPUT} placeholder="Nombre del perro" />
            </Field>
            <Field label="Raza *" required>
              <input value={form.breed} onChange={e => set('breed', e.target.value)}
                className={INPUT} placeholder="Raza" />
            </Field>
          </div>

          <div className="grid grid-cols-3 gap-md">
            <Field label="Edad (meses) *">
              <input type="number" min="0" value={form.ageMonths} onChange={e => set('ageMonths', e.target.value)}
                className={INPUT} placeholder="0" />
            </Field>
            <Field label="Sexo *">
              <select value={form.gender} onChange={e => set('gender', e.target.value)} className={INPUT}>
                <option value="Male">Macho</option>
                <option value="Female">Hembra</option>
              </select>
            </Field>
            <Field label="Peso (kg) *">
              <input type="number" step="0.1" min="0" value={form.weightKg} onChange={e => set('weightKg', e.target.value)}
                className={INPUT} placeholder="0.0" />
            </Field>
          </div>

          <Field label="Fecha de entrada *">
            <input type="date" value={form.arrivalDate} onChange={e => set('arrivalDate', e.target.value)}
              className={INPUT} max={today} />
          </Field>

          <Field label="Características">
            <input value={form.traits} onChange={e => set('traits', e.target.value)}
              className={INPUT} placeholder="Sociable, bueno con niños..." />
          </Field>

          <Field label="Notas">
            <textarea value={form.notes} onChange={e => set('notes', e.target.value)}
              rows={3} className={INPUT} placeholder="Notas adicionales..." />
          </Field>

          <div className="flex gap-sm justify-end">
            <Link to="/dogs" className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">
              Cancelar
            </Link>
            <button
              type="submit"
              disabled={saving}
              className="bg-primary text-on-primary px-lg py-3 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all disabled:opacity-60 flex items-center gap-sm"
            >
              {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
              Registrar
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';

function Field({ label, children, required }: { label: string; children: React.ReactNode; required?: boolean }) {
  return (
    <div>
      <label className="block text-label-md font-label-md text-on-surface mb-1">
        {label}{required && <span className="text-error ml-0.5">*</span>}
      </label>
      {children}
    </div>
  );
}
