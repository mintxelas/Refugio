import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { dogsApi } from '../api/dogs';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import type { DogDto, MedicalRecordDto, MedicationDto, UpcomingVisit } from '../types';
import { dashboardApi } from '../api/dashboard';

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';

export function Health() {
  const [searchParams] = useSearchParams();
  const dogIdParam = searchParams.get('dogId');
  const navigate = useNavigate();

  const [dogs, setDogs] = useState<DogDto[]>([]);
  const [visits, setVisits] = useState<UpcomingVisit[]>([]);
  const [selectedDogId, setSelectedDogId] = useState<number | null>(dogIdParam ? parseInt(dogIdParam, 10) : null);
  const [medicalRecords, setMedicalRecords] = useState<MedicalRecordDto[]>([]);
  const [medications, setMedications] = useState<MedicationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [dogLoading, setDogLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  // Add medical record form
  const [medForm, setMedForm] = useState({ vetName: '', diagnosis: '', treatment: '', notes: '', nextVisitDate: '' });
  const [medSaving, setMedSaving] = useState(false);

  // Add medication form
  const [medcForm, setMedcForm] = useState({ name: '', dosage: '', frequency: '', startDate: new Date().toISOString().substring(0, 10), endDate: '' });
  const [medcSaving, setMedcSaving] = useState(false);

  useEffect(() => {
    Promise.all([dogsApi.list(), dashboardApi.getUpcomingVisits().catch(() => [])])
      .then(([d, v]) => { setDogs(d); setVisits(v); })
      .catch(() => setErrors(['Failed to load health data.']))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedDogId) return;
    setDogLoading(true);
    Promise.all([
      dogsApi.getMedicalRecords(selectedDogId),
      dogsApi.getMedications(selectedDogId),
    ])
      .then(([m, meds]) => { setMedicalRecords(m); setMedications(meds); })
      .finally(() => setDogLoading(false));
  }, [selectedDogId]);

  const addMedical = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDogId || !medForm.vetName.trim() || !medForm.diagnosis.trim() || !medForm.treatment.trim()) return;
    setMedSaving(true);
    try {
      const rec = await dogsApi.createMedicalRecord(selectedDogId, {
        vetName: medForm.vetName.trim(), diagnosis: medForm.diagnosis.trim(),
        treatment: medForm.treatment.trim(), notes: medForm.notes.trim() || null,
        nextVisitDate: medForm.nextVisitDate || null,
      });
      setMedicalRecords(rs => [rec, ...rs]);
      setMedForm({ vetName: '', diagnosis: '', treatment: '', notes: '', nextVisitDate: '' });
    } catch {
      setErrors(['Failed to add record.']);
    } finally {
      setMedSaving(false);
    }
  };

  const addMedication = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDogId || !medcForm.name.trim() || !medcForm.dosage.trim() || !medcForm.frequency.trim()) return;
    setMedcSaving(true);
    try {
      const med = await dogsApi.createMedication(selectedDogId, {
        name: medcForm.name.trim(), dosage: medcForm.dosage.trim(),
        frequency: medcForm.frequency.trim(), startDate: medcForm.startDate,
        endDate: medcForm.endDate || null,
      });
      setMedications(ms => [med, ...ms]);
      setMedcForm({ name: '', dosage: '', frequency: '', startDate: new Date().toISOString().substring(0, 10), endDate: '' });
    } catch {
      setErrors(['Failed to add medication.']);
    } finally {
      setMedcSaving(false);
    }
  };

  if (loading) return <LoadingSpinner />;

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      <h2 className="font-headline-xl text-headline-xl text-primary">Health Dashboard</h2>

      {errors.length > 0 && <div className="space-y-1">{errors.map((e, i) => <ErrorMessage key={i} message={e} />)}</div>}

      {/* Upcoming visits */}
      {visits.length > 0 && (
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
          <h3 className="font-headline-md text-headline-md text-on-surface mb-md">Upcoming Vet Visits</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-md">
            {visits.map((v, i) => (
              <div key={i} className="p-sm bg-surface-container rounded-lg">
                <p className="font-label-md text-label-md text-on-surface">{v.dogName}</p>
                <p className="text-body-sm text-on-surface-variant">{v.vetName}</p>
                <p className="text-body-sm text-primary">{new Date(v.nextVisitDate).toLocaleDateString()}</p>
                <p className="text-label-sm text-on-surface-variant">{v.diagnosis}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Dog selector */}
      <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
        <label className={LBL}>Select Dog</label>
        <select
          value={selectedDogId ?? ''}
          onChange={e => {
            const v = e.target.value;
            setSelectedDogId(v ? parseInt(v, 10) : null);
            if (v) navigate(`/health?dogId=${v}`, { replace: true });
          }}
          className={INPUT + ' max-w-sm'}
        >
          <option value="">-- Select a dog --</option>
          {dogs.map(d => <option key={d.id} value={d.id}>{d.name} ({d.breed})</option>)}
        </select>
      </div>

      {selectedDogId && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-gutter">
          {/* Add Medical Record */}
          <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
            <h3 className="font-headline-md text-headline-md text-on-surface mb-md">Add Medical Record</h3>
            <form onSubmit={addMedical} className="space-y-sm">
              <div><label className={LBL}>Vet Name *</label><input value={medForm.vetName} onChange={e => setMedForm(f => ({ ...f, vetName: e.target.value }))} className={INPUT} required /></div>
              <div><label className={LBL}>Diagnosis *</label><input value={medForm.diagnosis} onChange={e => setMedForm(f => ({ ...f, diagnosis: e.target.value }))} className={INPUT} required /></div>
              <div><label className={LBL}>Treatment *</label><textarea value={medForm.treatment} onChange={e => setMedForm(f => ({ ...f, treatment: e.target.value }))} rows={2} className={INPUT} required /></div>
              <div><label className={LBL}>Next Visit</label><input type="date" value={medForm.nextVisitDate} onChange={e => setMedForm(f => ({ ...f, nextVisitDate: e.target.value }))} className={INPUT} /></div>
              <button type="submit" disabled={medSaving} className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60">
                Add Record
              </button>
            </form>
            {dogLoading ? <LoadingSpinner /> : (
              <div className="mt-md space-y-2">
                {medicalRecords.map(r => (
                  <Link key={r.id} to={`/dogs/${selectedDogId}/medical/${r.id}`}
                    className="flex items-start gap-sm p-sm rounded-lg hover:bg-surface-container transition-all">
                    <span className="material-symbols-outlined text-on-surface-variant mt-0.5" style={{ fontSize: 18 }}>medical_services</span>
                    <div>
                      <p className="text-body-sm text-on-surface">{r.diagnosis}</p>
                      <p className="text-label-sm text-on-surface-variant">{r.vetName} · {new Date(r.visitDate).toLocaleDateString()}</p>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>

          {/* Add Medication */}
          <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
            <h3 className="font-headline-md text-headline-md text-on-surface mb-md">Add Medication</h3>
            <form onSubmit={addMedication} className="space-y-sm">
              <div><label className={LBL}>Name *</label><input value={medcForm.name} onChange={e => setMedcForm(f => ({ ...f, name: e.target.value }))} className={INPUT} required /></div>
              <div className="grid grid-cols-2 gap-sm">
                <div><label className={LBL}>Dosage *</label><input value={medcForm.dosage} onChange={e => setMedcForm(f => ({ ...f, dosage: e.target.value }))} className={INPUT} required /></div>
                <div><label className={LBL}>Frequency *</label><input value={medcForm.frequency} onChange={e => setMedcForm(f => ({ ...f, frequency: e.target.value }))} className={INPUT} required /></div>
              </div>
              <div className="grid grid-cols-2 gap-sm">
                <div><label className={LBL}>Start Date</label><input type="date" value={medcForm.startDate} onChange={e => setMedcForm(f => ({ ...f, startDate: e.target.value }))} className={INPUT} /></div>
                <div><label className={LBL}>End Date</label><input type="date" value={medcForm.endDate} onChange={e => setMedcForm(f => ({ ...f, endDate: e.target.value }))} className={INPUT} /></div>
              </div>
              <button type="submit" disabled={medcSaving} className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60">
                Add Medication
              </button>
            </form>
            {dogLoading ? <LoadingSpinner /> : (
              <div className="mt-md space-y-2">
                {medications.map(m => (
                  <Link key={m.id} to={`/dogs/${selectedDogId}/medications/${m.id}`}
                    className="flex items-start gap-sm p-sm rounded-lg hover:bg-surface-container transition-all">
                    <span className={`material-symbols-outlined mt-0.5 ${m.isActive ? 'text-primary' : 'text-on-surface-variant'}`} style={{ fontSize: 18 }}>medication</span>
                    <div>
                      <p className="text-body-sm text-on-surface">{m.name} — {m.dosage}</p>
                      <p className="text-label-sm text-on-surface-variant">{m.frequency} · {m.isActive ? 'Active' : 'Inactive'}</p>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </section>
  );
}
