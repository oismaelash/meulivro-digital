import React, { Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route, NavLink, useLocation } from 'react-router-dom';
import { BookProvider } from './store/BookContext';
import { Toaster } from 'react-hot-toast';
import { BookOpen, Users, Tag, Sparkles, LayoutDashboard, Menu, X } from 'lucide-react';
import './index.css';

const Dashboard = lazy(() => import('./pages/Dashboard'));
const BooksPage = lazy(() => import('./pages/BooksPage'));
const AuthorsPage = lazy(() => import('./pages/AuthorsPage'));
const GenresPage = lazy(() => import('./pages/GenresPage'));
const AIPage = lazy(() => import('./pages/AIPage'));

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, exact: true },
  { to: '/books', label: 'Livros', icon: BookOpen },
  { to: '/authors', label: 'Autores', icon: Users },
  { to: '/genres', label: 'Gêneros', icon: Tag },
  { to: '/ai', label: 'IA Features', icon: Sparkles },
];

function Sidebar() {
  const [open, setOpen] = React.useState(false);
  const location = useLocation();

  return (
    <>
      <button
        className="fixed top-4 left-4 z-50 md:hidden bg-amber-900 p-2 rounded-lg text-amber-100"
        onClick={() => setOpen(!open)}
      >
        {open ? <X size={20} /> : <Menu size={20} />}
      </button>

      <aside className={`
        fixed inset-y-0 left-0 z-40 w-64 bg-stone-950 border-r border-amber-900/30
        transform transition-transform duration-300 md:translate-x-0
        ${open ? 'translate-x-0' : '-translate-x-full'}
      `}>
        <div className="p-6 border-b border-amber-900/30">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-amber-600 rounded-xl flex items-center justify-center">
              <BookOpen size={20} className="text-stone-950" />
            </div>
            <div>
              <h1 className="font-serif text-lg font-bold text-amber-100">BookWise</h1>
              <p className="text-xs text-amber-600/70">Catálogo Inteligente</p>
            </div>
          </div>
        </div>

        <nav className="p-4 space-y-1">
          {navItems.map(({ to, label, icon: Icon, exact }) => (
            <NavLink
              key={to}
              to={to}
              end={exact}
              onClick={() => setOpen(false)}
              className={({ isActive }) => `
                flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-all duration-200
                ${isActive
                  ? 'bg-amber-600 text-stone-950'
                  : 'text-amber-200/60 hover:text-amber-100 hover:bg-amber-900/30'
                }
              `}
            >
              <Icon size={18} />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="absolute bottom-6 left-4 right-4">
          <div className="bg-amber-900/20 border border-amber-900/40 rounded-xl p-4">
            <p className="text-xs text-amber-500 font-medium mb-1">✨ AI Powered</p>
            <p className="text-xs text-amber-600/60">Recomendações, sinopses e análises com Claude AI</p>
          </div>
        </div>
      </aside>

      {open && (
        <div className="fixed inset-0 z-30 bg-black/60 md:hidden" onClick={() => setOpen(false)} />
      )}
    </>
  );
}

function LoadingSpinner() {
  return (
    <div className="flex items-center justify-center h-64">
      <div className="w-8 h-8 border-2 border-amber-600 border-t-transparent rounded-full animate-spin" />
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <BookProvider>
        <Toaster
          position="top-right"
          toastOptions={{
            style: { background: '#1c1917', color: '#fde68a', border: '1px solid #78350f' },
          }}
        />
        <div className="flex min-h-screen bg-stone-950">
          <Sidebar />
          <main className="flex-1 md:ml-64 p-6 md:p-8">
            <Suspense fallback={<LoadingSpinner />}>
              <Routes>
                <Route path="/" element={<Dashboard />} />
                <Route path="/books/*" element={<BooksPage />} />
                <Route path="/authors/*" element={<AuthorsPage />} />
                <Route path="/genres/*" element={<GenresPage />} />
                <Route path="/ai" element={<AIPage />} />
              </Routes>
            </Suspense>
          </main>
        </div>
      </BookProvider>
    </BrowserRouter>
  );
}
