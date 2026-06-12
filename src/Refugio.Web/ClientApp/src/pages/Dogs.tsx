import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { Pagination } from '../components/Pagination';
import { StatusChip } from '../components/StatusChip';
import type { DogDto, DogStatus, Page } from '../types';

const DOG_STATUSES: DogStatus[] = ['Available', 'Adopted', 'Foster', 'Medical', 'Quarantine'];

export function Dogs() {
  const [searchParams, setSearchParams] = useSearchParams();
  const search = searchParams.get('search') ?? '';
  const status = (searchParams.get('status') ?? '') as DogStatus | '';
  const page = parseInt(searchParams.get('page') ?? '1', 10);

  const [result, setResult] = useState<Page<DogDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchInput, setSearchInput] = useState(search);

  useEffect(() => {
    setLoading(true);
    dogsApi.paged(search || undefined, (status as DogStatus) || undefined, page)
      .then(setResult)
      .catch(() => setError('Failed to load dogs.'))
      .finally(() => setLoading(false));
  }, [search, status, page]);

  const applySearch = (e: React.FormEvent) => {
    e.preventDefault();
    const p = new URLSearchParams(searchParams);
    p.set('search', searchInput);
    p.set('page', '1');
    setSearchParams(p, { replace: true });
  };

  const setStatus = (s: string) => {
    const p = new URLSearchParams(searchParams);
    if (s) p.set('status', s); else p.delete('status');
    p.set('page', '1');
    setSearchParams(p, { replace: true });
  };

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="font-headline-xl text-headline-xl text-primary">Dogs</h2>
        <Link
          to="/dogs/new"
          className="bg-primary text-on-primary px-md py-3 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all flex items-center gap-sm"
        >
          <span className="material-symbols-outlined">add</span>
          Check In Dog
        </Link>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-sm items-center">
        <form onSubmit={applySearch} className="flex gap-sm">
          <div className="relative">
            <span className="absolute inset-y-0 left-3 flex items-center text-on-surface-variant">
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>search</span>
            </span>
            <input
              value={searchInput}
              onChange={e => setSearchInput(e.target.value)}
              placeholder="Search dogs..."
              className="pl-9 pr-md py-2 bg-surface-container border border-outline-variant rounded-lg text-body-sm focus:ring-2 focus:ring-primary transition-all w-64"
            />
          </div>
          <button type="submit" className="bg-primary text-on-primary px-md py-2 rounded-lg text-label-md hover:brightness-110 transition-all">
            Search
          </button>
        </form>

        <div className="flex gap-xs flex-wrap">
          <button
            onClick={() => setStatus('')}
            className={`px-sm py-1.5 rounded-full text-label-sm border transition-all ${
              !status ? 'bg-primary text-on-primary border-primary' : 'border-outline-variant hover:bg-surface-container'
            }`}
          >
            All
          </button>
          {DOG_STATUSES.map(s => (
            <button
              key={s}
              onClick={() => setStatus(s)}
              className={`px-sm py-1.5 rounded-full text-label-sm border transition-all ${
                status === s ? 'bg-primary text-on-primary border-primary' : 'border-outline-variant hover:bg-surface-container'
              }`}
            >
              {s}
            </button>
          ))}
        </div>
      </div>

      {loading ? <LoadingSpinner /> : error ? <ErrorMessage message={error} /> : (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-gutter">
            {result?.items.map(dog => (
              <Link
                key={dog.id}
                to={`/dogs/${dog.id}`}
                className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 overflow-hidden hover:shadow-soft-hover transition-all group"
              >
                <div className="aspect-square bg-surface-container flex items-center justify-center overflow-hidden">
                  {dog.photoUrl ? (
                    <img src={dog.photoUrl} alt={dog.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
                  ) : (
                    <span className="material-symbols-outlined text-on-surface-variant text-6xl" style={{ fontVariationSettings: "'FILL' 1" }}>pets</span>
                  )}
                </div>
                <div className="p-md">
                  <div className="flex items-start justify-between gap-xs mb-xs">
                    <h3 className="font-headline-md text-headline-md text-on-surface">{dog.name}</h3>
                    <StatusChip status={dog.status} type="dog" />
                  </div>
                  <p className="text-body-sm text-on-surface-variant">{dog.breed}</p>
                  <p className="text-label-sm text-on-surface-variant mt-1">
                    {dog.ageMonths < 12
                      ? `${dog.ageMonths}mo`
                      : `${Math.floor(dog.ageMonths / 12)}yr`}
                    {' · '}{dog.gender}
                  </p>
                </div>
              </Link>
            ))}
          </div>

          {result && (
            <Pagination
              totalCount={result.totalCount}
              pageSize={result.pageSize}
              pageNumber={result.pageNumber}
            />
          )}
        </>
      )}
    </section>
  );
}
