import { useState, useRef, useEffect } from 'react';
import { Sparkles, Send, TrendingUp, BookOpen, Lightbulb, Bot, User, RefreshCw } from 'lucide-react';
import { aiApi } from '../services/api';
import { useBookStore } from '../store/BookContext';
import toast from 'react-hot-toast';

type Tab = 'chat' | 'recommendations' | 'trends';

interface ChatMessage { role: 'user' | 'assistant'; content: string; }

function ChatTab() {
  const [messages, setMessages] = useState<ChatMessage[]>([
    { role: 'assistant', content: '👋 Olá! Sou o BookWise AI. Posso te ajudar a encontrar livros, dar recomendações e responder perguntas sobre o catálogo. O que você gostaria de saber?' }
  ]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages]);

  const send = async () => {
    if (!input.trim() || loading) return;
    const userMsg = input.trim();
    setInput('');
    setMessages(m => [...m, { role: 'user', content: userMsg }]);
    setLoading(true);
    try {
      const history = messages.map(m => ({ role: m.role, content: m.content }));
      const res = await aiApi.chat(userMsg, history);
      if (res.success) setMessages(m => [...m, { role: 'assistant', content: res.data.reply }]);
      else toast.error('Erro no chat');
    } catch { toast.error('Serviço de IA indisponível'); }
    finally { setLoading(false); }
  };

  return (
    <div className="flex flex-col h-[500px]">
      <div className="flex-1 overflow-y-auto space-y-4 mb-4 pr-1">
        {messages.map((msg, i) => (
          <div key={i} className={`flex gap-3 ${msg.role === 'user' ? 'flex-row-reverse' : ''}`}>
            <div className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${msg.role === 'assistant' ? 'bg-purple-800' : 'bg-amber-700'}`}>
              {msg.role === 'assistant' ? <Bot size={14} className="text-purple-200" /> : <User size={14} className="text-amber-100" />}
            </div>
            <div className={`max-w-[80%] px-4 py-3 rounded-2xl text-sm leading-relaxed ${
              msg.role === 'assistant' ? 'bg-stone-800 text-stone-200 rounded-tl-sm' : 'bg-amber-700 text-stone-950 rounded-tr-sm'
            }`}>
              {msg.content}
            </div>
          </div>
        ))}
        {loading && (
          <div className="flex gap-3">
            <div className="w-8 h-8 rounded-full bg-purple-800 flex items-center justify-center">
              <Bot size={14} className="text-purple-200" />
            </div>
            <div className="bg-stone-800 px-4 py-3 rounded-2xl rounded-tl-sm">
              <div className="flex gap-1">
                {[0, 1, 2].map(i => <div key={i} className="w-2 h-2 bg-stone-500 rounded-full animate-bounce" style={{ animationDelay: `${i * 150}ms` }} />)}
              </div>
            </div>
          </div>
        )}
        <div ref={bottomRef} />
      </div>
      <div className="flex gap-3">
        <input
          className="input-field flex-1"
          placeholder="Pergunte sobre livros, autores, recomendações..."
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && send()}
          disabled={loading}
        />
        <button onClick={send} disabled={loading || !input.trim()} className="btn-primary px-4 disabled:opacity-50">
          <Send size={16} />
        </button>
      </div>
    </div>
  );
}

function RecommendationsTab() {
  const { state, fetchBooks } = useBookStore();
  const [selectedBookId, setSelectedBookId] = useState<number>(0);
  const [result, setResult] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => { fetchBooks(); }, [fetchBooks]);

  const fetchRecs = async () => {
    if (!selectedBookId) return;
    setLoading(true);
    try {
      const res = await aiApi.getRecommendations(selectedBookId);
      if (res.success) setResult(res.data);
      else toast.error(res.message);
    } catch { toast.error('Erro ao buscar recomendações'); }
    finally { setLoading(false); }
  };

  return (
    <div className="space-y-6">
      <div className="flex gap-3">
        <select className="input-field flex-1" value={selectedBookId} onChange={e => setSelectedBookId(+e.target.value)}>
          <option value={0}>Selecione um livro base...</option>
          {state.books.map(b => <option key={b.id} value={b.id}>{b.title} — {b.authorName}</option>)}
        </select>
        <button onClick={fetchRecs} disabled={!selectedBookId || loading} className="btn-primary flex items-center gap-2 disabled:opacity-50">
          {loading ? <RefreshCw size={16} className="animate-spin" /> : <Sparkles size={16} />} Recomendar
        </button>
      </div>

      {result && (
        <div className="space-y-4">
          <p className="text-sm text-stone-400 italic bg-stone-800/50 rounded-xl p-3">{result.reasoning}</p>
          <div className="grid gap-3">
            {result.recommendations.map((rec: any, i: number) => (
              <div key={i} className="bg-stone-800 rounded-xl p-4 flex gap-4">
                <div className="w-8 h-8 bg-amber-600 rounded-full flex items-center justify-center text-stone-950 font-bold text-sm flex-shrink-0">{i + 1}</div>
                <div>
                  <p className="font-semibold text-amber-100">{rec.title}</p>
                  <p className="text-xs text-stone-400">{rec.author} · {rec.genre}</p>
                  <p className="text-xs text-stone-500 mt-1">{rec.reason}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function TrendsTab() {
  const [result, setResult] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  const fetchTrends = async () => {
    setLoading(true);
    try {
      const res = await aiApi.analyzeTrends();
      if (res.success) setResult(res.data);
      else toast.error(res.message);
    } catch { toast.error('Erro ao analisar tendências'); }
    finally { setLoading(false); }
  };

  const maxCount = result?.trends ? Math.max(...result.trends.map((t: any) => t.bookCount), 1) : 1;

  return (
    <div className="space-y-6">
      <button onClick={fetchTrends} disabled={loading} className="btn-primary flex items-center gap-2 disabled:opacity-50">
        {loading ? <RefreshCw size={16} className="animate-spin" /> : <TrendingUp size={16} />}
        {loading ? 'Analisando...' : 'Analisar Tendências'}
      </button>

      {result && (
        <div className="space-y-6">
          <div className="bg-purple-900/20 border border-purple-800/30 rounded-xl p-4">
            <div className="flex items-start gap-2">
              <Lightbulb size={16} className="text-purple-400 mt-0.5 flex-shrink-0" />
              <p className="text-sm text-purple-200">{result.summary}</p>
            </div>
          </div>
          <div className="space-y-4">
            {result.trends.map((trend: any) => (
              <div key={trend.genreName} className="bg-stone-800 rounded-xl p-4">
                <div className="flex justify-between text-sm mb-2">
                  <span className="font-medium text-amber-100">{trend.genreName}</span>
                  <span className="text-stone-400">{trend.bookCount} livros · {trend.percentage}%</span>
                </div>
                <div className="h-2 bg-stone-700 rounded-full mb-3">
                  <div className="h-full bg-gradient-to-r from-amber-600 to-amber-400 rounded-full transition-all duration-700"
                    style={{ width: `${(trend.bookCount / maxCount) * 100}%` }} />
                </div>
                <p className="text-xs text-stone-400">{trend.insight}</p>
              </div>
            ))}
          </div>
          <p className="text-xs text-stone-600">Gerado em: {new Date(result.generatedAt).toLocaleString('pt-BR')}</p>
        </div>
      )}
    </div>
  );
}

export default function AIPage() {
  const [tab, setTab] = useState<Tab>('chat');

  const tabs = [
    { id: 'chat' as Tab, label: 'Chat', icon: Bot, desc: 'Converse com a IA sobre livros' },
    { id: 'recommendations' as Tab, label: 'Recomendações', icon: BookOpen, desc: 'Livros similares por IA' },
    { id: 'trends' as Tab, label: 'Tendências', icon: TrendingUp, desc: 'Análise do catálogo' },
  ];

  return (
    <div className="space-y-6 animate-fadeIn">
      <div>
        <h2 className="text-2xl font-serif font-bold text-amber-100 flex items-center gap-2">
          <Sparkles className="text-purple-400" size={24} /> Features de IA
        </h2>
        <p className="text-stone-400 text-sm">Powered by Claude AI (Anthropic)</p>
      </div>

      <div className="grid grid-cols-3 gap-3">
        {tabs.map(t => (
          <button key={t.id} onClick={() => setTab(t.id)}
            className={`p-4 rounded-xl border text-left transition-all ${
              tab === t.id
                ? 'bg-purple-900/30 border-purple-700/60 text-purple-200'
                : 'bg-stone-900 border-stone-800 text-stone-400 hover:border-stone-700'
            }`}>
            <t.icon size={18} className="mb-2" />
            <p className="font-medium text-sm">{t.label}</p>
            <p className="text-xs opacity-60 mt-0.5">{t.desc}</p>
          </button>
        ))}
      </div>

      <div className="bg-stone-900 border border-stone-800 rounded-2xl p-6">
        {tab === 'chat' && <ChatTab />}
        {tab === 'recommendations' && <RecommendationsTab />}
        {tab === 'trends' && <TrendsTab />}
      </div>
    </div>
  );
}
