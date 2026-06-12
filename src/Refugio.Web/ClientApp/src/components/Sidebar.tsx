import { NavLink } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import type { ShelterSettingsDto } from '../types';

interface NavItem {
  to: string;
  icon: string;
  label: string;
  managerOnly?: boolean;
  errorStyle?: boolean;
}

const navItems: NavItem[] = [
  { to: '/', icon: 'dashboard', label: 'Dashboard' },
  { to: '/dogs', icon: 'pets', label: 'Dogs' },
  { to: '/health', icon: 'medical_services', label: 'Health' },
  { to: '/adoptions', icon: 'favorite', label: 'Adoptions' },
  { to: '/funds', icon: 'payments', label: 'Funds' },
  { to: '/calendar', icon: 'calendar_month', label: 'Calendar' },
  { to: '/volunteers', icon: 'group', label: 'Volunteers' },
  { to: '/reports', icon: 'bar_chart', label: 'Reports' },
  { to: '/admin/deleted', icon: 'admin_panel_settings', label: 'Admin / Deleted', managerOnly: true, errorStyle: true },
  { to: '/settings', icon: 'settings', label: 'Settings', managerOnly: true },
];

interface Props {
  settings: ShelterSettingsDto | null;
}

export function Sidebar({ settings }: Props) {
  const { user } = useAuth();

  return (
    <aside className="w-64 fixed left-0 top-0 h-screen bg-surface-container flex flex-col py-md border-r border-outline-variant z-50">
      <div className="px-md mb-lg">
        <div className="flex items-center gap-sm">
          <div className="w-[60px] h-[60px] rounded-lg overflow-hidden flex items-center justify-center flex-shrink-0">
            {settings?.logoUrl ? (
              <img src={settings.logoUrl} alt="logo" className="w-full h-full object-cover" />
            ) : (
              <div className="w-10 h-10 rounded-lg bg-primary flex items-center justify-center">
                <span className="material-symbols-outlined text-on-primary" style={{ fontVariationSettings: "'FILL' 1" }}>pets</span>
              </div>
            )}
          </div>
          <div>
            <h1 className="font-headline-md text-headline-md text-primary leading-tight">
              {settings?.name ?? 'Refugio'}
            </h1>
            {settings?.phrase && (
              <p className="text-label-sm font-label-sm text-on-surface-variant">{settings.phrase}</p>
            )}
          </div>
        </div>
      </div>

      <nav className="flex-1 space-y-1 px-sm overflow-y-auto">
        {navItems.map(item => {
          if (item.managerOnly && user?.role !== 'Manager') return null;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-sm px-md py-3 transition-all duration-200 ${
                  isActive
                    ? item.errorStyle
                      ? 'text-error font-bold border-r-4 border-error bg-error-container/10'
                      : 'text-primary font-bold border-r-4 border-primary bg-primary-container/10'
                    : item.errorStyle
                      ? 'text-on-surface-variant hover:bg-error-container/20'
                      : 'text-on-surface-variant hover:bg-primary-container/20'
                }`
              }
            >
              <span className="material-symbols-outlined">{item.icon}</span>
              <span className="font-label-md text-label-md">{item.label}</span>
            </NavLink>
          );
        })}
      </nav>

      <div className="px-sm mt-auto py-md">
        <NavLink
          to="/dogs/new"
          className="w-full mb-md bg-primary text-on-primary py-3 rounded-lg font-label-md text-label-md flex items-center justify-center gap-sm hover:brightness-110 transition-all"
        >
          <span className="material-symbols-outlined">add</span>
          Check In Dog
        </NavLink>
      </div>
    </aside>
  );
}
