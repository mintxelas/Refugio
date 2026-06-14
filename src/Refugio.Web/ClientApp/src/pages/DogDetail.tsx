import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { StatusChip } from '../components/StatusChip';
import type { DogDto, MedicalRecordDto, MedicationDto, DogPhotoDto } from '../types';

export function DogDetail() {
  const { id } = useParams<{ id: string }>();
  const dogId = parseInt(id!, 10);

  const [dog, setDog] = useState<DogDto | null>(null);
  const [medical, setMedical] = useState<MedicalRecordDto[]>([]);
  const [medications, setMedications] = useState<MedicationDto[]>([]);
  const [photos, setPhotos] = useState<DogPhotoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.all([
      dogsApi.get(dogId),
      dogsApi.getMedicalRecords(dogId),
      dogsApi.getMedications(dogId),
      dogsApi.getPhotos(dogId),
    ])
      .then(([d, m, meds, p]) => {
        setDog(d);
        setMedical(m);
        setMedications(meds);
        setPhotos(p);
      })
      .catch(() => setError('Perro no encontrado.'))
      .finally(() => setLoading(false));
  }, [dogId]);

  if (loading) return <LoadingSpinner />;
  if (error || !dog) return <div className="p-margin-desktop"><ErrorMessage message={error || 'No encontrado'} /></div>;

  const mainPhoto = photos.find(p => p.isDefault) ?? photos[0];

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      <div className="flex items-center gap-sm">
        <Link to="/dogs" className="text-on-surface-variant hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span>
        </Link>
        <h2 className="font-headline-xl text-headline-xl text-primary flex-1">{dog.name}</h2>
        <Link
          to={`/dogs/${dog.id}/edit`}
          className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all flex items-center gap-sm"
        >
          <span className="material-symbols-outlined">edit</span>
          Editar
        </Link>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-gutter">
        {/* Photo + basic info */}
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 overflow-hidden">
          <div className="aspect-square bg-surface-container flex items-center justify-center">
            {mainPhoto ? (
              <img src={mainPhoto.url} alt={dog.name} className="w-full h-full object-cover" />
            ) : (
              <span className="material-symbols-outlined text-on-surface-variant text-8xl" style={{ fontVariationSettings: "'FILL' 1" }}>pets</span>
            )}
          </div>
          <div className="p-md space-y-2">
            <div className="flex items-center justify-between">
              <h3 className="font-headline-md text-headline-md text-on-surface">{dog.name}</h3>
              <StatusChip status={dog.status} type="dog" />
            </div>
            <p className="text-body-md text-on-surface-variant">{dog.breed}</p>
            <div className="grid grid-cols-2 gap-2 text-body-sm">
              <div><span className="text-on-surface-variant">Edad:</span> {dog.ageMonths < 12 ? `${dog.ageMonths}m` : `${Math.floor(dog.ageMonths / 12)}a`}</div>
              <div><span className="text-on-surface-variant">Sexo:</span> {dog.gender === 'Male' ? 'Macho' : 'Hembra'}</div>
              <div><span className="text-on-surface-variant">Peso:</span> {dog.weightKg}kg</div>
              <div><span className="text-on-surface-variant">Llegada:</span> {new Date(dog.arrivalDate).toLocaleDateString()}</div>
            </div>
            {dog.traits && <p className="text-body-sm text-on-surface-variant"><span className="font-medium">Características:</span> {dog.traits}</p>}
            {dog.notes && <p className="text-body-sm text-on-surface-variant"><span className="font-medium">Notas:</span> {dog.notes}</p>}
          </div>
        </div>

        <div className="lg:col-span-2 space-y-gutter">
          {/* Photo gallery */}
          {photos.length > 1 && (
            <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
              <h3 className="font-headline-md text-headline-md text-on-surface mb-md">Fotos</h3>
              <div className="flex gap-2 flex-wrap">
                {photos.map(p => (
                  <img key={p.id} src={p.url} alt="" className={`w-20 h-20 rounded-lg object-cover ${p.isDefault ? 'ring-2 ring-primary' : ''}`} />
                ))}
              </div>
            </div>
          )}

          {/* Medical records */}
          <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
            <div className="flex items-center justify-between mb-md">
              <h3 className="font-headline-md text-headline-md text-on-surface">Registros médicos</h3>
              <Link to={`/health?dogId=${dog.id}`} className="text-label-sm text-primary hover:underline">
                <span className="material-symbols-outlined align-middle mr-1" style={{ fontSize: 16 }}>add</span>Añadir
              </Link>
            </div>
            {medical.length === 0 ? (
              <p className="text-body-sm text-on-surface-variant">Sin registros médicos.</p>
            ) : (
              <div className="space-y-2">
                {medical.map(r => (
                  <Link
                    key={r.id}
                    to={`/dogs/${dog.id}/medical/${r.id}`}
                    className="flex items-start gap-sm p-sm rounded-lg hover:bg-surface-container transition-all"
                  >
                    <span className="material-symbols-outlined text-on-surface-variant mt-0.5" style={{ fontSize: 18 }}>medical_services</span>
                    <div className="flex-1 min-w-0">
                      <p className="text-body-sm text-on-surface">{r.diagnosis}</p>
                      <p className="text-label-sm text-on-surface-variant">{r.vetName} · {new Date(r.visitDate).toLocaleDateString()}</p>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>

          {/* Medications */}
          <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
            <div className="flex items-center justify-between mb-md">
              <h3 className="font-headline-md text-headline-md text-on-surface">Medicamentos</h3>
              <Link to={`/health?dogId=${dog.id}`} className="text-label-sm text-primary hover:underline">
                <span className="material-symbols-outlined align-middle mr-1" style={{ fontSize: 16 }}>add</span>Añadir
              </Link>
            </div>
            {medications.length === 0 ? (
              <p className="text-body-sm text-on-surface-variant">Sin medicamentos.</p>
            ) : (
              <div className="space-y-2">
                {medications.map(m => (
                  <Link
                    key={m.id}
                    to={`/dogs/${dog.id}/medications/${m.id}`}
                    className="flex items-start gap-sm p-sm rounded-lg hover:bg-surface-container transition-all"
                  >
                    <span className={`material-symbols-outlined mt-0.5 ${m.isActive ? 'text-primary' : 'text-on-surface-variant'}`} style={{ fontSize: 18 }}>
                      medication
                    </span>
                    <div className="flex-1 min-w-0">
                      <p className="text-body-sm text-on-surface">{m.name} — {m.dosage}</p>
                      <p className="text-label-sm text-on-surface-variant">{m.frequency} · {m.isActive ? 'Activo' : 'Inactivo'}</p>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </section>
  );
}
