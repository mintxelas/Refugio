import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { volunteersApi } from '../api/volunteers';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { Pagination } from '../components/Pagination';
import { StatusChip } from '../components/StatusChip';
import type { Page, VolunteerCounts, VolunteerDto, VolunteerStatus } from '../types';

const STATUSES: VolunteerStatus[] = ['Active', 'Inactive', 'Pending'];

export function Volunteers() {
  const [searchParams, setSearchParams] = useSearchParams();
  const status = (searchParams.get('status') ?? '') as VolunteerStatus | '';
  const page = parseInt(searchParams.get('page') ?? '1', 10);

  const [result, setResult] = useState<Page<VolunteerDto> | null>(null);
  const [counts, setCounts] = useState<VolunteerCounts | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    setLoading(true);
    Promise.all([
      volunteersApi.paged((status as VolunteerStatus) || undefined, page),
      volunteersApi.counts(),
    ])
      .then(([r, c]) => { setResult(r); setCounts(c); })
      .catch(() => setError('Failed to load volunteers.'))
      .finally(() => setLoading(false));
  }, [status, page]);

  const setStatus = (s: string) => {
    const p = new URLSearchParams(searchParams);
    if (s) p.set('status', s); else p.delete('status');
    p.set('page', '1');
    setSearchParams(p, { replace: true });
  };

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="font-headline-xl text-headline-xl text-primary">Volunteers</h2>
        <Link to="/volunteers/new" className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 flex items-center gap-sm">
          <span className="material-symbols-outlined">person_add</span>
          Add Volunteer
        </Link>
      </div>

      {/* Count cards */}
      {counts && (
        <div className="grid grid-cols-3 gap-gutter">
          {[
            { label: 'Total', value: counts.total, color: 'text-primary' },
            { label: 'Active', value: counts.active, color: 'text-primary' },
            { label: 'Pending', value: counts.pending, color: 'text-secondary' },
          ].map(c => (
            <div key={c.label} className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md text-center">
              <p className="text-label-md font-label-md text-on-surface-variant">{c.label}</p>
              <p className={`font-headline-lg text-headline-lg ${c.color}`}>{c.value}</p>
            </div>
          ))}
        </div>
      )}

      {/* Status filter */}
      <div className="flex gap-xs">
        <button onClick={() => setStatus('')}
          className={`px-sm py-1.5 rounded-full text-label-sm border transition-all ${!status ? 'bg-primary text-on-primary border-primary' : 'border-outline-variant hover:bg-surface-container'}`}>
          All
        </button>
        {STATUSES.map(s => (
          <button key={s} onClick={() => setStatus(s)}
            className={`px-sm py-1.5 rounded-full text-label-sm border transition-all ${status === s ? 'bg-primary text-on-primary border-primary' : 'border-outline-variant hover:bg-surface-container'}`}>
            {s}
          </button>
        ))}
      </div>

      {loading ? <LoadingSpinner /> : error ? <ErrorMessage message={error} /> : (
        <>
          <div className="overflow-hidden rounded-xl border border-outline-variant/30">
            <table className="w-full">
              <thead className="bg-surface-container">
                <tr>
                  {['', 'Name', 'Role', 'Status', 'Email', 'Phone', 'Joined', ''].map((h, i) => (
                    <th key={i} className="px-md py-3 text-left text-label-md font-label-md text-on-surface-variant">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-outline-variant/20">
                {result?.items.map(v => (
                  <tr key={v.id} className="hover:bg-surface-container/50 transition-colors">
                    <td className="px-md py-3">
                      {v.photoUrl ? (
                        <img src={v.photoUrl} alt={v.name} className="w-9 h-9 rounded-full object-cover" />
                      ) : (
                        <div className="w-9 h-9 rounded-full bg-primary-fixed flex items-center justify-center text-on-primary-fixed text-sm font-bold">
                          {v.name.charAt(0).toUpperCase()}
                        </div>
                      )}
                    </td>
                    <td className="px-md py-3 text-body-sm font-medium">{v.name}</td>
                    <td className="px-md py-3 text-body-sm">{v.role}</td>
                    <td className="px-md py-3"><StatusChip status={v.status} type="volunteer" /></td>
                    <td className="px-md py-3 text-body-sm text-on-surface-variant">{v.email}</td>
                    <td className="px-md py-3 text-body-sm text-on-surface-variant">{v.phone}</td>
                    <td className="px-md py-3 text-body-sm text-on-surface-variant">{new Date(v.joinDate).toLocaleDateString()}</td>
                    <td className="px-md py-3">
                      <Link to={`/volunteers/${v.id}`} className="text-primary hover:underline text-label-sm">Edit</Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {result && <Pagination totalCount={result.totalCount} pageSize={result.pageSize} pageNumber={result.pageNumber} />}
        </>
      )}
    </section>
  );
}
