using BookWise.Application.DTOs.Requests;
using BookWise.Application.DTOs.Responses;
using BookWise.Application.Interfaces;
using BookWise.Domain.Entities;
using BookWise.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookWise.Application.Services;

public class AuthorService : IAuthorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthorService> _logger;

    public AuthorService(IUnitOfWork unitOfWork, ILogger<AuthorService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<AuthorViewModel>>> GetAllAsync(CancellationToken ct = default)
    {
        var authors = await _unitOfWork.Authors.GetAllWithBooksAsync(ct);
        var viewModels = authors.Select(a => new AuthorViewModel(
            a.Id, a.Name, a.Biography, a.Nationality, a.BirthDate, a.Books.Count, a.CreatedAt));
        return ApiResponse<IEnumerable<AuthorViewModel>>.Ok(viewModels);
    }

    public async Task<ApiResponse<AuthorWithBooksViewModel>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var author = await _unitOfWork.Authors.GetByIdWithBooksAsync(id, ct);
        if (author is null)
            return ApiResponse<AuthorWithBooksViewModel>.Fail($"Author with ID {id} not found.");

        var vm = new AuthorWithBooksViewModel(
            author.Id, author.Name, author.Biography, author.Nationality, author.BirthDate,
            author.Books.Select(b => new BookSummaryViewModel(b.Id, b.Title, b.CoverImageUrl, author.Name, b.Genre.Name, b.PublicationYear))
        );
        return ApiResponse<AuthorWithBooksViewModel>.Ok(vm);
    }

    public async Task<ApiResponse<AuthorViewModel>> CreateAsync(CreateAuthorRequest request, CancellationToken ct = default)
    {
        var nameExists = await _unitOfWork.Authors.ExistsByNameAsync(request.Name, ct);
        if (nameExists)
            return ApiResponse<AuthorViewModel>.Fail($"Author '{request.Name}' already exists.");

        var author = new Author(request.Name, request.Biography, request.Nationality, request.BirthDate);
        await _unitOfWork.Authors.AddAsync(author, ct);
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation("Author created: {AuthorId} - {Name}", author.Id, author.Name);
        return ApiResponse<AuthorViewModel>.Ok(
            new AuthorViewModel(author.Id, author.Name, author.Biography, author.Nationality, author.BirthDate, 0, author.CreatedAt),
            "Author created successfully.");
    }

    public async Task<ApiResponse<AuthorViewModel>> UpdateAsync(int id, UpdateAuthorRequest request, CancellationToken ct = default)
    {
        var author = await _unitOfWork.Authors.GetByIdWithBooksAsync(id, ct);
        if (author is null)
            return ApiResponse<AuthorViewModel>.Fail($"Author with ID {id} not found.");

        author.Update(request.Name, request.Biography, request.Nationality, request.BirthDate);
        await _unitOfWork.Authors.UpdateAsync(author, ct);
        await _unitOfWork.CommitAsync(ct);

        return ApiResponse<AuthorViewModel>.Ok(
            new AuthorViewModel(author.Id, author.Name, author.Biography, author.Nationality, author.BirthDate, author.Books.Count, author.CreatedAt),
            "Author updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var author = await _unitOfWork.Authors.GetByIdWithBooksAsync(id, ct);
        if (author is null)
            return ApiResponse<bool>.Fail($"Author with ID {id} not found.");

        if (author.Books.Any())
            return ApiResponse<bool>.Fail("Cannot delete author with associated books. Remove books first.");

        await _unitOfWork.Authors.DeleteAsync(author, ct);
        await _unitOfWork.CommitAsync(ct);
        return ApiResponse<bool>.Ok(true, "Author deleted successfully.");
    }
}

public class GenreService : IGenreService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenreService> _logger;

    public GenreService(IUnitOfWork unitOfWork, ILogger<GenreService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<GenreViewModel>>> GetAllAsync(CancellationToken ct = default)
    {
        var genres = await _unitOfWork.Genres.GetAllWithBooksAsync(ct);
        var viewModels = genres.Select(g => new GenreViewModel(g.Id, g.Name, g.Description, g.Books.Count, g.CreatedAt));
        return ApiResponse<IEnumerable<GenreViewModel>>.Ok(viewModels);
    }

    public async Task<ApiResponse<GenreWithBooksViewModel>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var genre = await _unitOfWork.Genres.GetByIdWithBooksAsync(id, ct);
        if (genre is null)
            return ApiResponse<GenreWithBooksViewModel>.Fail($"Genre with ID {id} not found.");

        var vm = new GenreWithBooksViewModel(
            genre.Id, genre.Name, genre.Description,
            genre.Books.Select(b => new BookSummaryViewModel(b.Id, b.Title, b.CoverImageUrl, b.Author.Name, genre.Name, b.PublicationYear))
        );
        return ApiResponse<GenreWithBooksViewModel>.Ok(vm);
    }

    public async Task<ApiResponse<GenreViewModel>> CreateAsync(CreateGenreRequest request, CancellationToken ct = default)
    {
        var nameExists = await _unitOfWork.Genres.ExistsByNameAsync(request.Name, ct);
        if (nameExists)
            return ApiResponse<GenreViewModel>.Fail($"Genre '{request.Name}' already exists.");

        var genre = new Genre(request.Name, request.Description);
        await _unitOfWork.Genres.AddAsync(genre, ct);
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation("Genre created: {GenreId} - {Name}", genre.Id, genre.Name);
        return ApiResponse<GenreViewModel>.Ok(
            new GenreViewModel(genre.Id, genre.Name, genre.Description, 0, genre.CreatedAt),
            "Genre created successfully.");
    }

    public async Task<ApiResponse<GenreViewModel>> UpdateAsync(int id, UpdateGenreRequest request, CancellationToken ct = default)
    {
        var genre = await _unitOfWork.Genres.GetByIdWithBooksAsync(id, ct);
        if (genre is null)
            return ApiResponse<GenreViewModel>.Fail($"Genre with ID {id} not found.");

        genre.Update(request.Name, request.Description);
        await _unitOfWork.Genres.UpdateAsync(genre, ct);
        await _unitOfWork.CommitAsync(ct);

        return ApiResponse<GenreViewModel>.Ok(
            new GenreViewModel(genre.Id, genre.Name, genre.Description, genre.Books.Count, genre.CreatedAt),
            "Genre updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var genre = await _unitOfWork.Genres.GetByIdWithBooksAsync(id, ct);
        if (genre is null)
            return ApiResponse<bool>.Fail($"Genre with ID {id} not found.");

        if (genre.Books.Any())
            return ApiResponse<bool>.Fail("Cannot delete genre with associated books. Remove books first.");

        await _unitOfWork.Genres.DeleteAsync(genre, ct);
        await _unitOfWork.CommitAsync(ct);
        return ApiResponse<bool>.Ok(true, "Genre deleted successfully.");
    }
}
