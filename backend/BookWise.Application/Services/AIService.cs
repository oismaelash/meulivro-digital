using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BookWise.Application.DTOs.Requests;
using BookWise.Application.DTOs.Responses;
using BookWise.Application.Interfaces;
using BookWise.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookWise.Application.Services;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AIService> _logger;
    private readonly string _apiKey;
    private const string Model = "claude-sonnet-4-20250514";
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";

    public AIService(HttpClient httpClient, IUnitOfWork unitOfWork,
        IConfiguration configuration, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic API key not configured.");

        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<ApiResponse<SynopsisResponse>> GenerateSynopsisAsync(
        GenerateSynopsisRequest request, CancellationToken ct = default)
    {
        var prompt = $"""
            Generate a compelling book synopsis (2-3 paragraphs) for:
            Title: {request.Title}
            Author: {request.AuthorName}
            Genre: {request.GenreName}
            {(request.PublicationYear.HasValue ? $"Year: {request.PublicationYear}" : "")}
            
            Write in Portuguese (Brazil). Be engaging, avoid spoilers, and capture the essence of the genre.
            Return ONLY the synopsis text, no labels or extra formatting.
            """;

        var result = await CallClaudeAsync(prompt, 600, ct);
        if (!result.Success)
            return ApiResponse<SynopsisResponse>.Fail(result.Message!);

        return ApiResponse<SynopsisResponse>.Ok(
            new SynopsisResponse(result.Data!, Model, 600));
    }

    public async Task<ApiResponse<RecommendationResponse>> GetRecommendationsAsync(
        int bookId, CancellationToken ct = default)
    {
        var book = await _unitOfWork.Books.GetByIdWithDetailsAsync(bookId, ct);
        if (book is null)
            return ApiResponse<RecommendationResponse>.Fail($"Book {bookId} not found.");

        var allBooks = await _unitOfWork.Books.GetAllWithDetailsAsync(ct);
        var catalog = allBooks.Where(b => b.Id != bookId)
            .Select(b => $"- {b.Title} by {b.Author.Name} [{b.Genre.Name}]")
            .Take(50);

        var prompt = $$"""
            Based on the book "{{book.Title}}" by {{book.Author.Name}} (Genre: {{book.Genre.Name}}),
            recommend 5 books from the catalog below that readers would also enjoy.
            
            Available catalog:
            {{string.Join("\n", catalog)}}
            
            Respond ONLY with valid JSON in this exact format:
            {
              "recommendations": [
                {"title": "...", "author": "...", "genre": "...", "reason": "..."}
              ],
              "reasoning": "Brief explanation of the recommendation strategy in Portuguese"
            }
            """;

        var result = await CallClaudeAsync(prompt, 800, ct);
        if (!result.Success)
            return ApiResponse<RecommendationResponse>.Fail(result.Message!);

        try
        {
            var json = JsonDocument.Parse(ExtractJson(result.Data!));
            var recs = json.RootElement.GetProperty("recommendations").EnumerateArray()
                .Select(r => new BookRecommendationItem(
                    r.GetProperty("title").GetString()!,
                    r.GetProperty("author").GetString()!,
                    r.GetProperty("genre").GetString()!,
                    r.GetProperty("reason").GetString()!
                )).ToList();

            var reasoning = json.RootElement.GetProperty("reasoning").GetString()!;
            return ApiResponse<RecommendationResponse>.Ok(new RecommendationResponse(recs, reasoning));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse recommendations JSON");
            return ApiResponse<RecommendationResponse>.Fail("Failed to parse AI recommendations.");
        }
    }

    public async Task<ApiResponse<TrendAnalysisResponse>> AnalyzeTrendsAsync(CancellationToken ct = default)
    {
        var genres = await _unitOfWork.Genres.GetAllWithBooksAsync(ct);
        var totalBooks = genres.Sum(g => g.Books.Count);

        if (totalBooks == 0)
            return ApiResponse<TrendAnalysisResponse>.Fail("No books in the catalog to analyze.");

        var genreData = genres.Select(g => new
        {
            g.Name,
            Count = g.Books.Count,
            Percentage = totalBooks > 0 ? Math.Round((double)g.Books.Count / totalBooks * 100, 1) : 0,
            RecentBooks = g.Books.OrderByDescending(b => b.PublicationYear).Take(3).Select(b => b.Title)
        });

        var dataJson = JsonSerializer.Serialize(genreData);
        var prompt = $$"""
            Analyze the following book catalog genre distribution and provide insights:
            {{dataJson}}
            
            Respond ONLY with valid JSON:
            {
              "trends": [
                {"genreName": "...", "insight": "brief insight in Portuguese"}
              ],
              "summary": "Overall summary of catalog trends in Portuguese (2-3 sentences)"
            }
            """;

        var result = await CallClaudeAsync(prompt, 600, ct);
        if (!result.Success)
            return ApiResponse<TrendAnalysisResponse>.Fail(result.Message!);

        try
        {
            var json = JsonDocument.Parse(ExtractJson(result.Data!));
            var insightsMap = json.RootElement.GetProperty("trends").EnumerateArray()
                .ToDictionary(t => t.GetProperty("genreName").GetString()!, t => t.GetProperty("insight").GetString()!);

            var trends = genreData.Select(g => new GenreTrendItem(
                g.Name, g.Count, g.Percentage,
                insightsMap.GetValueOrDefault(g.Name, "No specific insight available.")
            )).OrderByDescending(t => t.BookCount);

            var summary = json.RootElement.GetProperty("summary").GetString()!;
            return ApiResponse<TrendAnalysisResponse>.Ok(
                new TrendAnalysisResponse(trends, summary, DateTime.UtcNow.ToString("o")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse trend analysis JSON");
            return ApiResponse<TrendAnalysisResponse>.Fail("Failed to parse trend analysis.");
        }
    }

    public async Task<ApiResponse<ChatResponse>> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        var books = await _unitOfWork.Books.GetAllWithDetailsAsync(ct);
        var catalogSummary = string.Join("\n", books.Take(100)
            .Select(b => $"- ID:{b.Id} | {b.Title} | {b.Author.Name} | {b.Genre.Name} | {b.PublicationYear}"));

        var systemPrompt = $"""
            You are BookWise AI, a helpful assistant for a book catalog system.
            You help users find books, get recommendations, and learn about authors and genres.
            Always respond in the same language the user writes in.
            Be conversational, helpful, and enthusiastic about books.
            
            Current catalog:
            {catalogSummary}
            """;

        var messages = new List<object>();

        if (request.History != null)
        {
            foreach (var msg in request.History.TakeLast(10))
                messages.Add(new { role = msg.Role, content = msg.Content });
        }

        messages.Add(new { role = "user", content = request.Message });

        var body = new
        {
            model = Model,
            max_tokens = 1000,
            system = systemPrompt,
            messages
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(AnthropicApiUrl, body, ct);
            var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var reply = data.GetProperty("content")[0].GetProperty("text").GetString()!;
            return ApiResponse<ChatResponse>.Ok(new ChatResponse(reply, Model));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat API call failed");
            return ApiResponse<ChatResponse>.Fail("AI chat service unavailable.");
        }
    }

    private async Task<ApiResponse<string>> CallClaudeAsync(string prompt, int maxTokens, CancellationToken ct)
    {
        var body = new
        {
            model = Model,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = prompt } }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(AnthropicApiUrl, body, ct);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var text = data.GetProperty("content")[0].GetProperty("text").GetString()!;
            return ApiResponse<string>.Ok(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API call failed");
            return ApiResponse<string>.Fail("AI service temporarily unavailable.");
        }
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }
}
