namespace BookWise.Application.DTOs.Responses;

// ViewModels (display-oriented)
public record BookViewModel(
    int Id,
    string Title,
    string? Description,
    int PublicationYear,
    string? ISBN,
    string? CoverImageUrl,
    string AuthorName,
    string GenreName,
    int AuthorId,
    int GenreId,
    DateTime CreatedAt
);

public record BookSummaryViewModel(
    int Id,
    string Title,
    string? CoverImageUrl,
    string AuthorName,
    string GenreName,
    int PublicationYear
);

public record AuthorViewModel(
    int Id,
    string Name,
    string? Biography,
    string? Nationality,
    DateTime? BirthDate,
    int BookCount,
    DateTime CreatedAt
);

public record AuthorWithBooksViewModel(
    int Id,
    string Name,
    string? Biography,
    string? Nationality,
    DateTime? BirthDate,
    IEnumerable<BookSummaryViewModel> Books
);

public record GenreViewModel(
    int Id,
    string Name,
    string? Description,
    int BookCount,
    DateTime CreatedAt
);

public record GenreWithBooksViewModel(
    int Id,
    string Name,
    string? Description,
    IEnumerable<BookSummaryViewModel> Books
);

// AI Response DTOs
public record SynopsisResponse(string Synopsis, string Model, int TokensUsed);

public record RecommendationResponse(
    IEnumerable<BookRecommendationItem> Recommendations,
    string Reasoning
);

public record BookRecommendationItem(
    string Title,
    string Author,
    string Genre,
    string Reason,
    int? ExistingBookId = null
);

public record TrendAnalysisResponse(
    IEnumerable<GenreTrendItem> Trends,
    string Summary,
    string GeneratedAt
);

public record GenreTrendItem(
    string GenreName,
    int BookCount,
    double Percentage,
    string Insight
);

public record ChatResponse(string Reply, string Model);

// Standard API response wrapper
public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message,
    IEnumerable<string>? Errors = null
)
{
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new(true, data, message);

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new(false, default, message, errors);
}

public record PaginatedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
