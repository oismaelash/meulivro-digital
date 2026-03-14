using BookWise.Application.DTOs.Requests;
using BookWise.Application.Services;
using BookWise.Domain.Entities;
using BookWise.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookWise.Tests.Unit.Services;

public class AuthorServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IAuthorRepository> _authorRepoMock;
    private readonly AuthorService _sut;

    public AuthorServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _authorRepoMock = new Mock<IAuthorRepository>();
        _uowMock.Setup(u => u.Authors).Returns(_authorRepoMock.Object);
        _sut = new AuthorService(_uowMock.Object, Mock.Of<ILogger<AuthorService>>());
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ReturnsFailResponse()
    {
        _authorRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<int>(), It.IsAny<string>(), default))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(1, new CreateAuthorRequest("Existing Author", null, null, null));

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorHasBooks_ReturnsFailResponse()
    {
        var author = new Author(1, "Test Author", null, null, null);
        // Adding books via reflection since constructor is protected
        var books = new List<Book> { new Book(1, "Book", null, 2024, null, 1, 1) };
        typeof(Author).GetProperty("Books")!.SetValue(author, books);

        _authorRepoMock.Setup(r => r.GetByIdWithBooksAsync(It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync(author);

        var result = await _sut.DeleteAsync(1, 1);

        Assert.False(result.Success);
        Assert.Contains("books", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsFailResponse()
    {
        _authorRepoMock.Setup(r => r.GetByIdWithBooksAsync(It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync((Author?)null);

        var result = await _sut.GetByIdAsync(1, 999);

        Assert.False(result.Success);
    }
}

public class GenreServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IGenreRepository> _genreRepoMock;
    private readonly GenreService _sut;

    public GenreServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _genreRepoMock = new Mock<IGenreRepository>();
        _uowMock.Setup(u => u.Genres).Returns(_genreRepoMock.Object);
        _sut = new GenreService(_uowMock.Object, Mock.Of<ILogger<GenreService>>());
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ReturnsFailResponse()
    {
        _genreRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<int>(), It.IsAny<string>(), default))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(1, new CreateGenreRequest("Existing Genre", null));

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSuccessResponse()
    {
        _genreRepoMock.Setup(r => r.GetAllWithBooksAsync(It.IsAny<int>(), default))
            .ReturnsAsync(new List<Genre>());

        var result = await _sut.GetAllAsync(1);

        Assert.True(result.Success);
    }
}
