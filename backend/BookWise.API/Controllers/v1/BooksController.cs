using BookWise.Application.DTOs.Requests;
using BookWise.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookWise.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService) => _bookService = bookService;

    /// <summary>Get all books with author and genre details</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _bookService.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>Get a book by ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _bookService.GetByIdAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Search books by title or author name</summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string term, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest("Search term is required.");

        var result = await _bookService.SearchAsync(term, ct);
        return Ok(result);
    }

    /// <summary>Create a new book</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBookRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _bookService.CreateAsync(request, ct);
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

        var result = await _bookService.UpdateAsync(id, request, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Delete a book (soft delete)</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _bookService.DeleteAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
