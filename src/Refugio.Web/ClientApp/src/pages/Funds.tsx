import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { financeApi } from '../api/finance';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { Pagination } from '../components/Pagination';
import { useAuth } from '../auth/AuthContext';
import type { DonationDto, ExpenseDto, GoalDto, FinanceSummary, Page } from '../types';

type Tab = 'donations' | 'expenses' | 'goals' | 'summary';

export function Funds() {
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = (searchParams.get('tab') ?? 'donations') as Tab;
  const page = parseInt(searchParams.get('page') ?? '1', 10);
  const year = parseInt(searchParams.get('year') ?? String(new Date().getFullYear()), 10);
  const { user } = useAuth();
  const isManager = user?.role === 'Manager';

  const [donations, setDonations] = useState<Page<DonationDto> | null>(null);
  const [expenses, setExpenses] = useState<Page<ExpenseDto> | null>(null);
  const [goals, setGoals] = useState<GoalDto[]>([]);
  const [summary, setSummary] = useState<FinanceSummary | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setLoading(true);
    const tasks: Promise<void>[] = [];
    if (tab === 'donations') tasks.push(financeApi.getDonationsPaged(page).then(setDonations));
    if (tab === 'expenses') tasks.push(financeApi.getExpensesPaged(page).then(setExpenses));
    if (tab === 'goals') tasks.push(financeApi.getGoals().then(setGoals));
    if (tab === 'summary') tasks.push(financeApi.getSummary(year).then(setSummary));
    Promise.all(tasks).finally(() => setLoading(false));
  }, [tab, page, year]);

  const setTab = (t: Tab) => {
    const p = new URLSearchParams(searchParams);
    p.set('tab', t); p.set('page', '1');
    setSearchParams(p, { replace: true });
  };

  const deleteDonation = async (id: number) => {
    if (!confirm('Delete donation?')) return;
    await financeApi.deleteDonation(id);
    financeApi.getDonationsPaged(page).then(setDonations);
  };
  const deleteExpense = async (id: number) => {
    if (!confirm('Delete expense?')) return;
    await financeApi.deleteExpense(id);
    financeApi.getExpensesPaged(page).then(setExpenses);
  };
  const deleteGoal = async (id: number) => {
    if (!confirm('Delete goal?')) return;
    await financeApi.deleteGoal(id);
    financeApi.getGoals().then(setGoals);
  };

  const TABS: { key: Tab; label: string }[] = [
    { key: 'donations', label: 'Donations' },
    { key: 'expenses', label: 'Expenses' },
    { key: 'goals', label: 'Goals' },
    { key: 'summary', label: 'Summary' },
  ];

  return (
    <section className="p-margin-desktop space-y-lg max-w-screen-xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="font-headline-xl text-headline-xl text-primary">Funds</h2>
        <div className="flex gap-sm">
          {tab === 'donations' && (
            <>
              <a href="/api/export/donations" className="px-md py-2 rounded-lg border border-outline-variant text-label-md hover:bg-surface-container transition-all flex items-center gap-xs">
                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>download</span>CSV
              </a>
              <Link to="/funds/donations/new" className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 flex items-center gap-xs">
                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>add</span>Add
              </Link>
            </>
          )}
          {tab === 'expenses' && (
            <>
              <a href="/api/export/expenses" className="px-md py-2 rounded-lg border border-outline-variant text-label-md hover:bg-surface-container transition-all flex items-center gap-xs">
                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>download</span>CSV
              </a>
              <Link to="/funds/expenses/new" className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 flex items-center gap-xs">
                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>add</span>Add
              </Link>
            </>
          )}
          {tab === 'goals' && (
            <Link to="/funds/goals/new" className="bg-primary text-on-primary px-md py-2 rounded-lg font-label-md text-label-md hover:brightness-110 flex items-center gap-xs">
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>add</span>Add Goal
            </Link>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-xs border-b border-outline-variant">
        {TABS.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`px-md py-3 font-label-md text-label-md transition-all border-b-2 -mb-px ${tab === t.key ? 'border-primary text-primary' : 'border-transparent text-on-surface-variant hover:text-on-surface'}`}>
            {t.label}
          </button>
        ))}
      </div>

      {loading ? <LoadingSpinner /> : (
        <>
          {tab === 'donations' && donations && (
            <div className="space-y-md">
              <div className="overflow-hidden rounded-xl border border-outline-variant/30">
                <table className="w-full">
                  <thead className="bg-surface-container">
                    <tr>
                      {['Date', 'Donor', 'Category', 'Amount', 'Notes', ''].map(h => (
                        <th key={h} className="px-md py-3 text-left text-label-md font-label-md text-on-surface-variant">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-outline-variant/20">
                    {donations.items.map(d => (
                      <tr key={d.id} className="hover:bg-surface-container/50 transition-colors">
                        <td className="px-md py-3 text-body-sm">{new Date(d.date).toLocaleDateString()}</td>
                        <td className="px-md py-3 text-body-sm">{d.donorName}</td>
                        <td className="px-md py-3 text-body-sm">{d.category}</td>
                        <td className="px-md py-3 text-body-sm font-medium text-primary">${d.amount.toLocaleString()}</td>
                        <td className="px-md py-3 text-body-sm text-on-surface-variant truncate max-w-xs">{d.notes}</td>
                        <td className="px-md py-3 text-right">
                          <Link to={`/funds/donations/${d.id}`} className="text-primary hover:underline text-label-sm mr-2">Edit</Link>
                          {isManager && <button onClick={() => deleteDonation(d.id)} className="text-error hover:underline text-label-sm">Del</button>}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <Pagination totalCount={donations.totalCount} pageSize={donations.pageSize} pageNumber={donations.pageNumber} />
            </div>
          )}

          {tab === 'expenses' && expenses && (
            <div className="space-y-md">
              <div className="overflow-hidden rounded-xl border border-outline-variant/30">
                <table className="w-full">
                  <thead className="bg-surface-container">
                    <tr>
                      {['Date', 'Description', 'Category', 'Amount', 'Notes', ''].map(h => (
                        <th key={h} className="px-md py-3 text-left text-label-md font-label-md text-on-surface-variant">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-outline-variant/20">
                    {expenses.items.map(e => (
                      <tr key={e.id} className="hover:bg-surface-container/50 transition-colors">
                        <td className="px-md py-3 text-body-sm">{new Date(e.date).toLocaleDateString()}</td>
                        <td className="px-md py-3 text-body-sm">{e.description}</td>
                        <td className="px-md py-3 text-body-sm">{e.category}</td>
                        <td className="px-md py-3 text-body-sm font-medium text-error">${e.amount.toLocaleString()}</td>
                        <td className="px-md py-3 text-body-sm text-on-surface-variant truncate max-w-xs">{e.notes}</td>
                        <td className="px-md py-3 text-right">
                          <Link to={`/funds/expenses/${e.id}`} className="text-primary hover:underline text-label-sm mr-2">Edit</Link>
                          {isManager && <button onClick={() => deleteExpense(e.id)} className="text-error hover:underline text-label-sm">Del</button>}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <Pagination totalCount={expenses.totalCount} pageSize={expenses.pageSize} pageNumber={expenses.pageNumber} />
            </div>
          )}

          {tab === 'goals' && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-gutter">
              {goals.map(g => {
                const pct = g.targetAmount > 0 ? Math.min(100, Math.round((g.currentAmount / g.targetAmount) * 100)) : 0;
                return (
                  <div key={g.id} className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md">
                    <div className="flex items-start justify-between mb-sm">
                      <h3 className="font-headline-md text-headline-md text-on-surface">{g.title}</h3>
                      <div className="flex gap-1">
                        <Link to={`/funds/goals/${g.id}`} className="text-primary text-label-sm hover:underline">Edit</Link>
                        {isManager && <button onClick={() => deleteGoal(g.id)} className="text-error text-label-sm hover:underline ml-2">Del</button>}
                      </div>
                    </div>
                    {g.description && <p className="text-body-sm text-on-surface-variant mb-sm">{g.description}</p>}
                    <div className="h-2 bg-surface-container rounded-full overflow-hidden mb-1">
                      <div className="h-full bg-primary rounded-full" style={{ width: `${pct}%` }} />
                    </div>
                    <div className="flex justify-between text-label-sm text-on-surface-variant">
                      <span>${g.currentAmount.toLocaleString()} raised</span>
                      <span>{pct}% of ${g.targetAmount.toLocaleString()}</span>
                    </div>
                    {g.deadline && <p className="text-label-sm text-on-surface-variant mt-1">Deadline: {new Date(g.deadline).toLocaleDateString()}</p>}
                  </div>
                );
              })}
            </div>
          )}

          {tab === 'summary' && summary && (
            <div className="space-y-lg">
              <div className="grid grid-cols-2 gap-gutter">
                <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md text-center">
                  <p className="text-label-md font-label-md text-on-surface-variant">Total Income</p>
                  <p className="font-headline-lg text-headline-lg text-primary">${summary.totalIncome.toLocaleString()}</p>
                </div>
                <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-md text-center">
                  <p className="text-label-md font-label-md text-on-surface-variant">Total Expenses</p>
                  <p className="font-headline-lg text-headline-lg text-error">${summary.totalExpenses.toLocaleString()}</p>
                </div>
              </div>
              <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 overflow-hidden">
                <table className="w-full">
                  <thead className="bg-surface-container">
                    <tr>
                      {['Month', 'Income', 'Expenses', 'Net'].map(h => (
                        <th key={h} className="px-md py-3 text-left text-label-md font-label-md text-on-surface-variant">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-outline-variant/20">
                    {summary.monthly.map(m => (
                      <tr key={m.month} className="hover:bg-surface-container/50">
                        <td className="px-md py-3 text-body-sm">{new Date(year, m.month - 1).toLocaleString('default', { month: 'long' })}</td>
                        <td className="px-md py-3 text-body-sm text-primary">${m.income.toLocaleString()}</td>
                        <td className="px-md py-3 text-body-sm text-error">${m.expenses.toLocaleString()}</td>
                        <td className={`px-md py-3 text-body-sm font-medium ${m.income - m.expenses >= 0 ? 'text-primary' : 'text-error'}`}>
                          ${(m.income - m.expenses).toLocaleString()}
                        </td>
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
