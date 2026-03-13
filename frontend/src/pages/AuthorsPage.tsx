import { useEffect, useRef, useState } from 'react';
import { Plus, Pencil, Trash2, Users, Tag, X, BookOpen } from 'lucide-react';
import { useBookStore } from '../store/BookContext';
import { authorsApi, genresApi, CreateAuthorDto, CreateGenreDto } from '../services/api';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { useDialogHotkeys } from '../hooks/useDialogHotkeys';
import toast from 'react-hot-toast';

const getErrorMessage = (err: unknown, fallback: string) =>
  err instanceof Error && err.message ? err.message : fallback;

// ===== AUTHORS =====
function AuthorModal({ author, onClose, onSave }: any) {
  const formRef = useRef<HTMLFormElement>(null);
  const [form, setForm] = useState<CreateAuthorDto>(author || { name: '', biography: '', nationality: '', birthDate: '' });

  const handleSubmit = async (e: React.FormEvent) => { e.preventDefault(); await onSave(form); };

  useDialogHotkeys({
    open: true,
    onCancel: onClose,
    onConfirm: () => formRef.current?.requestSubmit(),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div className="bg-stone-900 border border-stone-700 rounded-2xl w-full max-w-md">
        <div className="flex items-center justify-between p-6 border-b border-stone-800">
          <h3 className="font-serif text-lg font-semibold text-amber-100">{author ? 'Editar Autor' : 'Novo Autor'}</h3>
          <button
            type="button"
            onClick={onClose}
            className="text-stone-400 hover:text-amber-100"
            title="Fechar (Esc)"
            aria-label="Fechar (Esc)"
          >
            <X size={20} />
          </button>
        </div>
        <form ref={formRef} onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="text-xs text-stone-400 mb-1 block">Nome *</label>
            <input className="input-field" required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs text-stone-400 mb-1 block">Nacionalidade</label>
              <input className="input-field" value={form.nationality || ''} onChange={e => setForm(f => ({ ...f, nationality: e.target.value }))} />
            </div>
            <div>
              <label className="text-xs text-stone-400 mb-1 block">Nascimento</label>
              <input type="date" className="input-field" value={form.birthDate || ''} onChange={e => setForm(f => ({ ...f, birthDate: e.target.value }))} />
            </div>
          </div>
          <div>
            <label className="text-xs text-stone-400 mb-1 block">Biografia</label>
            <textarea className="input-field min-h-[80px] resize-none" value={form.biography || ''} onChange={e => setForm(f => ({ ...f, biography: e.target.value }))} />
          </div>
          <div className="flex gap-3 pt-2">
            <button type="button" onClick={onClose} className="flex-1 btn-secondary">Cancelar (Esc)</button>
            <button type="submit" className="flex-1 btn-primary">Salvar (Enter · Ctrl/⌘+Enter)</button>
          </div>
        </form>
      </div>
    </div>
  );
}

export function AuthorsPage() {
  const { state, fetchAuthors, dispatch } = useBookStore();
  const [modal, setModal] = useState<{ open: boolean; author?: any }>({ open: false });
  const [deleteDialog, setDeleteDialog] = useState<{ open: boolean; id?: number }>({ open: false });
  const [deleting, setDeleting] = useState(false);

  useEffect(() => { fetchAuthors(); }, [fetchAuthors]);

  const handleSave = async (form: CreateAuthorDto) => {
    try {
      if (modal.author) {
        const res = await authorsApi.update(modal.author.id, form);
        if (res.success) { dispatch({ type: 'UPDATE_AUTHOR', payload: res.data }); toast.success('Autor atualizado!'); }
      } else {
        const res = await authorsApi.create(form);
        if (res.success) { dispatch({ type: 'ADD_AUTHOR', payload: res.data }); toast.success('Autor criado!'); }
        else toast.error(res.message || 'Erro ao salvar');
      }
      setModal({ open: false });
    } catch (err) { toast.error(getErrorMessage(err, 'Erro ao salvar')); }
  };

  const handleDelete = async (id: number) => {
    setDeleteDialog({ open: true, id });
  };

  const confirmDelete = async () => {
    if (!deleteDialog.id || deleting) return;
    setDeleting(true);
    try {
      const res = await authorsApi.delete(deleteDialog.id);
      if (res.success) {
        dispatch({ type: 'DELETE_AUTHOR', payload: deleteDialog.id });
        toast.success('Autor excluído!');
        setDeleteDialog({ open: false });
      } else toast.error(res.message || 'Erro ao excluir');
    } catch (err) { toast.error(getErrorMessage(err, 'Erro ao excluir')); }
    finally { setDeleting(false); }
  };

  return (
    <div className="space-y-6 animate-fadeIn">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-serif font-bold text-amber-100">Autores</h2>
          <p className="text-stone-400 text-sm">{state.authors.length} autores cadastrados</p>
        </div>
        <button onClick={() => setModal({ open: true })} className="btn-primary flex items-center gap-2">
          <Plus size={16} /> Novo Autor
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {state.authors.map(author => (
          <div key={author.id} className="bg-stone-900 border border-stone-800 hover:border-amber-800/40 rounded-xl p-5 group transition-all">
            <div className="flex items-start justify-between mb-3">
              <div className="w-10 h-10 bg-blue-900/30 rounded-full flex items-center justify-center">
                <Users size={16} className="text-blue-400" />
              </div>
              <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <button onClick={() => setModal({ open: true, author })} className="p-1.5 text-stone-400 hover:text-amber-400 hover:bg-stone-800 rounded-lg transition-colors">
                  <Pencil size={13} />
                </button>
                <button onClick={() => handleDelete(author.id)} className="p-1.5 text-stone-400 hover:text-red-400 hover:bg-stone-800 rounded-lg transition-colors">
                  <Trash2 size={13} />
                </button>
              </div>
            </div>
            <h4 className="font-semibold text-amber-100 mb-1">{author.name}</h4>
            {author.nationality && <p className="text-xs text-stone-500 mb-1">{author.nationality}</p>}
            <div className="flex items-center gap-1 text-xs text-stone-500 mt-2">
              <BookOpen size={12} />
              <span>{author.bookCount} {author.bookCount === 1 ? 'livro' : 'livros'}</span>
            </div>
          </div>
        ))}
        {state.authors.length === 0 && (
          <div className="col-span-full text-center py-16">
            <Users className="mx-auto text-stone-700 mb-3" size={40} />
            <p className="text-stone-400">Nenhum autor cadastrado</p>
          </div>
        )}
      </div>

      {modal.open && <AuthorModal author={modal.author} onClose={() => setModal({ open: false })} onSave={handleSave} />}
      <ConfirmDialog
        open={deleteDialog.open}
        title="Excluir autor?"
        description="Essa ação não pode ser desfeita."
        confirmText="Excluir (Enter · Ctrl/⌘+Enter)"
        cancelText="Cancelar (Esc)"
        confirmDisabled={deleting}
        onCancel={() => setDeleteDialog({ open: false })}
        onConfirm={confirmDelete}
      />
    </div>
  );
}

// ===== GENRES =====
function GenreModal({ genre, onClose, onSave }: any) {
  const formRef = useRef<HTMLFormElement>(null);
  const [form, setForm] = useState<CreateGenreDto>(genre || { name: '', description: '' });

  const handleSubmit = async (e: React.FormEvent) => { e.preventDefault(); await onSave(form); };

  useDialogHotkeys({
    open: true,
    onCancel: onClose,
    onConfirm: () => formRef.current?.requestSubmit(),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div className="bg-stone-900 border border-stone-700 rounded-2xl w-full max-w-sm">
        <div className="flex items-center justify-between p-6 border-b border-stone-800">
          <h3 className="font-serif text-lg font-semibold text-amber-100">{genre ? 'Editar Gênero' : 'Novo Gênero'}</h3>
          <button
            type="button"
            onClick={onClose}
            className="text-stone-400 hover:text-amber-100"
            title="Fechar (Esc)"
            aria-label="Fechar (Esc)"
          >
            <X size={20} />
          </button>
        </div>
        <form ref={formRef} onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="text-xs text-stone-400 mb-1 block">Nome *</label>
            <input className="input-field" required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
          </div>
          <div>
            <label className="text-xs text-stone-400 mb-1 block">Descrição</label>
            <textarea className="input-field resize-none" value={form.description || ''} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
          </div>
          <div className="flex gap-3 pt-2">
            <button type="button" onClick={onClose} className="flex-1 btn-secondary">Cancelar (Esc)</button>
            <button type="submit" className="flex-1 btn-primary">Salvar (Enter · Ctrl/⌘+Enter)</button>
          </div>
        </form>
      </div>
    </div>
  );
}

export function GenresPage() {
  const { state, fetchGenres, dispatch } = useBookStore();
  const [modal, setModal] = useState<{ open: boolean; genre?: any }>({ open: false });
  const [deleteDialog, setDeleteDialog] = useState<{ open: boolean; id?: number }>({ open: false });
  const [deleting, setDeleting] = useState(false);

  useEffect(() => { fetchGenres(); }, [fetchGenres]);

  const handleSave = async (form: CreateGenreDto) => {
    try {
      if (modal.genre) {
        const res = await genresApi.update(modal.genre.id, form);
        if (res.success) { dispatch({ type: 'UPDATE_GENRE', payload: res.data }); toast.success('Gênero atualizado!'); }
      } else {
        const res = await genresApi.create(form);
        if (res.success) { dispatch({ type: 'ADD_GENRE', payload: res.data }); toast.success('Gênero criado!'); }
        else toast.error(res.message || 'Erro ao salvar');
      }
      setModal({ open: false });
    } catch (err) { toast.error(getErrorMessage(err, 'Erro ao salvar')); }
  };

  const handleDelete = async (id: number) => {
    setDeleteDialog({ open: true, id });
  };

  const confirmDelete = async () => {
    if (!deleteDialog.id || deleting) return;
    setDeleting(true);
    try {
      const res = await genresApi.delete(deleteDialog.id);
      if (res.success) {
        dispatch({ type: 'DELETE_GENRE', payload: deleteDialog.id });
        toast.success('Gênero excluído!');
        setDeleteDialog({ open: false });
      } else toast.error(res.message || 'Erro ao excluir');
    } catch (err) { toast.error(getErrorMessage(err, 'Erro ao excluir')); }
    finally { setDeleting(false); }
  };

  const colors = ['bg-emerald-900/30 text-emerald-400', 'bg-violet-900/30 text-violet-400', 'bg-rose-900/30 text-rose-400', 'bg-cyan-900/30 text-cyan-400', 'bg-orange-900/30 text-orange-400'];

  return (
    <div className="space-y-6 animate-fadeIn">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-serif font-bold text-amber-100">Gêneros</h2>
          <p className="text-stone-400 text-sm">{state.genres.length} gêneros cadastrados</p>
        </div>
        <button onClick={() => setModal({ open: true })} className="btn-primary flex items-center gap-2">
          <Plus size={16} /> Novo Gênero
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {state.genres.map((genre, i) => (
          <div key={genre.id} className="bg-stone-900 border border-stone-800 hover:border-amber-800/40 rounded-xl p-5 group transition-all">
            <div className="flex items-start justify-between mb-3">
              <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${colors[i % colors.length]}`}>
                <Tag size={16} />
              </div>
              <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <button onClick={() => setModal({ open: true, genre })} className="p-1.5 text-stone-400 hover:text-amber-400 hover:bg-stone-800 rounded-lg transition-colors">
                  <Pencil size={13} />
                </button>
                <button onClick={() => handleDelete(genre.id)} className="p-1.5 text-stone-400 hover:text-red-400 hover:bg-stone-800 rounded-lg transition-colors">
                  <Trash2 size={13} />
                </button>
              </div>
            </div>
            <h4 className="font-semibold text-amber-100 mb-1">{genre.name}</h4>
            {genre.description && <p className="text-xs text-stone-500 line-clamp-2">{genre.description}</p>}
            <p className="text-xs text-stone-600 mt-2">{genre.bookCount} {genre.bookCount === 1 ? 'livro' : 'livros'}</p>
          </div>
        ))}
        {state.genres.length === 0 && (
          <div className="col-span-full text-center py-16">
            <Tag className="mx-auto text-stone-700 mb-3" size={40} />
            <p className="text-stone-400">Nenhum gênero cadastrado</p>
          </div>
        )}
      </div>

      {modal.open && <GenreModal genre={modal.genre} onClose={() => setModal({ open: false })} onSave={handleSave} />}
      <ConfirmDialog
        open={deleteDialog.open}
        title="Excluir gênero?"
        description="Essa ação não pode ser desfeita."
        confirmText="Excluir (Enter · Ctrl/⌘+Enter)"
        cancelText="Cancelar (Esc)"
        confirmDisabled={deleting}
        onCancel={() => setDeleteDialog({ open: false })}
        onConfirm={confirmDelete}
      />
    </div>
  );
}

export default AuthorsPage;
