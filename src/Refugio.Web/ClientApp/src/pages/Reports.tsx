import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { dashboardApi } from '../api/dashboard';
import { LoadingSpinner } from '../components/LoadingSpinner';
import type { AdoptionConversionStats, ShelterStayStats } from '../types';

const MONTHS = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

export function Reports() {
  const [searchParams, setSearchParams] = useSearchParams();
  const year = parseInt(searchParams.get('year') ?? String(new Date().getFullYear()), 10);

  const [conversion, setConversion] = useState<AdoptionConversionStats | null>(null);
  const [stay, setStay] = useState<ShelterStayStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    Promise.all([
      dashboardApi.getAdoptionConversion(year),
      dashboardApi.getShelterStay(),
    ])
      .then(([c, s]) => { setConversion(c); setStay(s); })
      .finally(() => setLoading(false));
  }, [year]);

  const setYear = (y: number) => {
    const p = new URLSearchParams(searchParams);
    p.set('year', String(y));
    setSearchParams(p, { replace: true });
  };

  const maxConv = conversion
    ? Math.max(1, ...conversion.monthly.map(m => Math.max(m.applied, m.finalized)))
    : 1;

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="font-headline-xl text-headline-xl text-primary">Informes</h2>
        <div className="flex items-center gap-sm">
          <button onClick={() => setYear(year - 1)} className="p-2 rounded-lg hover:bg-surface-container transition-all">
            <span className="material-symbols-outlined">chevron_left</span>
          </button>
          <span className="font-label-md text-label-md text-on-surface w-12 text-center">{year}</span>
          <button onClick={() => setYear(year + 1)} className="p-2 rounded-lg hover:bg-surface-container transition-all">
            <span className="material-symbols-outlined">chevron_right</span>
          </button>
        </div>
      </div>

      {loading ? <LoadingSpinner /> : (
        <>
          {/* Adoption Conversion */}
          {conversion && (
            <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
              <div className="flex items-center justify-between mb-md">
                <h3 className="font-headline-md text-headline-md text-on-surface">Conversión de adopciones {year}</h3>
                <div className="flex gap-md text-label-sm">
                  <span className="flex items-center gap-xs"><span className="w-3 h-3 rounded-sm bg-primary inline-block" />Solicitadas: {conversion.totalApplied}</span>
                  <span className="flex items-center gap-xs"><span className="w-3 h-3 rounded-sm bg-secondary inline-block" />Finalizadas: {conversion.totalFinalized}</span>
                </div>
              </div>
              <div className="flex items-end gap-1 h-40">
                {MONTHS.map((m, i) => {
                  const data = conversion.monthly.find(d => d.month === i + 1) ?? { applied: 0, finalized: 0 };
                  return (
                    <div key={m} className="flex-1 flex flex-col items-center gap-0.5">
                      <div className="flex gap-0.5 items-end w-full">
                        <div className="flex-1 bg-primary/70 rounded-t transition-all" style={{ height: `${(data.applied / maxConv) * 120}px` }} title={`Solicitadas: ${data.applied}`} />
                        <div className="flex-1 bg-secondary/70 rounded-t transition-all" style={{ height: `${(data.finalized / maxConv) * 120}px` }} title={`Finalizadas: ${data.finalized}`} />
                      </div>
                      <span className="text-label-sm text-on-surface-variant" style={{ fontSize: 10 }}>{m}</span>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Shelter Stay by Breed */}
          {stay && stay.byBreed.length > 0 && (
            <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
              <h3 className="font-headline-md text-headline-md text-on-surface mb-md">Estancia media en el refugio por raza</h3>
              <div className="overflow-hidden rounded-lg border border-outline-variant/20">
                <table className="w-full">
                  <thead className="bg-surface-container">
                    <tr>
                      {['Raza', 'Días medios', 'Perros'].map(h => (
                        <th key={h} className="px-md py-3 text-left text-label-md font-label-md text-on-surface-variant">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-outline-variant/20">
                    {stay.byBreed.sort((a, b) => b.avgDays - a.avgDays).map(b => (
                      <tr key={b.breed} className="hover:bg-surface-container/50">
                        <td className="px-md py-3 text-body-sm">{b.breed}</td>
                        <td className="px-md py-3 text-body-sm font-medium text-primary">{b.avgDays.toFixed(1)}</td>
                        <td className="px-md py-3 text-body-sm text-on-surface-variant">{b.count}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}
    </section>
  );
}
