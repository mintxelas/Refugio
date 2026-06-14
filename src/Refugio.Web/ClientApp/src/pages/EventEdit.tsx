import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { eventsApi } from '../api/events';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { useAuth } from '../auth/AuthContext';

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

function toDateTimeLocal(iso: string | null | undefined) {
  if (!iso) return '';
  return iso.substring(0, 16);
}

export function EventEdit() {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const dateParam = searchParams.get('date');
  const isNew = id === 'new';
  const eventId = isNew ? 0 : parseInt(id!, 10);
  const navigate = useNavigate();
  const { user } = useAuth();
  const isManager = user?.role === 'Manager';

  const [loading, setLoading] = useState(!isNew);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const defaultStart = dateParam ? `${dateParam}T09:00` : new Date().toISOString().substring(0, 16);
  const defaultEnd = dateParam ? `${dateParam}T10:00` : new Date(Date.now() + 3600000).toISOString().substring(0, 16);

  const [form, setForm] = useState({
    title: '', startDateTime: defaultStart, endDateTime: defaultEnd,
    location: '', description: '', eventType: 'General', assignedVolunteers: '',
  });

  useEffect(() => {
    if (isNew) return;
    eventsApi.get(eventId)
      .then(ev => setForm({
        title: ev.title, startDateTime: toDateTimeLocal(ev.startDateTime),
        endDateTime: toDateTimeLocal(ev.endDateTime), location: ev.location ?? '',
        description: ev.description ?? '', eventType: ev.eventType,
        assignedVolunteers: ev.assignedVolunteers?.toString() ?? '',
      }))
      .catch(() => setErrors(['Evento no encontrado.']))
      .finally(() => setLoading(false));
  }, [eventId, isNew]);

  const set = (f: string, v: string) => setForm(prev => ({ ...prev, [f]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.title.trim()) { setErrors(['El título es obligatorio.']); return; }
    setSaving(true);
    try {
      const body = {
        title: form.title.trim(), startDateTime: form.startDateTime,
        endDateTime: form.endDateTime, location: form.location.trim() || null,
        description: form.description.trim() || null, eventType: form.eventType,
        assignedVolunteers: form.assignedVolunteers ? parseInt(form.assignedVolunteers, 10) : null,
      };
      if (isNew) {
        await eventsApi.create(body);
      } else {
        await eventsApi.update(eventId, body);
      }
      navigate('/calendar');
    } catch {
      setErrors(['Error al guardar.']);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('¿Eliminar este evento?')) return;
    await eventsApi.delete(eventId);
    navigate('/calendar');
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop max-w-2xl mx-auto space-y-lg">
      <div className="flex items-center gap-sm">
        <Link to="/calendar" className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary flex-1">{isNew ? 'Nuevo evento' : 'Editar evento'}</h2>
        {!isNew && isManager && (
          <button onClick={handleDelete} className="text-error hover:bg-error-container/20 px-md py-2 rounded-lg text-label-md transition-all flex items-center gap-xs">
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>delete</span>
            Eliminar
          </button>
        )}
      </div>

      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
        {errors.length > 0 && <div className="mb-md space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}
        <form onSubmit={handleSubmit} className="space-y-md">
          <div><label className={LBL}>Título *</label><input value={form.title} onChange={e => set('title', e.target.value)} className={INPUT} /></div>
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Inicio</label><input type="datetime-local" value={form.startDateTime} onChange={e => set('startDateTime', e.target.value)} className={INPUT} /></div>
            <div><label className={LBL}>Fin</label><input type="datetime-local" value={form.endDateTime} onChange={e => set('endDateTime', e.target.value)} className={INPUT} /></div>
          </div>
          <div className="grid grid-cols-2 gap-md">
            <div><label className={LBL}>Tipo</label><input value={form.eventType} onChange={e => set('eventType', e.target.value)} className={INPUT} placeholder="General, Formación, Adopción..." /></div>
            <div><label className={LBL}>Voluntarios necesarios</label><input type="number" min="0" value={form.assignedVolunteers} onChange={e => set('assignedVolunteers', e.target.value)} className={INPUT} /></div>
          </div>
          <div><label className={LBL}>Lugar</label><input value={form.location} onChange={e => set('location', e.target.value)} className={INPUT} /></div>
          <div><label className={LBL}>Descripción</label><textarea value={form.description} onChange={e => set('description', e.target.value)} rows={3} className={INPUT} /></div>
          <div className="flex gap-sm justify-end pt-sm">
            <Link to="/calendar" className="px-md py-3 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancelar</Link>
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
