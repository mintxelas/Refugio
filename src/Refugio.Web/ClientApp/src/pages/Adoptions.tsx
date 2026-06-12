import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { adoptionsApi } from '../api/adoptions';
import { dogsApi } from '../api/dogs';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { StatusChip } from '../components/StatusChip';
import { useAuth } from '../auth/AuthContext';
import type { AdoptionDto, AdoptionStatus, AdoptionType, DogDto } from '../types';

const COLUMNS: AdoptionStatus[] = ['Applied', 'Interview', 'HomeCheck', 'Approved', 'Finalized'];
const PAGE_SIZE = 5;

export function Adoptions() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const isManager = user?.role === 'Manager';

  // Per-column limits
  const getLimit = (col: AdoptionStatus) => parseInt(searchParams.get(`${col}Limit`) ?? String(PAGE_SIZE), 10);

  const [allAdoptions, setAllAdoptions] = useState<AdoptionDto[]>([]);
  const [dogs, setDogs] = useState<DogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showNewForm, setShowNewForm] = useState(false);
  const [newForm, setNewForm] = useState({ dogId: '', applicantName: '', applicantEmail: '', applicantPhone: '', type: 'Adoption' as AdoptionType, notes: '' });
  const [newSaving, setNewSaving] = useState(false);

  const load = () => {
    setLoading(true);
    Promise.all([adoptionsApi.list(), dogsApi.list()])
      .then(([a, d]) => { setAllAdoptions(a); setDogs(d); })
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const advanceStatus = async (id: number, current: AdoptionStatus) => {
    const next: Record<AdoptionStatus, AdoptionStatus | null> = {
      Applied: 'Interview', Interview: 'HomeCheck', HomeCheck: 'Approved', Approved: 'Finalized', Finalized: null, Rejected: null,
    };
    const n = next[current];
    if (!n) return;
    await adoptionsApi.updateStatus(id, n);
    setAllAdoptions(as => as.map(a => a.id === id ? { ...a, status: n } : a));
  };

  const rejectAdoption = async (id: number) => {
    if (!confirm('Reject this adoption?')) return;
    await adoptionsApi.updateStatus(id, 'Rejected');
    setAllAdoptions(as => as.map(a => a.id === id ? { ...a, status: 'Rejected' } : a));
  };

  const createAdoption = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newForm.dogId || !newForm.applicantName.trim()) return;
    setNewSaving(true);
    try {
      const a = await adoptionsApi.create({
        dogId: parseInt(newForm.dogId, 10),
        applicantName: newForm.applicantName.trim(),
        applicantEmail: newForm.applicantEmail.trim() || null,
        applicantPhone: newForm.applicantPhone.trim() || null,
        type: newForm.type,
        notes: newForm.notes.trim() || null,
      });
      setAllAdoptions(as => [a, ...as]);
      setShowNewForm(false);
      setNewForm({ dogId: '', applicantName: '', applicantEmail: '', applicantPhone: '', type: 'Adoption', notes: '' });
    } catch {
      // keep form open
    } finally {
      setNewSaving(false);
    }
  };

  const showMore = (col: AdoptionStatus) => {
    const p = new URLSearchParams(searchParams);
    p.set(`${col}Limit`, String(getLimit(col) + PAGE_SIZE));
    setSearchParams(p, { replace: true });
  };

  if (loading) return <LoadingSpinner />;

  const rejected = allAdoptions.filter(a => a.status === 'Rejected');

  return (
    <section className="p-margin-desktop space-y-lg">
      <div className="flex items-center justify-between">
        <h2 className="font-headline-xl text-headline-xl text-primary">Adoptions</h2>
        <div className="flex gap-sm">
          <a href="/api/export/adoptions" className="px-md py-2 rounded-lg border border-outline-variant text-label-md hover:bg-surface-container transition-all flex items-center gap-xs">
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>download</span>
            CSV
          </a>
          <button onClick={() => setShowNewForm(!showNewForm)}
            className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all flex items-center gap-sm">
            <span className="material-symbols-outlined">add</span>
            New Application
          </button>
        </div>
      </div>

      {/* New application form */}
      {showNewForm && (
        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
          <h3 className="font-headline-md text-headline-md text-on-surface mb-md">New Application</h3>
          <form onSubmit={createAdoption} className="grid grid-cols-2 gap-md">
            <div>
              <label className={LBL}>Dog *</label>
              <select value={newForm.dogId} onChange={e => setNewForm(f => ({ ...f, dogId: e.target.value }))} className={INPUT} required>
                <option value="">Select dog</option>
                {dogs.filter(d => d.status === 'Available' || d.status === 'Foster').map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
            </div>
            <div><label className={LBL}>Type</label>
              <select value={newForm.type} onChange={e => setNewForm(f => ({ ...f, type: e.target.value as AdoptionType }))} className={INPUT}>
                <option value="Adoption">Adoption</option>
                <option value="Foster">Foster</option>
              </select>
            </div>
            <div><label className={LBL}>Applicant Name *</label><input value={newForm.applicantName} onChange={e => setNewForm(f => ({ ...f, applicantName: e.target.value }))} className={INPUT} required /></div>
            <div><label className={LBL}>Email</label><input type="email" value={newForm.applicantEmail} onChange={e => setNewForm(f => ({ ...f, applicantEmail: e.target.value }))} className={INPUT} /></div>
            <div><label className={LBL}>Phone</label><input value={newForm.applicantPhone} onChange={e => setNewForm(f => ({ ...f, applicantPhone: e.target.value }))} className={INPUT} /></div>
            <div><label className={LBL}>Notes</label><input value={newForm.notes} onChange={e => setNewForm(f => ({ ...f, notes: e.target.value }))} className={INPUT} /></div>
            <div className="col-span-2 flex gap-sm justify-end">
              <button type="button" onClick={() => setShowNewForm(false)} className="px-md py-2 rounded-lg border border-outline-variant text-body-sm hover:bg-surface-container transition-all">Cancel</button>
              <button type="submit" disabled={newSaving} className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 disabled:opacity-60">Submit</button>
            </div>
          </form>
        </div>
      )}

      {/* Kanban */}
      <div className="flex gap-gutter overflow-x-auto pb-md">
        {COLUMNS.map(col => {
          const cards = allAdoptions.filter(a => a.status === col);
          const limit = getLimit(col);
          const visible = cards.slice(0, limit);
          return (
            <div key={col} className="flex-shrink-0 w-64">
              <div className="flex items-center justify-between mb-sm px-1">
                <span className="font-label-md text-label-md text-on-surface">{col}</span>
                <span className="bg-primary/10 text-primary text-label-sm px-sm py-xs rounded-full">{cards.length}</span>
              </div>
              <div className="space-y-2">
                {visible.map(a => (
                  <div key={a.id} className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
                    <div className="flex items-start justify-between mb-xs">
                      <Link to={`/adoptions/${a.id}`} className="font-label-md text-label-md text-on-surface hover:text-primary transition-colors flex-1 truncate">
                        {a.applicantName}
                      </Link>
                      <span className={`text-label-sm px-xs py-0.5 rounded ${a.type === 'Foster' ? 'bg-tertiary/10 text-tertiary' : 'bg-secondary/10 text-secondary'}`}>
                        {a.type}
                      </span>
                    </div>
                    {a.dog && <p className="text-label-sm text-on-surface-variant mb-sm">🐕 {a.dog.name}</p>}
                    <p className="text-label-sm text-on-surface-variant mb-sm">{new Date(a.createdAt).toLocaleDateString()}</p>
                    <div className="flex gap-1">
                      {col !== 'Finalized' && (
                        <button onClick={() => advanceStatus(a.id, col)}
                          className="flex-1 bg-primary/10 text-primary py-1 rounded text-label-sm hover:bg-primary/20 transition-all">
                          Advance
                        </button>
                      )}
                      {isManager && col !== 'Finalized' && col !== 'Rejected' && (
                        <button onClick={() => rejectAdoption(a.id)}
                          className="px-sm bg-error/10 text-error py-1 rounded text-label-sm hover:bg-error/20 transition-all">
                          Reject
                        </button>
                      )}
                    </div>
                  </div>
                ))}
                {cards.length > limit && (
                  <button onClick={() => showMore(col)} className="w-full py-2 text-label-sm text-primary hover:bg-primary/5 rounded-lg transition-all">
                    Show more ({cards.length - limit} more)
                  </button>
                )}
              </div>
            </div>
          );
        })}

        {/* Rejected column */}
        {rejected.length > 0 && (
          <div className="flex-shrink-0 w-64">
            <div className="flex items-center justify-between mb-sm px-1">
              <span className="font-label-md text-label-md text-error">Rejected</span>
              <span className="bg-error/10 text-error text-label-sm px-sm py-xs rounded-full">{rejected.length}</span>
            </div>
            <div className="space-y-2">
              {rejected.slice(0, getLimit('Rejected')).map(a => (
                <Link key={a.id} to={`/adoptions/${a.id}`}
                  className="block bg-surface-container-lowest rounded-xl border border-error/20 p-md hover:shadow-soft transition-all">
                  <p className="font-label-md text-label-md text-on-surface">{a.applicantName}</p>
                  {a.dog && <p className="text-label-sm text-on-surface-variant">{a.dog.name}</p>}
                </Link>
              ))}
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

const INPUT = 'w-full px-md py-2.5 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all';
const LBL = 'block text-label-md font-label-md text-on-surface mb-1';
