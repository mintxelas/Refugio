import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { eventsApi } from '../api/events';
import { LoadingSpinner } from '../components/LoadingSpinner';
import type { ShelterEventDto } from '../types';

function startOfWeek(d: Date): Date {
  const day = d.getDay(); // 0=Sun
  const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Monday
  const m = new Date(d);
  m.setDate(diff);
  m.setHours(0, 0, 0, 0);
  return m;
}

function addDays(d: Date, n: number): Date {
  const r = new Date(d);
  r.setDate(r.getDate() + n);
  return r;
}

function toIsoDate(d: Date): string {
  return d.toISOString().substring(0, 10);
}

const HOURS = Array.from({ length: 14 }, (_, i) => i + 8); // 8am–9pm

export function Calendar() {
  const [searchParams, setSearchParams] = useSearchParams();
  const weekParam = searchParams.get('week');
  const weekStart = weekParam ? new Date(weekParam + 'T00:00:00') : startOfWeek(new Date());
  const weekDays = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));

  const [events, setEvents] = useState<ShelterEventDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const from = weekStart.toISOString();
    const to = addDays(weekStart, 7).toISOString();
    setLoading(true);
    eventsApi.list(from, to)
      .then(setEvents)
      .finally(() => setLoading(false));
  }, [weekParam]);

  const navigate = (dir: number) => {
    const next = addDays(weekStart, dir * 7);
    const p = new URLSearchParams(searchParams);
    p.set('week', toIsoDate(next));
    setSearchParams(p, { replace: true });
  };

  const eventsOnDay = (day: Date) =>
    events.filter(e => {
      const s = new Date(e.startDateTime);
      return s.toDateString() === day.toDateString();
    });

  return (
    <section className="p-margin-desktop space-y-lg">
      <div className="flex items-center justify-between">
        <h2 className="font-headline-xl text-headline-xl text-primary">Calendar</h2>
        <div className="flex items-center gap-sm">
          <button onClick={() => navigate(-1)} className="p-2 rounded-lg hover:bg-surface-container transition-all">
            <span className="material-symbols-outlined">chevron_left</span>
          </button>
          <span className="font-label-md text-label-md text-on-surface">
            {weekStart.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })}
          </span>
          <button onClick={() => navigate(1)} className="p-2 rounded-lg hover:bg-surface-container transition-all">
            <span className="material-symbols-outlined">chevron_right</span>
          </button>
          <Link to="/calendar/events/new"
            className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all flex items-center gap-sm ml-md">
            <span className="material-symbols-outlined">add</span>
            New Event
          </Link>
        </div>
      </div>

      {loading ? <LoadingSpinner /> : (
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 overflow-hidden">
          {/* Day headers */}
          <div className="grid grid-cols-7 border-b border-outline-variant">
            {weekDays.map((d, i) => {
              const isToday = d.toDateString() === new Date().toDateString();
              return (
                <div key={i} className={`p-sm text-center border-r border-outline-variant last:border-r-0 ${isToday ? 'bg-primary/5' : ''}`}>
                  <p className="text-label-sm text-on-surface-variant">
                    {d.toLocaleDateString('en-US', { weekday: 'short' })}
                  </p>
                  <p className={`font-label-md text-label-md ${isToday ? 'text-primary' : 'text-on-surface'}`}>
                    {d.getDate()}
                  </p>
                </div>
              );
            })}
          </div>

          {/* Event grid */}
          <div className="grid grid-cols-7 min-h-[400px]">
            {weekDays.map((d, i) => {
              const dayEvents = eventsOnDay(d);
              const isToday = d.toDateString() === new Date().toDateString();
              return (
                <div key={i} className={`p-xs border-r border-outline-variant last:border-r-0 min-h-[200px] ${isToday ? 'bg-primary/5' : ''}`}>
                  {dayEvents.map(ev => (
                    <Link
                      key={ev.id}
                      to={`/calendar/events/${ev.id}`}
                      className="block mb-1 p-xs rounded text-label-sm bg-primary/10 text-primary hover:bg-primary/20 transition-all truncate"
                    >
                      {new Date(ev.startDateTime).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })} {ev.title}
                    </Link>
                  ))}
                  {dayEvents.length === 0 && (
                    <Link
                      to={`/calendar/events/new?date=${toIsoDate(d)}`}
                      className="block w-full h-full opacity-0 hover:opacity-100 transition-all text-center py-4"
                    >
                      <span className="material-symbols-outlined text-primary" style={{ fontSize: 20 }}>add</span>
                    </Link>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </section>
  );
}
