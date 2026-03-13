import React, { createContext, useContext, useReducer, useCallback } from 'react';
import { Book, Author, Genre, booksApi, authorsApi, genresApi } from '../services/api';

interface AppState {
  books: Book[];
  authors: Author[];
  genres: Genre[];
  loading: { books: boolean; authors: boolean; genres: boolean };
  error: string | null;
}

type Action =
  | { type: 'SET_BOOKS'; payload: Book[] }
  | { type: 'SET_AUTHORS'; payload: Author[] }
  | { type: 'SET_GENRES'; payload: Genre[] }
  | { type: 'SET_LOADING'; key: keyof AppState['loading']; value: boolean }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'ADD_BOOK'; payload: Book }
  | { type: 'UPDATE_BOOK'; payload: Book }
  | { type: 'DELETE_BOOK'; payload: number }
  | { type: 'ADD_AUTHOR'; payload: Author }
  | { type: 'UPDATE_AUTHOR'; payload: Author }
  | { type: 'DELETE_AUTHOR'; payload: number }
  | { type: 'ADD_GENRE'; payload: Genre }
  | { type: 'UPDATE_GENRE'; payload: Genre }
  | { type: 'DELETE_GENRE'; payload: number };

function reducer(state: AppState, action: Action): AppState {
  switch (action.type) {
    case 'SET_BOOKS': return { ...state, books: action.payload };
    case 'SET_AUTHORS': return { ...state, authors: action.payload };
    case 'SET_GENRES': return { ...state, genres: action.payload };
    case 'SET_LOADING': return { ...state, loading: { ...state.loading, [action.key]: action.value } };
    case 'SET_ERROR': return { ...state, error: action.payload };
    case 'ADD_BOOK': return { ...state, books: [action.payload, ...state.books] };
    case 'UPDATE_BOOK': return { ...state, books: state.books.map(b => b.id === action.payload.id ? action.payload : b) };
    case 'DELETE_BOOK': return { ...state, books: state.books.filter(b => b.id !== action.payload) };
    case 'ADD_AUTHOR': return { ...state, authors: [action.payload, ...state.authors] };
    case 'UPDATE_AUTHOR': return { ...state, authors: state.authors.map(a => a.id === action.payload.id ? action.payload : a) };
    case 'DELETE_AUTHOR': return { ...state, authors: state.authors.filter(a => a.id !== action.payload) };
    case 'ADD_GENRE': return { ...state, genres: [action.payload, ...state.genres] };
    case 'UPDATE_GENRE': return { ...state, genres: state.genres.map(g => g.id === action.payload.id ? action.payload : g) };
    case 'DELETE_GENRE': return { ...state, genres: state.genres.filter(g => g.id !== action.payload) };
    default: return state;
  }
}

interface BookContextValue {
  state: AppState;
  dispatch: React.Dispatch<Action>;
  fetchBooks: () => Promise<void>;
  fetchAuthors: () => Promise<void>;
  fetchGenres: () => Promise<void>;
  fetchAll: () => Promise<void>;
}

const BookContext = createContext<BookContextValue | null>(null);

export function BookProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(reducer, {
    books: [], authors: [], genres: [],
    loading: { books: false, authors: false, genres: false },
    error: null,
  });

  const fetchBooks = useCallback(async () => {
    dispatch({ type: 'SET_LOADING', key: 'books', value: true });
    try {
      const res = await booksApi.getAll();
      if (res.success) dispatch({ type: 'SET_BOOKS', payload: res.data });
    } catch { dispatch({ type: 'SET_ERROR', payload: 'Failed to fetch books' }); }
    finally { dispatch({ type: 'SET_LOADING', key: 'books', value: false }); }
  }, []);

  const fetchAuthors = useCallback(async () => {
    dispatch({ type: 'SET_LOADING', key: 'authors', value: true });
    try {
      const res = await authorsApi.getAll();
      if (res.success) dispatch({ type: 'SET_AUTHORS', payload: res.data });
    } catch { dispatch({ type: 'SET_ERROR', payload: 'Failed to fetch authors' }); }
    finally { dispatch({ type: 'SET_LOADING', key: 'authors', value: false }); }
  }, []);

  const fetchGenres = useCallback(async () => {
    dispatch({ type: 'SET_LOADING', key: 'genres', value: true });
    try {
      const res = await genresApi.getAll();
      if (res.success) dispatch({ type: 'SET_GENRES', payload: res.data });
    } catch { dispatch({ type: 'SET_ERROR', payload: 'Failed to fetch genres' }); }
    finally { dispatch({ type: 'SET_LOADING', key: 'genres', value: false }); }
  }, []);

  const fetchAll = useCallback(async () => {
    await Promise.all([fetchBooks(), fetchAuthors(), fetchGenres()]);
  }, [fetchBooks, fetchAuthors, fetchGenres]);

  return (
    <BookContext.Provider value={{ state, dispatch, fetchBooks, fetchAuthors, fetchGenres, fetchAll }}>
      {children}
    </BookContext.Provider>
  );
}

export function useBookStore() {
  const ctx = useContext(BookContext);
  if (!ctx) throw new Error('useBookStore must be used within BookProvider');
  return ctx;
}
