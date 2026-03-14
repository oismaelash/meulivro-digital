using BookWise.Application.DTOs.Requests;
using BookWise.Application.DTOs.Responses;
using BookWise.Application.Interfaces;
using BookWise.Domain.Entities;
using BookWise.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookWise.Application.Services;

public class BookService : IBookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookService> _logger;

    public BookService(IUnitOfWork unitOfWork, ILogger<BookService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<BookViewModel>>> GetAllAsync(int userId, CancellationToken ct = default)
    {
        var books = await _unitOfWork.Books.GetAllWithDetailsAsync(userId, ct);
        var viewModels = books.Select(MapToViewModel);
        return ApiResponse<IEnumerable<BookViewModel>>.Ok(viewModels);
    }

    public async Task<ApiResponse<BookViewModel>> GetByIdAsync(int userId, int id, CancellationToken ct = default)
    {
        var book = await _unitOfWork.Books.GetByIdWithDetailsAsync(userId, id, ct);
        if (book is null)
            return ApiResponse<BookViewModel>.Fail($"Book with ID {id} not found.");

        return ApiResponse<BookViewModel>.Ok(MapToViewModel(book));
    }

    public async Task<ApiResponse<BookViewModel>> CreateAsync(int userId, CreateBookRequest request, CancellationToken ct = default)
    {
        var author = await _unitOfWork.Authors.GetByIdWithBooksAsync(userId, request.AuthorId, ct);
        if (author is null)
            return ApiResponse<BookViewModel>.Fail($"Author with ID {request.AuthorId} not found.");

        var genre = await _unitOfWork.Genres.GetByIdWithBooksAsync(userId, request.GenreId, ct);
        if (genre is null)
            return ApiResponse<BookViewModel>.Fail($"Genre with ID {request.GenreId} not found.");

        var book = new Book(
            userId,
            request.Title,
            request.Description,
            request.PublicationYear,
            request.ISBN,
            request.AuthorId,
            request.GenreId,
            request.CoverImageUrl
        );

        await _unitOfWork.Books.AddAsync(book, ct);
        await _unitOfWork.CommitAsync(ct);

        var created = await _unitOfWork.Books.GetByIdWithDetailsAsync(userId, book.Id, ct);
        _logger.LogInformation("Book created: {BookId} - {Title}", book.Id, book.Title);
        return ApiResponse<BookViewModel>.Ok(MapToViewModel(created!), "Book created successfully.");
    }

    public async Task<ApiResponse<BookViewModel>> ImportRemoteAsync(int userId, ImportRemoteBookRequest request, CancellationToken ct = default)
    {
        var genre = await _unitOfWork.Genres.GetByIdWithBooksAsync(userId, request.GenreId, ct);
        if (genre is null)
            return ApiResponse<BookViewModel>.Fail($"Genre with ID {request.GenreId} not found.");

        var authorName = request.AuthorName.Trim();
        if (authorName.Length < 2)
            return ApiResponse<BookViewModel>.Fail("AuthorName is required.");

        var author = await _unitOfWork.Authors.GetByNameAsync(userId, authorName, ct);
        if (author is null)
        {
            author = new Author(userId, authorName, null, null, null);
            await _unitOfWork.Authors.AddAsync(author, ct);
            await _unitOfWork.CommitAsync(ct);
        }

        var publicationYear = request.PublicationYear ?? DateTime.UtcNow.Year;

        var book = new Book(
            userId,
            request.Title,
            request.Description,
            publicationYear,
            request.ISBN,
            author.Id,
            request.GenreId,
            request.CoverImageUrl
        );

        await _unitOfWork.Books.AddAsync(book, ct);
        await _unitOfWork.CommitAsync(ct);

        var created = await _unitOfWork.Books.GetByIdWithDetailsAsync(userId, book.Id, ct);
        _logger.LogInformation("Book imported: {BookId} - {Title} ({Source})", book.Id, book.Title, request.Source);
        return ApiResponse<BookViewModel>.Ok(MapToViewModel(created!), "Book imported successfully.");
    }

    public async Task<ApiResponse<BookViewModel>> UpdateAsync(int userId, int id, UpdateBookRequest request, CancellationToken ct = default)
    {
        var book = await _unitOfWork.Books.GetByIdWithDetailsAsync(userId, id, ct);
        if (book is null)
            return ApiResponse<BookViewModel>.Fail($"Book with ID {id} not found.");

        var author = await _unitOfWork.Authors.GetByIdWithBooksAsync(userId, request.AuthorId, ct);
        if (author is null)
            return ApiResponse<BookViewModel>.Fail($"Author with ID {request.AuthorId} not found.");

        var genre = await _unitOfWork.Genres.GetByIdWithBooksAsync(userId, request.GenreId, ct);
        if (genre is null)
            return ApiResponse<BookViewModel>.Fail($"Genre with ID {request.GenreId} not found.");

        book.Update(request.Title, request.Description, request.PublicationYear, request.ISBN,
            request.AuthorId, request.GenreId, request.CoverImageUrl);

        await _unitOfWork.Books.UpdateAsync(book, ct);
        await _unitOfWork.CommitAsync(ct);

        var updated = await _unitOfWork.Books.GetByIdWithDetailsAsync(userId, id, ct);
        return ApiResponse<BookViewModel>.Ok(MapToViewModel(updated!), "Book updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int userId, int id, CancellationToken ct = default)
    {
        var book = await _unitOfWork.Books.GetByIdWithDetailsAsync(userId, id, ct);
        if (book is null)
            return ApiResponse<bool>.Fail($"Book with ID {id} not found.");

        await _unitOfWork.Books.DeleteAsync(book, ct);
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation("Book deleted: {BookId}", id);
        return ApiResponse<bool>.Ok(true, "Book deleted successfully.");
    }

    public async Task<ApiResponse<IEnumerable<BookSummaryViewModel>>> SearchAsync(int userId, string term, CancellationToken ct = default)
    {
        var books = await _unitOfWork.Books.SearchAsync(userId, term, ct);
        var viewModels = books.Select(b => new BookSummaryViewModel(
            b.Id, b.Title, b.CoverImageUrl, b.Author.Name, b.Genre.Name, b.PublicationYear));
        return ApiResponse<IEnumerable<BookSummaryViewModel>>.Ok(viewModels);
    }

    private static BookViewModel MapToViewModel(Book book) => new(
        book.Id, book.Title, book.Description, book.PublicationYear,
        book.ISBN, book.CoverImageUrl, book.Author.Name, book.Genre.Name,
        book.AuthorId, book.GenreId, book.CreatedAt
    );
}
