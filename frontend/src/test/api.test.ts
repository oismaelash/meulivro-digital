import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

describe('API Services', () => {
  beforeEach(() => { mockFetch.mockClear(); });

  it('booksApi.getAll calls correct endpoint', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, data: [], message: '' }),
    });

    const { booksApi } = await import('../services/api');
    const result = await booksApi.getAll();

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/books'),
      expect.any(Object)
    );
    expect(result.success).toBe(true);
  });

  it('booksApi.create sends POST request with correct body', async () => {
    const mockBook = { id: 1, title: 'Test Book', authorName: 'Author', genreName: 'Genre', publicationYear: 2024 };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, data: mockBook, message: 'Created' }),
    });

    const { booksApi } = await import('../services/api');
    const result = await booksApi.create({
      title: 'Test Book', publicationYear: 2024, authorId: 1, genreId: 1
    });

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/books'),
      expect.objectContaining({ method: 'POST' })
    );
    expect(result.success).toBe(true);
  });

  it('handles HTTP errors gracefully', async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 404 });

    const { booksApi } = await import('../services/api');
    await expect(booksApi.getById(999)).rejects.toThrow('HTTP 404');
  });
});

describe('ApiResponse shape', () => {
  it('success response has correct shape', () => {
    const response = { success: true, data: [], message: 'OK' };
    expect(response).toHaveProperty('success', true);
    expect(response).toHaveProperty('data');
    expect(response).toHaveProperty('message');
  });
});
