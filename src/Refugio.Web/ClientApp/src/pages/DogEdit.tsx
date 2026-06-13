import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { useAuth } from '../auth/AuthContext';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import type { DogDto, DogPhotoDto, DogStatus } from '../types';

const DOG_STATUSES: DogStatus[] = ['Available', 'Adopted', 'Foster', 'Medical', 'Quarantine'];
const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';

export function DogEdit() {
  const { id } = useParams<{ id: string }>();
  const dogId = parseInt(id!, 10);
  const navigate = useNavigate();
  const { user } = useAuth();
  const isManager = user?.role === 'Manager';
  const photoInputRef = useRef<HTMLInputElement>(null);

  const [dog, setDog] = useState<DogDto | null>(null);
  const [photos, setPhotos] = useState<DogPhotoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const today = new Date().toISOString().slice(0, 10);

  const [form, setForm] = useState({
    name: '', breed: '', ageMonths: '', gender: 'Male',
    status: 'Available' as DogStatus, weightKg: '', traits: '', notes: '', arrivalDate: today,
  });

  useEffect(() => {
    Promise.all([dogsApi.get(dogId), dogsApi.getPhotos(dogId)])
      .then(([d, p]) => {
        setDog(d);
        setPhotos(p);
        setForm({
          name: d.name, breed: d.breed, ageMonths: String(d.ageMonths),
          gender: d.gender, status: d.status, weightKg: String(d.weightKg),
          traits: d.traits ?? '', notes: d.notes ?? '',
          arrivalDate: d.arrivalDate.slice(0, 10),
        });
      })
      .catch(() => setErrors(['Failed to load dog.']))
      .finally(() => setLoading(false));
  }, [dogId]);

  const set = (field: string, value: string) => setForm(f => ({ ...f, [field]: value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: string[] = [];
    if (!form.name.trim()) errs.push('Name is required.');
    if (!form.breed.trim()) errs.push('Breed is required.');
    if (isNaN(parseInt(form.ageMonths, 10))) errs.push('Age must be a number.');
    if (isNaN(parseFloat(form.weightKg)) || parseFloat(form.weightKg) <= 0) errs.push('Weight must be > 0.');
    if (!form.arrivalDate) errs.push('Date of Entry is required.');
    if (errs.length) { setErrors(errs); return; }
    setErrors([]);
    setSaving(true);
    try {
      await dogsApi.update(dogId, {
        name: form.name.trim(), breed: form.breed.trim(),
        ageMonths: parseInt(form.ageMonths, 10), gender: form.gender,
        status: form.status, weightKg: parseFloat(form.weightKg),
        traits: form.traits.trim() || null, notes: form.notes.trim() || null,
        arrivalDate: form.arrivalDate,
      });
      navigate(`/dogs/${dogId}`);
    } catch {
      setErrors(['Failed to save changes.']);
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
      await dogsApi.uploadPhotos(dogId, fd);
      const updated = await dogsApi.getPhotos(dogId);
      setPhotos(updated);
    } catch {
      setErrors(['Failed to upload photos.']);
    }
    e.target.value = '';
  };

  const setDefault = async (photoId: number) => {
    await dogsApi.setDefaultPhoto(photoId);
    setPhotos(ps => ps.map(p => ({ ...p, isDefault: p.id === photoId })));
  };

  const deletePhoto = async (photoId: number) => {
    if (!confirm('Delete this photo?')) return;
    await dogsApi.deletePhoto(photoId);
    setPhotos(ps => ps.filter(p => p.id !== photoId));
  };

  const handleDelete = async () => {
    if (!confirm(`Delete ${dog?.name}? This can be undone from Admin.`)) return;
    await dogsApi.delete(dogId);
    navigate('/dogs');
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to={`/dogs/${dogId}`} className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary flex-1">Edit {dog?.name}</h2>
        {isManager && (
          <button onClick={handleDelete} className="text-error hover:bg-error-container/20 px-md py-2 rounded-lg text-label-md transition-all flex items-center gap-xs">
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>delete</span>
            Delete
          </button>
        )}
      </div>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && (
          <div className="mb-md space-y-1">
            {errors.map((e, i) => <ErrorMessage key={i} message={e} />)}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-md">
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Name *</label><input value={form.name} onChange={e => set('name', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Breed *</label><input value={form.breed} onChange={e => set('breed', e.target.value)} className={INPUT} /></div>
          </div>
          <div className="grid grid-cols-4 gap-md">
            <div><label className={LBL}>Age (mo) *</label><input type="number" min="0" value={form.ageMonths} onChange={e => set('ageMonths', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Gender</label><select value={form.gender} onChange={e => set('gender', e.target.value)} className={INPUT}><option>Male</option><option>Female</option></select></div>
            <div><label className={LBL}>Weight (kg)</label><input type="number" step="0.1" min="0" value={form.weightKg} onChange={e => set('weightKg', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Status</label>
              <select value={form.status} onChange={e => set('status', e.target.value)} className={INPUT}>
                {DOG_STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
            </div>
          </div>
          <div><label className={LBL}>Date of Entry *</label><input type="date" value={form.arrivalDate} onChange={e => set('arrivalDate', e.target.value)} className={INPUT} /></div>
          <div><label className={LBL}>Traits</label><input value={form.traits} onChange={e => set('traits', e.target.value)} className={INPUT} /></div>
          <div><label className={LBL}>Notes</label><textarea value={form.notes} onChange={e => set('notes', e.target.value)} rows={3} className={INPUT} /></div>

          <div className="flex gap-sm justify-end pt-sm">
            <Link to={`/dogs/${dogId}`} className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancel</Link>
            <button type="submit" disabled={saving} className="bg-primary text-on-primary px-lg py-3 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all disabled:opacity-60 flex items-center gap-sm">
              {saving && <span className="w-4 h-4 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />}
              Save Changes
            </button>
          </div>
        </form>
      </div>

      {/* Photo Gallery */}
      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
        <div className="flex items-center justify-between mb-md">
          <h3 className="font-headline-md text-headline-md text-on-surface">Photo Gallery</h3>
          <button onClick={() => photoInputRef.current?.click()}
            className="bg-primary text-on-primary px-sm py-1.5 rounded-lg text-label-sm hover:brightness-110 transition-all flex items-center gap-xs">
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>upload</span>
            Upload
          </button>
          <input ref={photoInputRef} type="file" accept="image/*" multiple hidden onChange={handlePhotoUpload} />
        </div>
        {photos.length === 0 ? (
          <p className="text-body-sm text-on-surface-variant text-center py-md">No photos yet.</p>
        ) : (
          <div className="flex gap-2 flex-wrap">
            {photos.map(p => (
              <div key={p.id} className="relative group">
                <img src={p.url} alt="" className={`w-24 h-24 rounded-lg object-cover ${p.isDefault ? 'ring-2 ring-primary' : ''}`} />
                <div className="absolute inset-0 bg-black/40 rounded-lg opacity-0 group-hover:opacity-100 transition-all flex items-center justify-center gap-1">
                  {!p.isDefault && (
                    <button onClick={() => setDefault(p.id)} title="Set default"
                      className="w-6 h-6 bg-primary rounded flex items-center justify-center text-on-primary">
                      <span className="material-symbols-outlined" style={{ fontSize: 14 }}>star</span>
                    </button>
                  )}
                  <button onClick={() => deletePhoto(p.id)} title="Delete"
                    className="w-6 h-6 bg-error rounded flex items-center justify-center text-on-error">
                    <span className="material-symbols-outlined" style={{ fontSize: 14 }}>delete</span>
                  </button>
                </div>
                {p.isDefault && (
                  <span className="absolute top-1 right-1 w-4 h-4 bg-primary rounded-full flex items-center justify-center">
                    <span className="material-symbols-outlined text-on-primary" style={{ fontSize: 10 }}>star</span>
                  </span>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

const LBL = 'block text-label-md font-label-md text-on-surface mb-1';
