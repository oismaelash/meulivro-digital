using BookWise.Application.DTOs.Requests;
using BookWise.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookWise.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IRemoteBookSearchService _remoteBookSearchService;

    public BooksController(IBookService bookService, IRemoteBookSearchService remoteBookSearchService)
    {
        _bookService = bookService;
        _remoteBookSearchService = remoteBookSearchService;
    }

    /// <summary>Get all books with author and genre details</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _bookService.GetAllAsync(GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Get a book by ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _bookService.GetByIdAsync(GetUserId(), id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Search books by title or author name</summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string term, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest("Search term is required.");

        var result = await _bookService.SearchAsync(GetUserId(), term, ct);
        return Ok(result);
    }

    /// <summary>Search remote providers (Google Books, Open Library)</summary>
    [HttpGet("remote-search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoteSearch(
        [FromQuery] string term,
        [FromQuery] string? sources,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest("Search term is required.");

        var parsedSources = string.IsNullOrWhiteSpace(sources)
            ? null
            : sources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var result = await _remoteBookSearchService.SearchAsync(term, parsedSources, limit <= 0 ? 20 : limit, ct);
        return Ok(result);
    }

    /// <summary>Import a remote book (creates the author if needed)</summary>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import([FromBody] ImportRemoteBookRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _bookService.ImportRemoteAsync(GetUserId(), request, ct);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>Create a new book</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBookRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _bookService.CreateAsync(GetUserId(), request, ct);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>Update an existing book</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBookRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _bookService.UpdateAsync(GetUserId(), id, request, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Delete a book (soft delete)</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _bookService.DeleteAsync(GetUserId(), id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    private int GetUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(userIdRaw, out var userId) ? userId : 0;
    }
}
