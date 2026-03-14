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
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;
    public AuthorsController(IAuthorService authorService) => _authorService = authorService;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _authorService.GetAllAsync(GetUserId(), ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _authorService.GetByIdAsync(GetUserId(), id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuthorRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _authorService.CreateAsync(GetUserId(), request, ct);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAuthorRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _authorService.UpdateAsync(GetUserId(), id, request, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _authorService.DeleteAsync(GetUserId(), id, ct);
        if (result.Success) return Ok(result);
        return result.ErrorCode switch
        {
            "not_found" => NotFound(result),
            "has_related_books" => Conflict(result),
            _ => BadRequest(result)
        };
    }

    private int GetUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(userIdRaw, out var userId) ? userId : 0;
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class GenresController : ControllerBase
{
    private readonly IGenreService _genreService;
    public GenresController(IGenreService genreService) => _genreService = genreService;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _genreService.GetAllAsync(GetUserId(), ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _genreService.GetByIdAsync(GetUserId(), id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGenreRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _genreService.CreateAsync(GetUserId(), request, ct);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGenreRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _genreService.UpdateAsync(GetUserId(), id, request, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _genreService.DeleteAsync(GetUserId(), id, ct);
        if (result.Success) return Ok(result);
        return result.ErrorCode switch
        {
            "not_found" => NotFound(result),
            "has_related_books" => Conflict(result),
            _ => BadRequest(result)
        };
    }

    private int GetUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(userIdRaw, out var userId) ? userId : 0;
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;
    public AIController(IAIService aiService) => _aiService = aiService;

    /// <summary>Generate a synopsis for a book using AI</summary>
    [HttpPost("synopsis")]
    public async Task<IActionResult> GenerateSynopsis([FromBody] GenerateSynopsisRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _aiService.GenerateSynopsisAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(503, result);
    }

    /// <summary>Get AI-powered book recommendations based on a book</summary>
    [HttpGet("recommendations/{bookId:int}")]
    public async Task<IActionResult> GetRecommendations(int bookId, CancellationToken ct)
    {
        var result = await _aiService.GetRecommendationsAsync(GetUserId(), bookId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Analyze genre trends in the catalog</summary>
    [HttpGet("trends")]
    public async Task<IActionResult> AnalyzeTrends(CancellationToken ct)
    {
        var result = await _aiService.AnalyzeTrendsAsync(GetUserId(), ct);
        return result.Success ? Ok(result) : StatusCode(503, result);
    }

    /// <summary>Chat with BookWise AI assistant</summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _aiService.ChatAsync(GetUserId(), request, ct);
        return result.Success ? Ok(result) : StatusCode(503, result);
    }

    private int GetUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(userIdRaw, out var userId) ? userId : 0;
    }
}
