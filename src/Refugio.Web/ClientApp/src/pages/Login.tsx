import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const ok = await login(email, password);
      if (ok) {
        navigate('/', { replace: true });
      } else {
        setError('Invalid email or password.');
      }
    } catch {
      setError('An unexpected error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-background flex items-center justify-center px-margin-mobile">
      <div className="w-full max-w-sm">
        <div className="text-center mb-lg">
          <div className="w-16 h-16 rounded-xl bg-primary flex items-center justify-center mx-auto mb-md">
            <span className="material-symbols-outlined text-on-primary text-4xl" style={{ fontVariationSettings: "'FILL' 1" }}>pets</span>
          </div>
          <h1 className="font-headline-xl text-headline-xl text-primary">Refugio</h1>
          <p className="text-body-sm text-on-surface-variant mt-1">Shelter Management</p>
        </div>

        <div className="bg-surface-container-lowest rounded-xl shadow-soft border border-outline-variant/30 p-lg">
          <h2 className="font-headline-md text-headline-md text-on-surface mb-md">Sign in</h2>

          {error && (
            <div className="mb-md bg-error-container/30 border border-error/30 rounded-lg px-md py-3 text-body-sm text-on-error-container">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-md">
            <div>
              <label className="block text-label-md font-label-md text-on-surface mb-1">Email</label>
              <input
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
                className="w-full px-md py-3 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all"
                placeholder="you@example.com"
              />
            </div>
            <div>
              <label className="block text-label-md font-label-md text-on-surface mb-1">Password</label>
              <input
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                required
                className="w-full px-md py-3 bg-surface-container border border-outline-variant rounded-lg text-body-md focus:ring-2 focus:ring-primary focus:border-primary transition-all"
                placeholder="••••••••"
              />
            </div>
            <button
              type="submit"
              disabled={loading}
              className="w-full bg-primary text-on-primary py-3 rounded-lg font-label-md text-label-md hover:brightness-110 transition-all disabled:opacity-60 flex items-center justify-center gap-sm"
            >
              {loading ? (
                <span className="w-5 h-5 border-2 border-on-primary/30 border-t-on-primary rounded-full animate-spin" />
              ) : null}
              Sign In
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
