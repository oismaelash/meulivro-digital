using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BookWise.Application.DTOs.Requests;
using BookWise.Application.DTOs.Responses;
using BookWise.Application.Interfaces;
using BookWise.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace BookWise.Application.Services;

public class AIService : IAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AIService> _logger;
    private readonly string? _deepSeekApiKey;
    private readonly string? _anthropicApiKey;
    private readonly string _deepSeekModel;
    private readonly string _anthropicModel;
    private readonly double _defaultTemperature;
    private readonly double _jsonTemperature;
    private readonly double _chatTemperature;

    private const string DeepSeekChatCompletionsPath = "chat/completions";
    private const string AnthropicMessagesPath = "v1/messages";

    private record AiTextResult(string Text, string Model);

    public AIService(IHttpClientFactory httpClientFactory, IUnitOfWork unitOfWork,
        IConfiguration configuration, ILogger<AIService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _deepSeekApiKey = configuration["DeepSeek:ApiKey"];
        _anthropicApiKey = configuration["Anthropic:ApiKey"];
        _deepSeekModel = configuration["DeepSeek:Model"] ?? "deepseek-chat";
        _anthropicModel = configuration["Anthropic:Model"] ?? "claude-sonnet-4-20250514";

        _defaultTemperature = ParseDoubleOrDefault(configuration["AI:Temperature:Default"], 1.0);
        _jsonTemperature = ParseDoubleOrDefault(configuration["AI:Temperature:Json"], 0.2);
        _chatTemperature = ParseDoubleOrDefault(configuration["AI:Temperature:Chat"], 1.3);

        if (string.IsNullOrWhiteSpace(_deepSeekApiKey) && string.IsNullOrWhiteSpace(_anthropicApiKey))
            throw new InvalidOperationException("No AI provider API key configured (DeepSeek or Anthropic).");
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

        var result = await CallLlmAsync(prompt, 600, _defaultTemperature, ct);
        if (!result.Success)
            return ApiResponse<SynopsisResponse>.Fail(result.Message!);

        return ApiResponse<SynopsisResponse>.Ok(
            new SynopsisResponse(result.Data!.Text, result.Data!.Model, 600));
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

        var result = await CallLlmAsync(prompt, 800, _jsonTemperature, ct);
        if (!result.Success)
            return ApiResponse<RecommendationResponse>.Fail(result.Message!);

        try
        {
            var json = JsonDocument.Parse(ExtractJson(result.Data!.Text));
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

        var result = await CallLlmAsync(prompt, 600, _jsonTemperature, ct);
        if (!result.Success)
            return ApiResponse<TrendAnalysisResponse>.Fail(result.Message!);

        try
        {
            var json = JsonDocument.Parse(ExtractJson(result.Data!.Text));
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

        var result = await CallChatAsync(systemPrompt, messages, 1000, _chatTemperature, ct);
        if (!result.Success)
            return ApiResponse<ChatResponse>.Fail(result.Message!);

        return ApiResponse<ChatResponse>.Ok(new ChatResponse(result.Data!.Text, result.Data!.Model));
    }

    private async Task<ApiResponse<AiTextResult>> CallLlmAsync(string prompt, int maxTokens, double temperature, CancellationToken ct)
    {
        var deepSeekAttempt = await TryCallDeepSeekAsync(
            new[] { new { role = "user", content = prompt } },
            maxTokens,
            temperature,
            ct
        );

        if (deepSeekAttempt.Success)
            return deepSeekAttempt;

        if (string.IsNullOrWhiteSpace(_anthropicApiKey))
            return ApiResponse<AiTextResult>.Fail(deepSeekAttempt.Message ?? "AI service temporarily unavailable.");

        return await TryCallAnthropicAsync(
            system: null,
            new[] { new { role = "user", content = prompt } },
            maxTokens,
            temperature: null,
            ct
        );
    }

    private async Task<ApiResponse<AiTextResult>> CallChatAsync(
        string systemPrompt,
        List<object> messages,
        int maxTokens,
        double temperature,
        CancellationToken ct)
    {
        var deepSeekMessages = new List<object>(messages.Count + 1)
        {
            new { role = "system", content = systemPrompt }
        };
        deepSeekMessages.AddRange(messages);

        var deepSeekAttempt = await TryCallDeepSeekAsync(
            deepSeekMessages.ToArray(),
            maxTokens,
            temperature,
            ct
        );

        if (deepSeekAttempt.Success)
            return deepSeekAttempt;

        if (string.IsNullOrWhiteSpace(_anthropicApiKey))
            return ApiResponse<AiTextResult>.Fail(deepSeekAttempt.Message ?? "AI chat service unavailable.");

        return await TryCallAnthropicAsync(systemPrompt, messages.ToArray(), maxTokens, temperature: null, ct);
    }

    private async Task<ApiResponse<AiTextResult>> TryCallDeepSeekAsync(
        object[] messages,
        int maxTokens,
        double temperature,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_deepSeekApiKey))
            return ApiResponse<AiTextResult>.Fail("DeepSeek API key not configured.");

        var body = new
        {
            model = _deepSeekModel,
            messages,
            max_tokens = maxTokens,
            temperature,
            stream = false
        };

        try
        {
            var client = _httpClientFactory.CreateClient("DeepSeek");

            using var request = new HttpRequestMessage(HttpMethod.Post, DeepSeekChatCompletionsPath)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _deepSeekApiKey);

            using var response = await client.SendAsync(request, ct);
            var payload = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var msg = TryExtractProviderError(payload) ?? $"DeepSeek request failed ({(int)response.StatusCode}).";
                _logger.LogWarning("DeepSeek API call failed: {StatusCode} {Message}", (int)response.StatusCode, msg);
                return ApiResponse<AiTextResult>.Fail(msg);
            }

            using var doc = JsonDocument.Parse(payload.Trim());
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                var msg = errorElement.TryGetProperty("message", out var m) ? m.GetString() : "DeepSeek request failed.";
                return ApiResponse<AiTextResult>.Fail(msg ?? "DeepSeek request failed.");
            }

            var text = ExtractOpenAiCompatibleContent(doc.RootElement);
            return ApiResponse<AiTextResult>.Ok(new AiTextResult(text, _deepSeekModel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepSeek API call failed");
            return ApiResponse<AiTextResult>.Fail("AI service temporarily unavailable.");
        }
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }

    private async Task<ApiResponse<AiTextResult>> TryCallAnthropicAsync(
        string? system,
        object[] messages,
        int maxTokens,
        double? temperature,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_anthropicApiKey))
            return ApiResponse<AiTextResult>.Fail("Anthropic API key not configured.");

        object body;
        if (system is null)
        {
            body = temperature.HasValue
                ? new
                {
                    model = _anthropicModel,
                    max_tokens = maxTokens,
                    messages,
                    temperature = temperature.Value
                }
                : new
                {
                    model = _anthropicModel,
                    max_tokens = maxTokens,
                    messages
                };
        }
        else
        {
            body = temperature.HasValue
                ? new
                {
                    model = _anthropicModel,
                    max_tokens = maxTokens,
                    system,
                    messages,
                    temperature = temperature.Value
                }
                : new
                {
                    model = _anthropicModel,
                    max_tokens = maxTokens,
                    system,
                    messages
                };
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Anthropic");

            using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicMessagesPath)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.TryAddWithoutValidation("x-api-key", _anthropicApiKey);
            request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");

            using var response = await client.SendAsync(request, ct);
            var payload = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var msg = TryExtractProviderError(payload) ?? $"Anthropic request failed ({(int)response.StatusCode}).";
                _logger.LogWarning("Anthropic API call failed: {StatusCode} {Message}", (int)response.StatusCode, msg);
                return ApiResponse<AiTextResult>.Fail(msg);
            }

            using var doc = JsonDocument.Parse(payload.Trim());
            var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
            return ApiResponse<AiTextResult>.Ok(new AiTextResult(text, _anthropicModel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic API call failed");
            return ApiResponse<AiTextResult>.Fail("AI service temporarily unavailable.");
        }
    }

    private static string ExtractOpenAiCompatibleContent(JsonElement root)
    {
        var choice = root.GetProperty("choices")[0];
        var message = choice.GetProperty("message");
        var content = message.GetProperty("content");

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString() ?? "",
            JsonValueKind.Array => string.Join("", content.EnumerateArray()
                .Select(p => p.TryGetProperty("text", out var t) ? t.GetString() : p.GetString())
                .Where(s => !string.IsNullOrEmpty(s))),
            _ => content.ToString()
        };
    }

    private static string? TryExtractProviderError(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload.Trim());
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.TryGetProperty("message", out var msg))
                    return msg.GetString();
            }

            if (doc.RootElement.TryGetProperty("message", out var topMessage))
                return topMessage.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static double ParseDoubleOrDefault(string? value, double fallback)
        => double.TryParse(value, out var parsed) ? parsed : fallback;
}
