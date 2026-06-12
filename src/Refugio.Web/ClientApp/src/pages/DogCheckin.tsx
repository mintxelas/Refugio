import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { ErrorMessage } from '../components/ErrorMessage';

export function DogCheckin() {
  const navigate = useNavigate();
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const [form, setForm] = useState({
    name: '', breed: '', ageMonths: '', gender: 'Male',
    weightKg: '', traits: '', notes: '',
  });

  const set = (field: string, value: string) =>
    setForm(f => ({ ...f, [field]: value }));

  const validate = () => {
    const e: string[] = [];
    if (!form.name.trim()) e.push('Name is required.');
    if (!form.breed.trim()) e.push('Breed is required.');
    const age = parseInt(form.ageMonths, 10);
    if (isNaN(age) || age < 0) e.push('Age must be a non-negative number.');
    const wt = parseFloat(form.weightKg);
    if (isNaN(wt) || wt <= 0) e.push('Weight must be greater than 0.');
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
      });
      navigate(`/dogs/${dog.id}`);
    } catch {
      setErrors(['Failed to check in dog. Please try again.']);
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
        <h2 className="font-headline-xl text-headline-xl text-primary">Check In Dog</h2>
      </div>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && (
          <div className="mb-md space-y-1">
            {errors.map((e, i) => <ErrorMessage key={i} message={e} />)}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-md">
          <div className="grid grid-cols-2 gap-md">
            <Field label="Name *" required>
              <input value={form.name} onChange={e => set('name', e.target.value)}
                className={INPUT} placeholder="Dog's name" />
            </Field>
            <Field label="Breed *" required>
              <input value={form.breed} onChange={e => set('breed', e.target.value)}
                className={INPUT} placeholder="Breed" />
            </Field>
          </div>

          <div className="grid grid-cols-3 gap-md">
            <Field label="Age (months) *">
              <input type="number" min="0" value={form.ageMonths} onChange={e => set('ageMonths', e.target.value)}
                className={INPUT} placeholder="0" />
            </Field>
            <Field label="Gender *">
              <select value={form.gender} onChange={e => set('gender', e.target.value)} className={INPUT}>
                <option>Male</option>
                <option>Female</option>
              </select>
            </Field>
            <Field label="Weight (kg) *">
              <input type="number" step="0.1" min="0" value={form.weightKg} onChange={e => set('weightKg', e.target.value)}
                className={INPUT} placeholder="0.0" />
            </Field>
          </div>

          <Field label="Traits">
            <input value={form.traits} onChange={e => set('traits', e.target.value)}
              className={INPUT} placeholder="Friendly, good with kids..." />
          </Field>

          <Field label="Notes">
            <textarea value={form.notes} onChange={e => set('notes', e.target.value)}
              rows={3} className={INPUT} placeholder="Any additional notes..." />
          </Field>

          <div className="flex gap-sm justify-end">
            <Link to="/dogs" className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">
              Cancel
            </Link>
            <button
              type="submit"
              disabled={saving}
              className="bg-primary text-on-primary px-lg py-3 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all disabled:opacity-60 flex items-center gap-sm"
            >
              {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
              Check In
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
