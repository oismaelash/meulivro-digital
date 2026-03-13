using BookWise.Domain.Entities;

namespace BookWise.Domain.Interfaces;

public interface IBookRepository : IRepository<Book>
{
    Task<IEnumerable<Book>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<Book?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetByAuthorAsync(int authorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetByGenreAsync(int genreId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> SearchAsync(string term, CancellationToken cancellationToken = default);
}

public interface IAuthorRepository : IRepository<Author>
{
    Task<IEnumerable<Author>> GetAllWithBooksAsync(CancellationToken cancellationToken = default);
    Task<Author?> GetByIdWithBooksAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}

public interface IGenreRepository : IRepository<Genre>
{
    Task<IEnumerable<Genre>> GetAllWithBooksAsync(CancellationToken cancellationToken = default);
    Task<Genre?> GetByIdWithBooksAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    IBookRepository Books { get; }
    IAuthorRepository Authors { get; }
    IGenreRepository Genres { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
