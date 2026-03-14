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

    public async Task<IEnumerable<Book>> GetAllWithDetailsAsync(int userId, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Where(b => b.UserAccountId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public async Task<Book?> GetByIdWithDetailsAsync(int userId, int id, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserAccountId == userId, ct);

    public async Task<IEnumerable<Book>> GetByAuthorAsync(int userId, int authorId, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Where(b => b.AuthorId == authorId && b.UserAccountId == userId)
            .ToListAsync(ct);

    public async Task<IEnumerable<Book>> GetByGenreAsync(int userId, int genreId, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Where(b => b.GenreId == genreId && b.UserAccountId == userId)
            .ToListAsync(ct);

    public async Task<IEnumerable<Book>> SearchAsync(int userId, string term, CancellationToken ct = default) =>
        await _context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Where(b => b.UserAccountId == userId && (
                        EF.Functions.ILike(b.Title, $"%{term}%") ||
                        EF.Functions.ILike(b.Author.Name, $"%{term}%"))
            )
            .ToListAsync(ct);
}

public class AuthorRepository : BaseRepository<Author>, IAuthorRepository
{
    public AuthorRepository(BookWiseDbContext context) : base(context) { }

    public async Task<IEnumerable<Author>> GetAllWithBooksAsync(int userId, CancellationToken ct = default) =>
        await _context.Authors
            .Include(a => a.Books)
            .Where(a => a.UserAccountId == userId)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<Author?> GetByIdWithBooksAsync(int userId, int id, CancellationToken ct = default) =>
        await _context.Authors
            .Include(a => a.Books).ThenInclude(b => b.Genre)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserAccountId == userId, ct);

    public async Task<bool> ExistsByNameAsync(int userId, string name, CancellationToken ct = default) =>
        await _context.Authors.AnyAsync(a => a.UserAccountId == userId && a.Name.ToLower() == name.ToLower(), ct);

    public async Task<Author?> GetByNameAsync(int userId, string name, CancellationToken ct = default) =>
        await _context.Authors.FirstOrDefaultAsync(a => a.UserAccountId == userId && a.Name.ToLower() == name.ToLower(), ct);
}

public class GenreRepository : BaseRepository<Genre>, IGenreRepository
{
    public GenreRepository(BookWiseDbContext context) : base(context) { }

    public async Task<IEnumerable<Genre>> GetAllWithBooksAsync(int userId, CancellationToken ct = default) =>
        await _context.Genres
            .Include(g => g.Books)
            .Where(g => g.UserAccountId == userId)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

    public async Task<Genre?> GetByIdWithBooksAsync(int userId, int id, CancellationToken ct = default) =>
        await _context.Genres
            .Include(g => g.Books).ThenInclude(b => b.Author)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserAccountId == userId, ct);

    public async Task<bool> ExistsByNameAsync(int userId, string name, CancellationToken ct = default) =>
        await _context.Genres.AnyAsync(g => g.UserAccountId == userId && g.Name.ToLower() == name.ToLower(), ct);

    public async Task<Genre?> GetByNameAsync(int userId, string name, CancellationToken ct = default) =>
        await _context.Genres.FirstOrDefaultAsync(g => g.UserAccountId == userId && g.Name.ToLower() == name.ToLower(), ct);

    public async Task<bool> AnyAsync(int userId, CancellationToken ct = default) =>
        await _context.Genres.AnyAsync(g => g.UserAccountId == userId, ct);
}

public class UserAccountRepository : BaseRepository<UserAccount>, IUserAccountRepository
{
    public UserAccountRepository(BookWiseDbContext context) : base(context) { }

    public async Task<UserAccount?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject, ct);

    public async Task<UserAccount?> GetByPhoneNumberAsync(string phoneNumberE164, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumberE164 == phoneNumberE164, ct);

    public async Task<UserAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLower();
        return await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalized, ct);
    }
}

public class LoginOtpRepository : BaseRepository<LoginOtp>, ILoginOtpRepository
{
    public LoginOtpRepository(BookWiseDbContext context) : base(context) { }

    public async Task<LoginOtp?> GetLatestActiveAsync(string phoneNumberE164, CancellationToken ct = default) =>
        await _context.LoginOtps
            .Where(o => o.PhoneNumberE164 == phoneNumberE164)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly BookWiseDbContext _context;
    public IBookRepository Books { get; }
    public IAuthorRepository Authors { get; }
    public IGenreRepository Genres { get; }
    public IUserAccountRepository Users { get; }
    public ILoginOtpRepository LoginOtps { get; }

    public UnitOfWork(BookWiseDbContext context, IBookRepository books,
        IAuthorRepository authors, IGenreRepository genres, IUserAccountRepository users, ILoginOtpRepository loginOtps)
    {
        _context = context;
        Books = books;
        Authors = authors;
        Genres = genres;
        Users = users;
        LoginOtps = loginOtps;
    }

    public async Task<int> CommitAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
