using BookWise.Application.DTOs.Requests;
using BookWise.Application.Services;
using BookWise.Domain.Entities;
using BookWise.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookWise.Tests.Unit.Services;

public class BookServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IBookRepository> _bookRepoMock;
    private readonly Mock<IAuthorRepository> _authorRepoMock;
    private readonly Mock<IGenreRepository> _genreRepoMock;
    private readonly BookService _sut;

    public BookServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _bookRepoMock = new Mock<IBookRepository>();
        _authorRepoMock = new Mock<IAuthorRepository>();
        _genreRepoMock = new Mock<IGenreRepository>();

        _uowMock.Setup(u => u.Books).Returns(_bookRepoMock.Object);
        _uowMock.Setup(u => u.Authors).Returns(_authorRepoMock.Object);
        _uowMock.Setup(u => u.Genres).Returns(_genreRepoMock.Object);

        _sut = new BookService(_uowMock.Object, Mock.Of<ILogger<BookService>>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookNotFound_ReturnsFailResponse()
    {
        _bookRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync((Book?)null);

        var result = await _sut.GetByIdAsync(1, 999);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorNotFound_ReturnsFailResponse()
    {
        _authorRepoMock.Setup(r => r.GetByIdWithBooksAsync(It.IsAny<int>(), It.IsAny<int>(), default)).ReturnsAsync((Author?)null);

        var request = new CreateBookRequest("Test Book", null, 2024, null, 1, 1);
        var result = await _sut.CreateAsync(1, request);

        Assert.False(result.Success);
        Assert.Contains("Author", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenGenreNotFound_ReturnsFailResponse()
    {
        _authorRepoMock.Setup(r => r.GetByIdWithBooksAsync(It.IsAny<int>(), It.IsAny<int>(), default)).ReturnsAsync(new Author(1, "A", null, null, null));
        _genreRepoMock.Setup(r => r.GetByIdWithBooksAsync(It.IsAny<int>(), It.IsAny<int>(), default)).ReturnsAsync((Genre?)null);

        var request = new CreateBookRequest("Test Book", null, 2024, null, 1, 1);
        var result = await _sut.CreateAsync(1, request);

        Assert.False(result.Success);
        Assert.Contains("Genre", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenBookNotFound_ReturnsFailResponse()
    {
        _bookRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync((Book?)null);

        var result = await _sut.DeleteAsync(1, 999);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSuccessWithData()
    {
        var books = new List<Book>();
        _bookRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<int>(), default)).ReturnsAsync(books);

        var result = await _sut.GetAllAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}
