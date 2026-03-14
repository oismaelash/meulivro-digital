using BookWise.Domain.Entities;

namespace BookWise.Domain.Interfaces;

public interface IBookRepository : IRepository<Book>
{
    Task<IEnumerable<Book>> GetAllWithDetailsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Book?> GetByIdWithDetailsAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetByAuthorAsync(int userId, int authorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetByGenreAsync(int userId, int genreId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> SearchAsync(int userId, string term, CancellationToken cancellationToken = default);
}

public interface IAuthorRepository : IRepository<Author>
{
    Task<IEnumerable<Author>> GetAllWithBooksAsync(int userId, CancellationToken cancellationToken = default);
    Task<Author?> GetByIdWithBooksAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(int userId, string name, CancellationToken cancellationToken = default);
    Task<Author?> GetByNameAsync(int userId, string name, CancellationToken cancellationToken = default);
}

public interface IGenreRepository : IRepository<Genre>
{
    Task<IEnumerable<Genre>> GetAllWithBooksAsync(int userId, CancellationToken cancellationToken = default);
    Task<Genre?> GetByIdWithBooksAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(int userId, string name, CancellationToken cancellationToken = default);
    Task<Genre?> GetByNameAsync(int userId, string name, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(int userId, CancellationToken cancellationToken = default);
}

public interface IUserAccountRepository : IRepository<UserAccount>
{
    Task<UserAccount?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken = default);
    Task<UserAccount?> GetByPhoneNumberAsync(string phoneNumberE164, CancellationToken cancellationToken = default);
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface ILoginOtpRepository : IRepository<LoginOtp>
{
    Task<LoginOtp?> GetLatestActiveAsync(string phoneNumberE164, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    IBookRepository Books { get; }
    IAuthorRepository Authors { get; }
    IGenreRepository Genres { get; }
    IUserAccountRepository Users { get; }
    ILoginOtpRepository LoginOtps { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
