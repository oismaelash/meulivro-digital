using System.ComponentModel.DataAnnotations;

namespace BookWise.Application.DTOs.Requests;

public record CreateBookRequest(
    [Required][StringLength(300, MinimumLength = 1)] string Title,
    string? Description,
    [Range(1000, 2100)] int PublicationYear,
    [StringLength(13)] string? ISBN,
    [Required][Range(1, int.MaxValue)] int AuthorId,
    [Required][Range(1, int.MaxValue)] int GenreId,
    string? CoverImageUrl = null
);

public record ImportRemoteBookRequest(
    [Required][StringLength(300, MinimumLength = 1)] string Title,
    string? Description,
    [Range(1000, 2100)] int? PublicationYear,
    [StringLength(13)] string? ISBN,
    [Required][StringLength(200, MinimumLength = 2)] string AuthorName,
    [Required][Range(1, int.MaxValue)] int GenreId,
    string? CoverImageUrl = null,
    string? Source = null,
    string? SourceId = null
);

public record UpdateBookRequest(
    [Required][StringLength(300, MinimumLength = 1)] string Title,
    string? Description,
    [Range(1000, 2100)] int PublicationYear,
    [StringLength(13)] string? ISBN,
    [Required][Range(1, int.MaxValue)] int AuthorId,
    [Required][Range(1, int.MaxValue)] int GenreId,
    string? CoverImageUrl = null
);

public record CreateAuthorRequest(
    [Required][StringLength(200, MinimumLength = 2)] string Name,
    string? Biography,
    [StringLength(100)] string? Nationality,
    DateTime? BirthDate
);

public record UpdateAuthorRequest(
    [Required][StringLength(200, MinimumLength = 2)] string Name,
    string? Biography,
    [StringLength(100)] string? Nationality,
    DateTime? BirthDate
);

public record CreateGenreRequest(
    [Required][StringLength(100, MinimumLength = 2)] string Name,
    string? Description
);

public record UpdateGenreRequest(
    [Required][StringLength(100, MinimumLength = 2)] string Name,
    string? Description
);

public record GenerateSynopsisRequest(
    [Required] string Title,
    [Required] string AuthorName,
    [Required] string GenreName,
    int? PublicationYear
);

public record ChatRequest(
    [Required] string Message,
    List<ChatMessageDto>? History = null
);

public record ChatMessageDto(string Role, string Content);

public record RequestOtpRequest(
    [Required][StringLength(20, MinimumLength = 8)] string DestinationNumber
);

public record VerifyOtpRequest(
    [Required][StringLength(20, MinimumLength = 8)] string DestinationNumber,
    [Required][StringLength(10, MinimumLength = 4)] string Code
);

public record GoogleLoginRequest(
    [Required] string Credential
);
