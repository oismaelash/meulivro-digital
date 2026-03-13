import React from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { Rocket, Shield, MessageSquareText } from 'lucide-react';
import { authApi, ApiError } from '../services/api';
import { useAuth } from '../auth/AuthContext';

declare global {
  interface Window {
    google?: any;
  }
}

type Mode = 'otp' | 'google';

export default function LoginPage() {
  const navigate = useNavigate();
  const { setSession, token } = useAuth();

  const [mode, setMode] = React.useState<Mode>('otp');
  const [phone, setPhone] = React.useState('');
  const [code, setCode] = React.useState('');
  const [otpSent, setOtpSent] = React.useState(false);
  const [loading, setLoading] = React.useState(false);

  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;
  const googleBtnRef = React.useRef<HTMLDivElement | null>(null);
  const [googleReady, setGoogleReady] = React.useState(false);

  React.useEffect(() => {
    if (token) navigate('/', { replace: true });
  }, [token, navigate]);

  React.useEffect(() => {
    if (mode !== 'google') return;
    if (!googleClientId) return;
    if (window.google?.accounts?.id) {
      setGoogleReady(true);
      return;
    }

    const existing = document.getElementById('google-identity-script');
    if (existing) return;

    const script = document.createElement('script');
    script.id = 'google-identity-script';
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => setGoogleReady(true);
    script.onerror = () => setGoogleReady(false);
    document.body.appendChild(script);
  }, [mode, googleClientId]);

  React.useEffect(() => {
    if (mode !== 'google') return;
    if (!googleReady) return;
    if (!googleClientId) return;
    if (!googleBtnRef.current) return;
    if (!window.google?.accounts?.id) return;

    googleBtnRef.current.innerHTML = '';

    window.google.accounts.id.initialize({
      client_id: googleClientId,
      callback: async (response: { credential?: string }) => {
        if (!response?.credential) {
          toast.error('Falha ao autenticar com Google.');
          return;
        }

        setLoading(true);
        try {
          const res = await authApi.google(response.credential);
          if (!res.success || !res.data) throw new Error(res.message || 'Falha ao autenticar');
          setSession(res.data);
          toast.success('Login realizado.');
          navigate('/', { replace: true });
        } catch (err) {
          toast.error(err instanceof ApiError ? err.message : 'Falha ao autenticar com Google.');
        } finally {
          setLoading(false);
        }
      },
    });

    window.google.accounts.id.renderButton(googleBtnRef.current, {
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'pill',
      width: 360,
    });
  }, [mode, googleReady, googleClientId, navigate, setSession]);

  async function handleSendOtp(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authApi.requestOtp(phone);
      if (!res.success) throw new Error(res.message || 'Falha ao enviar código');
      setOtpSent(true);
      toast.success('Código enviado no WhatsApp.');
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Falha ao enviar código.');
    } finally {
      setLoading(false);
    }
  }

  async function handleVerifyOtp(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authApi.verifyOtp(phone, code);
      if (!res.success || !res.data) throw new Error(res.message || 'Falha ao validar código');
      setSession(res.data);
      toast.success('Login realizado.');
      navigate('/', { replace: true });
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Falha ao validar código.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen bg-stone-950 flex items-center justify-center p-6">
      <div className="w-full max-w-md">
        <div className="flex items-center justify-center gap-3 mb-8">
          <div className="w-12 h-12 bg-amber-600 rounded-2xl flex items-center justify-center">
            <Rocket size={22} className="text-stone-950" />
          </div>
          <div>
            <h1 className="font-serif text-2xl font-bold text-amber-100">BookWise</h1>
            <p className="text-sm text-amber-600/70">Entrar na sua conta</p>
          </div>
        </div>

        <div className="bg-stone-900/40 border border-amber-900/30 rounded-2xl p-6">
          <div className="flex gap-2 mb-6">
            <button
              type="button"
              onClick={() => setMode('otp')}
              className={`flex-1 px-4 py-2 rounded-xl text-sm font-medium transition ${
                mode === 'otp'
                  ? 'bg-amber-600 text-stone-950'
                  : 'bg-stone-950/40 text-amber-200/70 hover:bg-stone-950/60'
              }`}
            >
              <span className="inline-flex items-center justify-center gap-2">
                <MessageSquareText size={16} />
                WhatsApp
              </span>
            </button>
            <button
              type="button"
              onClick={() => setMode('google')}
              className={`flex-1 px-4 py-2 rounded-xl text-sm font-medium transition ${
                mode === 'google'
                  ? 'bg-amber-600 text-stone-950'
                  : 'bg-stone-950/40 text-amber-200/70 hover:bg-stone-950/60'
              }`}
            >
              <span className="inline-flex items-center justify-center gap-2">
                <Shield size={16} />
                Google
              </span>
            </button>
          </div>

          {mode === 'otp' ? (
            <div>
              <p className="text-sm text-amber-200/70 mb-4">
                Enviaremos um código via WhatsApp para confirmar o acesso.
              </p>

              {!otpSent ? (
                <form onSubmit={handleSendOtp} className="space-y-4">
                  <div>
                    <label className="block text-xs font-medium text-amber-200/70 mb-2">Número (E.164)</label>
                    <input
                      value={phone}
                      onChange={(e) => setPhone(e.target.value)}
                      placeholder="+5511999999999"
                      className="w-full px-4 py-3 rounded-xl bg-stone-950/40 border border-amber-900/30 text-amber-100 placeholder:text-amber-200/30 focus:outline-none focus:ring-2 focus:ring-amber-600/60"
                    />
                  </div>
                  <button
                    disabled={loading}
                    className="w-full px-4 py-3 rounded-xl bg-amber-600 text-stone-950 font-semibold hover:bg-amber-500 disabled:opacity-60 disabled:cursor-not-allowed"
                  >
                    {loading ? 'Enviando...' : 'Enviar código'}
                  </button>
                </form>
              ) : (
                <form onSubmit={handleVerifyOtp} className="space-y-4">
                  <div>
                    <label className="block text-xs font-medium text-amber-200/70 mb-2">Código</label>
                    <input
                      value={code}
                      onChange={(e) => setCode(e.target.value)}
                      placeholder="000000"
                      className="w-full px-4 py-3 rounded-xl bg-stone-950/40 border border-amber-900/30 text-amber-100 placeholder:text-amber-200/30 focus:outline-none focus:ring-2 focus:ring-amber-600/60"
                    />
                  </div>
                  <div className="flex gap-3">
                    <button
                      type="button"
                      onClick={() => {
                        setOtpSent(false);
                        setCode('');
                      }}
                      className="flex-1 px-4 py-3 rounded-xl bg-stone-950/40 text-amber-100 font-semibold hover:bg-stone-950/60 border border-amber-900/30"
                    >
                      Trocar número
                    </button>
                    <button
                      disabled={loading}
                      className="flex-1 px-4 py-3 rounded-xl bg-amber-600 text-stone-950 font-semibold hover:bg-amber-500 disabled:opacity-60 disabled:cursor-not-allowed"
                    >
                      {loading ? 'Validando...' : 'Entrar'}
                    </button>
                  </div>
                </form>
              )}
            </div>
          ) : (
            <div>
              <p className="text-sm text-amber-200/70 mb-4">Use sua conta Google para entrar.</p>

              {!googleClientId ? (
                <div className="text-sm text-amber-200/70 bg-stone-950/40 border border-amber-900/30 rounded-xl p-4">
                  Configure VITE_GOOGLE_CLIENT_ID para habilitar o login do Google.
                </div>
              ) : (
                <div className="flex justify-center">
                  <div
                    ref={googleBtnRef}
                    className={`min-h-[44px] ${loading ? 'opacity-60 pointer-events-none' : ''}`}
                  />
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
