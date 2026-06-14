import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function TopBar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const initials = user?.name
    .split(' ').slice(0, 2).map(w => w[0]?.toUpperCase() ?? '').join('') ?? '?';

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <header className="h-16 bg-surface-container-low shadow-sm flex items-center justify-between px-margin-desktop sticky top-0 z-40 border-b border-outline-variant">
      <div className="flex items-center gap-md">
        <div className="relative">
          <span className="absolute inset-y-0 left-3 flex items-center text-on-surface-variant">
            <span className="material-symbols-outlined">search</span>
          </span>
          <input
            className="pl-10 pr-4 py-2 bg-surface-container-high border-none rounded-full w-80 text-body-sm focus:ring-2 focus:ring-primary transition-all"
            placeholder="Buscar..."
            type="text"
            readOnly
          />
        </div>
      </div>

      <div className="flex items-center gap-md">
        <button className="w-10 h-10 flex items-center justify-center rounded-full hover:bg-surface-container-high transition-all text-on-surface-variant">
          <span className="material-symbols-outlined">notifications</span>
        </button>
        <div className="h-8 w-px bg-outline-variant" />

        {/* User dropdown (CSS hover) */}
        <div className="relative group">
          <button className="flex items-center gap-sm px-sm py-1.5 rounded-lg hover:bg-surface-container-high transition-all cursor-pointer">
            <div className="w-8 h-8 rounded-full bg-primary-fixed flex items-center justify-center text-on-primary-fixed font-bold text-sm">
              {initials}
            </div>
            <span className="font-label-md text-label-md text-on-surface">{user?.name}</span>
            <span className="material-symbols-outlined text-on-surface-variant" style={{ fontSize: 18 }}>expand_more</span>
          </button>
          <div className="absolute right-0 top-full mt-1 w-52 bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 invisible group-hover:visible opacity-0 group-hover:opacity-100 transition-all duration-150 z-50">
            <a
              href="/change-password"
              className="flex items-center gap-sm px-md py-3 text-on-surface hover:bg-surface-container rounded-t-xl text-body-sm transition-colors"
              onClick={e => { e.preventDefault(); navigate('/change-password'); }}
            >
              <span className="material-symbols-outlined text-on-surface-variant" style={{ fontSize: 18 }}>lock</span>
              Cambiar contraseña
            </a>
            <div className="h-px bg-outline-variant/30 mx-md" />
            <button
              onClick={handleLogout}
              className="w-full flex items-center gap-sm px-md py-3 text-error hover:bg-error-container/20 rounded-b-xl text-body-sm transition-colors"
            >
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>logout</span>
              Cerrar sesión
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}
