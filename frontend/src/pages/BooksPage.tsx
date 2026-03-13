import { useEffect, useState } from 'react';
import { Plus, Search, Pencil, Trash2, BookOpen, Sparkles, X } from 'lucide-react';
import { useBookStore } from '../store/BookContext';
import { booksApi, aiApi, CreateBookDto } from '../services/api';
import toast from 'react-hot-toast';

function BookModal({ book, onClose, onSave }: any) {
  const { state } = useBookStore();
  const [form, setForm] = useState<CreateBookDto>(book || {
    title: '', description: '', publicationYear: new Date().getFullYear(),
    isbn: '', authorId: 0, genreId: 0, coverImageUrl: ''
  });
  const [generating, setGenerating] = useState(false);

  const handleGenerateSynopsis = async () => {
    if (!form.title || !form.authorId || !form.genreId) {
      toast.error('Preencha título, autor e gênero primeiro');
      return;
    }
    const author = state.authors.find(a => a.id === form.authorId);
    const genre = state.genres.find(g => g.id === form.genreId);
    setGenerating(true);
    try {
      const res = await aiApi.generateSynopsis({
        title: form.title, authorName: author!.name,
        genreName: genre!.name, publicationYear: form.publicationYear
      });
      if (res.success) setForm(f => ({ ...f, description: res.data.synopsis }));
    } catch { toast.error('Erro ao gerar sinopse'); }
    finally { setGenerating(false); }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSave(form);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div className="bg-stone-900 border border-stone-700 rounded-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b border-stone-800">
          <h3 className="font-serif text-lg font-semibold text-amber-100">{book ? 'Editar Livro' : 'Novo Livro'}</h3>
          <button onClick={onClose} className="text-stone-400 hover:text-amber-100"><X size={20} /></button>
        </div>
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="text-xs text-stone-400 mb-1 block">Título *</label>
            <input className="input-field" required value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs text-stone-400 mb-1 block">Autor *</label>
              <select className="input-field" required value={form.authorId} onChange={e => setForm(f => ({ ...f, authorId: +e.target.value }))}>
                <option value={0}>Selecionar...</option>
                {state.authors.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs text-stone-400 mb-1 block">Gênero *</label>
              <select className="input-field" required value={form.genreId} onChange={e => setForm(f => ({ ...f, genreId: +e.target.value }))}>
                <option value={0}>Selecionar...</option>
                {state.genres.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
              </select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs text-stone-400 mb-1 block">Ano</label>
              <input type="number" className="input-field" value={form.publicationYear} onChange={e => setForm(f => ({ ...f, publicationYear: +e.target.value }))} />
            </div>
            <div>
              <label className="text-xs text-stone-400 mb-1 block">ISBN</label>
              <input className="input-field" value={form.isbn || ''} onChange={e => setForm(f => ({ ...f, isbn: e.target.value }))} />
            </div>
          </div>
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="text-xs text-stone-400">Descrição / Sinopse</label>
              <button type="button" onClick={handleGenerateSynopsis} disabled={generating}
                className="flex items-center gap-1 text-xs text-purple-400 hover:text-purple-300 disabled:opacity-50">
                <Sparkles size={12} /> {generating ? 'Gerando...' : 'Gerar com IA'}
              </button>
            </div>
            <textarea className="input-field min-h-[100px] resize-none" value={form.description || ''}
              onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
          </div>
          <div className="flex gap-3 pt-2">
            <button type="button" onClick={onClose} className="flex-1 btn-secondary">Cancelar</button>
            <button type="submit" className="flex-1 btn-primary">Salvar</button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function BooksPage() {
  const { state, fetchBooks, fetchAuthors, fetchGenres, dispatch } = useBookStore();
  const [search, setSearch] = useState('');
  const [modal, setModal] = useState<{ open: boolean; book?: any }>({ open: false });

  useEffect(() => { fetchBooks(); fetchAuthors(); fetchGenres(); }, [fetchBooks, fetchAuthors, fetchGenres]);

  const filtered = state.books.filter(b =>
    b.title.toLowerCase().includes(search.toLowerCase()) ||
    b.authorName.toLowerCase().includes(search.toLowerCase())
  );

  const handleSave = async (form: CreateBookDto) => {
    try {
      if (modal.book) {
        const res = await booksApi.update(modal.book.id, form);
        if (res.success) { dispatch({ type: 'UPDATE_BOOK', payload: res.data }); toast.success('Livro atualizado!'); }
      } else {
        const res = await booksApi.create(form);
        if (res.success) { dispatch({ type: 'ADD_BOOK', payload: res.data }); toast.success('Livro criado!'); }
        else toast.error(res.message);
      }
      setModal({ open: false });
    } catch { toast.error('Erro ao salvar'); }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Excluir este livro?')) return;
    try {
      const res = await booksApi.delete(id);
      if (res.success) { dispatch({ type: 'DELETE_BOOK', payload: id }); toast.success('Livro excluído!'); }
    } catch { toast.error('Erro ao excluir'); }
  };

  return (
    <div className="space-y-6 animate-fadeIn">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-serif font-bold text-amber-100">Livros</h2>
          <p className="text-stone-400 text-sm">{state.books.length} livros cadastrados</p>
        </div>
        <button onClick={() => setModal({ open: true })} className="btn-primary flex items-center gap-2">
          <Plus size={16} /> Novo Livro
        </button>
      </div>

      <div className="relative">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-stone-500" />
        <input className="input-field pl-10" placeholder="Buscar por título ou autor..." value={search} onChange={e => setSearch(e.target.value)} />
      </div>

      {state.loading.books ? (
        <div className="grid gap-3">
          {[1, 2, 3].map(i => <div key={i} className="h-20 bg-stone-900 rounded-xl animate-pulse" />)}
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16">
          <BookOpen className="mx-auto text-stone-700 mb-3" size={40} />
          <p className="text-stone-400">Nenhum livro encontrado</p>
        </div>
      ) : (
        <div className="grid gap-3">
          {filtered.map(book => (
            <div key={book.id} className="bg-stone-900 border border-stone-800 hover:border-amber-800/40 rounded-xl p-5 flex items-start gap-4 transition-all group">
              <div className="w-12 h-14 bg-amber-900/20 rounded-lg flex items-center justify-center flex-shrink-0">
                <BookOpen size={20} className="text-amber-700" />
              </div>
              <div className="flex-1 min-w-0">
                <h4 className="font-semibold text-amber-100 truncate">{book.title}</h4>
                <p className="text-sm text-stone-400">{book.authorName} · <span className="text-amber-700/70">{book.genreName}</span> · {book.publicationYear}</p>
                {book.description && <p className="text-xs text-stone-500 mt-1 line-clamp-2">{book.description}</p>}
              </div>
              <div className="flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                <button onClick={() => setModal({ open: true, book })} className="p-2 text-stone-400 hover:text-amber-400 hover:bg-stone-800 rounded-lg transition-colors">
                  <Pencil size={15} />
                </button>
                <button onClick={() => handleDelete(book.id)} className="p-2 text-stone-400 hover:text-red-400 hover:bg-stone-800 rounded-lg transition-colors">
                  <Trash2 size={15} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {modal.open && <BookModal book={modal.book} onClose={() => setModal({ open: false })} onSave={handleSave} />}
    </div>
  );
}
