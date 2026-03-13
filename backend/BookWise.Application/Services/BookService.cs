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

    public async Task<ApiResponse<IEnumerable<BookViewModel>>> GetAllAsync(CancellationToken ct = default)
    {
        var books = await _unitOfWork.Books.GetAllWithDetailsAsync(ct);
        var viewModels = books.Select(MapToViewModel);
        return ApiResponse<IEnumerable<BookViewModel>>.Ok(viewModels);
    }

    public async Task<ApiResponse<BookViewModel>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var book = await _unitOfWork.Books.GetByIdWithDetailsAsync(id, ct);
        if (book is null)
            return ApiResponse<BookViewModel>.Fail($"Book with ID {id} not found.");

        return ApiResponse<BookViewModel>.Ok(MapToViewModel(book));
    }

    public async Task<ApiResponse<BookViewModel>> CreateAsync(CreateBookRequest request, CancellationToken ct = default)
    {
        var authorExists = await _unitOfWork.Authors.ExistsAsync(request.AuthorId, ct);
        if (!authorExists)
            return ApiResponse<BookViewModel>.Fail($"Author with ID {request.AuthorId} not found.");

        var genreExists = await _unitOfWork.Genres.ExistsAsync(request.GenreId, ct);
        if (!genreExists)
            return ApiResponse<BookViewModel>.Fail($"Genre with ID {request.GenreId} not found.");

        var book = new Book(
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

        var created = await _unitOfWork.Books.GetByIdWithDetailsAsync(book.Id, ct);
        _logger.LogInformation("Book created: {BookId} - {Title}", book.Id, book.Title);
        return ApiResponse<BookViewModel>.Ok(MapToViewModel(created!), "Book created successfully.");
    }

    public async Task<ApiResponse<BookViewModel>> UpdateAsync(int id, UpdateBookRequest request, CancellationToken ct = default)
    {
        var book = await _unitOfWork.Books.GetByIdWithDetailsAsync(id, ct);
        if (book is null)
            return ApiResponse<BookViewModel>.Fail($"Book with ID {id} not found.");

        var authorExists = await _unitOfWork.Authors.ExistsAsync(request.AuthorId, ct);
        if (!authorExists)
            return ApiResponse<BookViewModel>.Fail($"Author with ID {request.AuthorId} not found.");

        var genreExists = await _unitOfWork.Genres.ExistsAsync(request.GenreId, ct);
        if (!genreExists)
            return ApiResponse<BookViewModel>.Fail($"Genre with ID {request.GenreId} not found.");

        book.Update(request.Title, request.Description, request.PublicationYear, request.ISBN,
            request.AuthorId, request.GenreId, request.CoverImageUrl);

        await _unitOfWork.Books.UpdateAsync(book, ct);
        await _unitOfWork.CommitAsync(ct);

        var updated = await _unitOfWork.Books.GetByIdWithDetailsAsync(id, ct);
        return ApiResponse<BookViewModel>.Ok(MapToViewModel(updated!), "Book updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(id, ct);
        if (book is null)
            return ApiResponse<bool>.Fail($"Book with ID {id} not found.");

        await _unitOfWork.Books.DeleteAsync(book, ct);
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation("Book deleted: {BookId}", id);
        return ApiResponse<bool>.Ok(true, "Book deleted successfully.");
    }

    public async Task<ApiResponse<IEnumerable<BookSummaryViewModel>>> SearchAsync(string term, CancellationToken ct = default)
    {
        var books = await _unitOfWork.Books.SearchAsync(term, ct);
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
