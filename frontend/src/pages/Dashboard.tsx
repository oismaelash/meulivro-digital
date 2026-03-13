import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { BookOpen, Users, Tag, Sparkles, TrendingUp, Plus } from 'lucide-react';
import { useBookStore } from '../store/BookContext';

function StatCard({ label, value, icon: Icon, color, to }: any) {
  return (
    <Link to={to} className="group bg-stone-900 border border-stone-800 hover:border-amber-700/50 rounded-2xl p-6 transition-all duration-300 hover:shadow-xl hover:shadow-amber-900/10">
      <div className="flex items-start justify-between mb-4">
        <div className={`w-12 h-12 rounded-xl flex items-center justify-center ${color}`}>
          <Icon size={22} />
        </div>
        <span className="text-xs text-amber-600/50 group-hover:text-amber-500 transition-colors">Ver todos →</span>
      </div>
      <p className="text-3xl font-serif font-bold text-amber-100 mb-1">{value}</p>
      <p className="text-sm text-stone-400">{label}</p>
    </Link>
  );
}

export default function Dashboard() {
  const { state, fetchAll } = useBookStore();

  useEffect(() => { fetchAll(); }, [fetchAll]);

  const recentBooks = state.books.slice(0, 5);

  return (
    <div className="space-y-8 animate-fadeIn">
      <div>
        <h2 className="text-3xl font-serif font-bold text-amber-100 mb-1">Bem-vindo ao BookWise</h2>
        <p className="text-stone-400 text-sm">Seu catálogo inteligente de livros, potencializado por IA</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard label="Total de Livros" value={state.books.length} icon={BookOpen} color="bg-amber-600/20 text-amber-400" to="/books" />
        <StatCard label="Autores" value={state.authors.length} icon={Users} color="bg-blue-600/20 text-blue-400" to="/authors" />
        <StatCard label="Gêneros" value={state.genres.length} icon={Tag} color="bg-emerald-600/20 text-emerald-400" to="/genres" />
        <StatCard label="Features de IA" value="4" icon={Sparkles} color="bg-purple-600/20 text-purple-400" to="/ai" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 bg-stone-900 border border-stone-800 rounded-2xl p-6">
          <div className="flex items-center justify-between mb-6">
            <h3 className="font-serif text-lg font-semibold text-amber-100">Livros Recentes</h3>
            <Link to="/books" className="text-xs text-amber-600 hover:text-amber-400 transition-colors">Ver todos</Link>
          </div>

          {state.loading.books ? (
            <div className="space-y-3">
              {[1, 2, 3].map(i => <div key={i} className="h-14 bg-stone-800 rounded-lg animate-pulse" />)}
            </div>
          ) : recentBooks.length === 0 ? (
            <div className="text-center py-12">
              <BookOpen className="mx-auto text-stone-700 mb-3" size={32} />
              <p className="text-stone-500 text-sm">Nenhum livro cadastrado</p>
              <Link to="/books" className="mt-3 inline-flex items-center gap-2 text-xs text-amber-600 hover:text-amber-400">
                <Plus size={14} /> Adicionar primeiro livro
              </Link>
            </div>
          ) : (
            <div className="space-y-3">
              {recentBooks.map(book => (
                <div key={book.id} className="flex items-center gap-4 p-3 bg-stone-800/50 rounded-xl hover:bg-stone-800 transition-colors">
                  <div className="w-10 h-12 bg-amber-900/30 rounded-lg flex items-center justify-center flex-shrink-0">
                    <BookOpen size={16} className="text-amber-600" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-amber-100 truncate">{book.title}</p>
                    <p className="text-xs text-stone-400">{book.authorName} · {book.genreName}</p>
                  </div>
                  <span className="text-xs text-stone-500 flex-shrink-0">{book.publicationYear}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="bg-stone-900 border border-stone-800 rounded-2xl p-6">
          <div className="flex items-center gap-2 mb-6">
            <TrendingUp size={18} className="text-amber-500" />
            <h3 className="font-serif text-lg font-semibold text-amber-100">Top Gêneros</h3>
          </div>
          <div className="space-y-3">
            {state.genres.slice(0, 5).map(genre => (
              <div key={genre.id} className="flex items-center gap-3">
                <div className="flex-1">
                  <div className="flex justify-between text-xs mb-1">
                    <span className="text-stone-300">{genre.name}</span>
                    <span className="text-stone-500">{genre.bookCount}</span>
                  </div>
                  <div className="h-1.5 bg-stone-800 rounded-full overflow-hidden">
                    <div
                      className="h-full bg-gradient-to-r from-amber-600 to-amber-400 rounded-full transition-all duration-700"
                      style={{ width: `${Math.min(100, (genre.bookCount / Math.max(...state.genres.map(g => g.bookCount), 1)) * 100)}%` }}
                    />
                  </div>
                </div>
              </div>
            ))}
            {state.genres.length === 0 && <p className="text-stone-500 text-sm text-center py-4">Sem gêneros cadastrados</p>}
          </div>

          <div className="mt-6 p-4 bg-purple-900/20 border border-purple-800/30 rounded-xl">
            <div className="flex items-center gap-2 mb-2">
              <Sparkles size={14} className="text-purple-400" />
              <span className="text-xs font-medium text-purple-300">IA disponível</span>
            </div>
            <p className="text-xs text-purple-400/70">Analise tendências e obtenha insights do seu catálogo com IA</p>
            <Link to="/ai" className="mt-3 inline-block text-xs text-purple-400 hover:text-purple-300 transition-colors">
              Explorar features →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
