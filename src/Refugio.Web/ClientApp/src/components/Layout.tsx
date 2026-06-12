import { useEffect, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';
import { settingsApi } from '../api/settings';
import type { ShelterSettingsDto } from '../types';

export function Layout() {
  const [settings, setSettings] = useState<ShelterSettingsDto | null>(null);

  useEffect(() => {
    settingsApi.get().then(setSettings).catch(() => setSettings(null));
  }, []);

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      <Sidebar settings={settings} />
      <div className="ml-64 flex-1 flex flex-col min-h-screen overflow-y-auto">
        <TopBar />
        <main className="flex-1">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
