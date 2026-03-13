using BookWise.Domain.Entities;
using BookWise.Domain.Interfaces;
using BookWise.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace BookWise.Infrastructure.Repositories;

public class BaseRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly BookWiseDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(BookWiseDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _dbSet.FindAsync([id], ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
        await _dbSet.ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        entity.SoftDelete();
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        await _dbSet.AnyAsync(e => e.Id == id, ct);
}

public class BookRepository : BaseRepository<Book>, IBookRepository
{
    public BookRepository(BookWiseDbContext context) : base(context) { }

    public async Task<IEnumerable<Book>> GetAllWithDetailsAsync(CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public async Task<Book?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IEnumerable<Book>> GetByAuthorAsync(int authorId, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Where(b => b.AuthorId == authorId)
            .ToListAsync(ct);

    public async Task<IEnumerable<Book>> GetByGenreAsync(int genreId, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Where(b => b.GenreId == genreId)
            .ToListAsync(ct);

    public async Task<IEnumerable<Book>> SearchAsync(string term, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Where(b => EF.Functions.ILike(b.Title, $"%{term}%") ||
                        EF.Functions.ILike(b.Author.Name, $"%{term}%"))
            .ToListAsync(ct);
}

public class AuthorRepository : BaseRepository<Author>, IAuthorRepository
{
    public AuthorRepository(BookWiseDbContext context) : base(context) { }

    public async Task<IEnumerable<Author>> GetAllWithBooksAsync(CancellationToken ct = default) =>
        await _context.Authors.Include(a => a.Books).OrderBy(a => a.Name).ToListAsync(ct);

    public async Task<Author?> GetByIdWithBooksAsync(int id, CancellationToken ct = default) =>
        await _context.Authors.Include(a => a.Books).ThenInclude(b => b.Genre)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        await _context.Authors.AnyAsync(a => a.Name.ToLower() == name.ToLower(), ct);
}

public class GenreRepository : BaseRepository<Genre>, IGenreRepository
{
    public GenreRepository(BookWiseDbContext context) : base(context) { }

    public async Task<IEnumerable<Genre>> GetAllWithBooksAsync(CancellationToken ct = default) =>
        await _context.Genres.Include(g => g.Books).OrderBy(g => g.Name).ToListAsync(ct);

    public async Task<Genre?> GetByIdWithBooksAsync(int id, CancellationToken ct = default) =>
        await _context.Genres.Include(g => g.Books).ThenInclude(b => b.Author)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        await _context.Genres.AnyAsync(g => g.Name.ToLower() == name.ToLower(), ct);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly BookWiseDbContext _context;
    public IBookRepository Books { get; }
    public IAuthorRepository Authors { get; }
    public IGenreRepository Genres { get; }

    public UnitOfWork(BookWiseDbContext context, IBookRepository books,
        IAuthorRepository authors, IGenreRepository genres)
    {
        _context = context;
        Books = books;
        Authors = authors;
        Genres = genres;
    }

    public async Task<int> CommitAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
