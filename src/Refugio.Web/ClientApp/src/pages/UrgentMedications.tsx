import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { dashboardApi } from '../api/dashboard';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import type { UrgentMedicationDto } from '../types';

export function UrgentMedications() {
  const [meds, setMeds] = useState<UrgentMedicationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    dashboardApi.getUrgentMedications()
      .then(setMeds)
      .catch(() => setError('Error al cargar los medicamentos urgentes.'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      <div>
        <h2 className="font-headline-xl text-headline-xl text-primary mb-xs">Medicamentos urgentes</h2>
        <p className="text-body-lg text-on-surface-variant">Perros con medicación activa que finaliza en los próximos 3 días.</p>
      </div>

      {error && <ErrorMessage message={error} />}

      {meds.length === 0 ? (
        <p className="text-body-sm text-on-surface-variant text-center py-md">No hay medicamentos urgentes.</p>
      ) : (
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 divide-y divide-outline-variant/30">
          {meds.map(m => (
            <Link
              key={m.medicationId}
              to={`/dogs/${m.dogId}/medications/${m.medicationId}`}
              className="flex items-center gap-sm p-md hover:bg-surface-container transition-all"
            >
              <span className="material-symbols-outlined text-error" style={{ fontSize: 24 }}>medication</span>
              <div className="flex-1 min-w-0">
                <p className="text-body-md text-on-surface">{m.dogName} — {m.name}</p>
                <p className="text-label-sm text-on-surface-variant">
                  {m.dosage} · {m.frequency}
                  {m.endDate && ` · Finaliza el ${new Date(m.endDate).toLocaleDateString()}`}
                </p>
              </div>
            </Link>
          ))}
        </div>
      )}
    </section>
  );
}
