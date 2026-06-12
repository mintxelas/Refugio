import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { dashboardApi } from '../api/dashboard';
import { tasksApi } from '../api/tasks';
import { dogsApi } from '../api/dogs';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import type { DashboardStats, ShelterTaskDto, DogDto } from '../types';

export function Home() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [tasks, setTasks] = useState<ShelterTaskDto[]>([]);
  const [dogs, setDogs] = useState<DogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.all([
      dashboardApi.getStats(),
      tasksApi.list(false),
      dogsApi.list(undefined, 'Available'),
    ])
      .then(([s, t, d]) => {
        setStats(s);
        setTasks(t.slice(0, 5));
        setDogs(d.slice(0, 6));
      })
      .catch(() => setError('Failed to load dashboard data.'))
      .finally(() => setLoading(false));
  }, []);

  const completeTask = async (id: number) => {
    await tasksApi.complete(id);
    setTasks(ts => ts.filter(t => t.id !== id));
  };

  const donationPct = stats && stats.donationGoal > 0
    ? Math.min(100, Math.round((stats.totalDonations / stats.donationGoal) * 100))
    : 0;

  if (loading) return <LoadingSpinner />;
  if (error) return <div className="p-margin-desktop"><ErrorMessage message={error} /></div>;

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      {/* Header */}
      <div className="flex justify-between items-end">
        <div>
          <h2 className="font-headline-xl text-headline-xl text-primary mb-xs">Good morning!</h2>
          <p className="text-body-lg text-on-surface-variant">Here's what's happening at the shelter today.</p>
        </div>
        <div className="text-right">
          <div className="font-label-md text-label-md text-secondary">
            {new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
          </div>
          <div className="text-body-sm text-on-surface-variant">Managing {stats?.totalDogs} dogs</div>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-gutter">
        <KpiCard
          label="Total Dogs"
          value={stats?.totalDogs ?? 0}
          sub="In care now"
          icon="pets"
          color="primary"
          href="/dogs"
        />
        <KpiCard
          label="New Adoptions"
          value={stats?.newAdoptions ?? 0}
          sub="This week"
          icon="favorite"
          color="secondary"
          href="/adoptions"
        />
        <KpiCard
          label="Urgent Meds"
          value={stats?.urgentMeds ?? 0}
          sub="Need attention"
          icon="medication"
          color="error"
          href="/health"
        />
        <div className="p-md bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30">
          <p className="text-label-md font-label-md text-on-surface-variant mb-xs">Donation Goal</p>
          <h3 className="font-headline-lg text-headline-lg text-tertiary">
            ${stats?.totalDonations.toLocaleString() ?? 0}
          </h3>
          <div className="mt-2 h-2 bg-surface-container rounded-full overflow-hidden">
            <div className="h-full bg-tertiary rounded-full transition-all" style={{ width: `${donationPct}%` }} />
          </div>
          <p className="text-label-sm text-on-surface-variant mt-1">
            {donationPct}% of ${stats?.donationGoal.toLocaleString() ?? 0}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-gutter">
        {/* Upcoming Tasks */}
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
          <div className="flex items-center justify-between mb-md">
            <h3 className="font-headline-md text-headline-md text-on-surface">Upcoming Tasks</h3>
            <span className="text-label-sm text-on-surface-variant">{tasks.length} pending</span>
          </div>
          {tasks.length === 0 ? (
            <p className="text-body-sm text-on-surface-variant text-center py-md">No pending tasks.</p>
          ) : (
            <ul className="space-y-2">
              {tasks.map(task => (
                <li key={task.id} className="flex items-center gap-sm p-sm rounded-lg hover:bg-surface-container transition-all">
                  <button
                    onClick={() => completeTask(task.id)}
                    className="w-5 h-5 rounded border-2 border-outline-variant hover:border-primary flex items-center justify-center transition-all flex-shrink-0"
                    title="Mark complete"
                  >
                    <span className="material-symbols-outlined text-on-surface-variant" style={{ fontSize: 14 }}>check</span>
                  </button>
                  <div className="flex-1 min-w-0">
                    <p className="text-body-sm text-on-surface truncate">{task.title}</p>
                    <p className="text-label-sm text-on-surface-variant">
                      {new Date(task.dueDateTime).toLocaleDateString()}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Dogs Needing Attention */}
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
          <div className="flex items-center justify-between mb-md">
            <h3 className="font-headline-md text-headline-md text-on-surface">Recent Dogs</h3>
            <Link to="/dogs" className="text-label-sm text-primary hover:underline">View all</Link>
          </div>
          {dogs.length === 0 ? (
            <p className="text-body-sm text-on-surface-variant text-center py-md">No dogs found.</p>
          ) : (
            <div className="grid grid-cols-2 gap-2">
              {dogs.map(dog => (
                <Link
                  key={dog.id}
                  to={`/dogs/${dog.id}`}
                  className="flex items-center gap-sm p-sm rounded-lg hover:bg-surface-container transition-all"
                >
                  {dog.photoUrl ? (
                    <img src={dog.photoUrl} alt={dog.name} className="w-10 h-10 rounded-lg object-cover flex-shrink-0" />
                  ) : (
                    <div className="w-10 h-10 rounded-lg bg-primary-fixed flex items-center justify-center flex-shrink-0">
                      <span className="material-symbols-outlined text-on-primary-fixed" style={{ fontSize: 18 }}>pets</span>
                    </div>
                  )}
                  <div className="min-w-0">
                    <p className="text-label-md font-label-md text-on-surface truncate">{dog.name}</p>
                    <p className="text-label-sm text-on-surface-variant truncate">{dog.breed}</p>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

interface KpiCardProps {
  label: string; value: number; sub: string; icon: string;
  color: 'primary' | 'secondary' | 'error'; href: string;
}

function KpiCard({ label, value, sub, icon, color, href }: KpiCardProps) {
  const textColor = `text-${color}`;
  const bgColor = color === 'primary' ? 'bg-primary-fixed' : color === 'secondary' ? 'bg-secondary-fixed' : 'bg-error-container';
  const iconColor = color === 'primary' ? 'text-on-primary-fixed' : color === 'secondary' ? 'text-on-secondary-fixed' : 'text-on-error-container';

  return (
    <div className="p-md bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 flex items-start justify-between">
      <div>
        <p className="text-label-md font-label-md text-on-surface-variant mb-xs">{label}</p>
        <h3 className={`font-headline-lg text-headline-lg ${textColor}`}>{value}</h3>
        <p className={`text-label-sm ${textColor} flex items-center gap-xs mt-2`}>
          <span className="material-symbols-outlined text-base">{icon}</span>
          {sub}
        </p>
      </div>
      <Link to={href} className={`w-12 h-12 rounded-lg ${bgColor} flex items-center justify-center ${iconColor} hover:brightness-90 transition-all`}>
        <span className="material-symbols-outlined">{icon}</span>
      </Link>
    </div>
  );
}
