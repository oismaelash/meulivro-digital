import { getAccessToken } from '../auth/token';

const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

export class ApiError<TBody = unknown> extends Error {
  status: number;
  body?: TBody;

  constructor(status: number, message: string, body?: TBody) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.body = body;
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = getAccessToken();
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
    ...options,
  });

  if (!res.ok) {
    const contentType = res.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
      let body: any = undefined;
      try {
        body = await res.json();
      } catch {
        body = undefined;
      }

      const message =
        body?.message ||
        (Array.isArray(body?.errors) && body.errors.length ? body.errors.join('\n') : null) ||
        `HTTP ${res.status}`;

      throw new ApiError(res.status, message, body);
    }

    let text = '';
    try {
      text = await res.text();
    } catch {
      text = '';
    }
    throw new ApiError(res.status, text || `HTTP ${res.status}`);
  }

  return res.json();
}

// Types
export interface ApiResponse<T> { success: boolean; data: T; message?: string | null; errors?: string[]; errorCode?: string | null; }
export interface UserViewModel { id: number; email?: string | null; name?: string | null; phoneNumberE164?: string | null; }
export interface AuthTokenResponse { accessToken: string; tokenType: string; expiresInSeconds: number; user: UserViewModel; }
export interface OtpRequestResponse { sent: boolean; }
export interface Book { id: number; title: string; description?: string; publicationYear: number; isbn?: string; coverImageUrl?: string; authorName: string; genreName: string; authorId: number; genreId: number; createdAt: string; }
export interface BookSummary { id: number; title: string; coverImageUrl?: string; authorName: string; genreName: string; publicationYear: number; }
export interface Author { id: number; name: string; biography?: string; nationality?: string; birthDate?: string; bookCount: number; createdAt: string; }
export interface AuthorWithBooks { id: number; name: string; biography?: string; nationality?: string; birthDate?: string; books: BookSummary[]; }
export interface Genre { id: number; name: string; description?: string; bookCount: number; createdAt: string; }
export interface GenreWithBooks { id: number; name: string; description?: string; books: BookSummary[]; }

export interface CreateBookDto { title: string; description?: string; publicationYear: number; isbn?: string; authorId: number; genreId: number; coverImageUrl?: string; }
export interface CreateAuthorDto { name: string; biography?: string; nationality?: string; birthDate?: string; }
export interface CreateGenreDto { name: string; description?: string; }

export interface RemoteBookResult {
  source: string;
  sourceId: string;
  title: string;
  authors: string[];
  publicationYear?: number;
  isbn?: string;
  description?: string;
  coverImageUrl?: string;
}

export interface ImportRemoteBookDto {
  title: string;
  description?: string;
  publicationYear?: number;
  isbn?: string;
  authorName: string;
  genreId: number;
  coverImageUrl?: string;
  source?: string;
  sourceId?: string;
}

// Auth
export const authApi = {
  requestOtp: (destinationNumber: string) =>
    request<ApiResponse<OtpRequestResponse>>('/auth/otp/request', {
      method: 'POST',
      body: JSON.stringify({ destinationNumber }),
    }),
  verifyOtp: (destinationNumber: string, code: string) =>
    request<ApiResponse<AuthTokenResponse>>('/auth/otp/verify', {
      method: 'POST',
      body: JSON.stringify({ destinationNumber, code }),
    }),
  google: (credential: string) =>
    request<ApiResponse<AuthTokenResponse>>('/auth/google', {
      method: 'POST',
      body: JSON.stringify({ credential }),
    }),
  me: () => request<ApiResponse<UserViewModel>>('/auth/me'),
};

// Books
export const booksApi = {
  getAll: () => request<ApiResponse<Book[]>>('/books'),
  getById: (id: number) => request<ApiResponse<Book>>(`/books/${id}`),
  search: (term: string) => request<ApiResponse<BookSummary[]>>(`/books/search?term=${encodeURIComponent(term)}`),
  remoteSearch: (term: string, sources?: string, limit: number = 20) =>
    request<ApiResponse<RemoteBookResult[]>>(`/books/remote-search?term=${encodeURIComponent(term)}${sources ? `&sources=${encodeURIComponent(sources)}` : ''}&limit=${limit}`),
  importRemote: (data: ImportRemoteBookDto) =>
    request<ApiResponse<Book>>('/books/import', { method: 'POST', body: JSON.stringify(data) }),
  create: (data: CreateBookDto) => request<ApiResponse<Book>>('/books', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: CreateBookDto) => request<ApiResponse<Book>>(`/books/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id: number) => request<ApiResponse<boolean>>(`/books/${id}`, { method: 'DELETE' }),
};

// Authors
export const authorsApi = {
  getAll: () => request<ApiResponse<Author[]>>('/authors'),
  getById: (id: number) => request<ApiResponse<AuthorWithBooks>>(`/authors/${id}`),
  create: (data: CreateAuthorDto) => request<ApiResponse<Author>>('/authors', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: CreateAuthorDto) => request<ApiResponse<Author>>(`/authors/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id: number) => request<ApiResponse<boolean>>(`/authors/${id}`, { method: 'DELETE' }),
};

// Genres
export const genresApi = {
  getAll: () => request<ApiResponse<Genre[]>>('/genres'),
  getById: (id: number) => request<ApiResponse<GenreWithBooks>>(`/genres/${id}`),
  create: (data: CreateGenreDto) => request<ApiResponse<Genre>>('/genres', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: CreateGenreDto) => request<ApiResponse<Genre>>(`/genres/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id: number) => request<ApiResponse<boolean>>(`/genres/${id}`, { method: 'DELETE' }),
};

// AI
export const aiApi = {
  generateSynopsis: (data: { title: string; authorName: string; genreName: string; publicationYear?: number }) =>
    request<ApiResponse<{ synopsis: string; model: string }>>('/ai/synopsis', { method: 'POST', body: JSON.stringify(data) }),
  getRecommendations: (bookId: number) =>
    request<ApiResponse<{ recommendations: any[]; reasoning: string }>>(`/ai/recommendations/${bookId}`),
  analyzeTrends: () =>
    request<ApiResponse<{ trends: any[]; summary: string; generatedAt: string }>>('/ai/trends'),
  chat: (message: string, history?: { role: string; content: string }[]) =>
    request<ApiResponse<{ reply: string; model: string }>>('/ai/chat', {
      method: 'POST', body: JSON.stringify({ message, history })
    }),
};
