using BookWise.Application.DTOs.Requests;
using BookWise.Application.DTOs.Responses;

namespace BookWise.Application.Interfaces;

public interface IBookService
{
    Task<ApiResponse<IEnumerable<BookViewModel>>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<ApiResponse<BookViewModel>> GetByIdAsync(int userId, int id, CancellationToken ct = default);
    Task<ApiResponse<BookViewModel>> CreateAsync(int userId, CreateBookRequest request, CancellationToken ct = default);
    Task<ApiResponse<BookViewModel>> ImportRemoteAsync(int userId, ImportRemoteBookRequest request, CancellationToken ct = default);
    Task<ApiResponse<BookViewModel>> UpdateAsync(int userId, int id, UpdateBookRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(int userId, int id, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<BookSummaryViewModel>>> SearchAsync(int userId, string term, CancellationToken ct = default);
}

public interface IRemoteBookSearchService
{
    Task<ApiResponse<IEnumerable<RemoteBookResultViewModel>>> SearchAsync(
        string term,
        IEnumerable<string>? sources = null,
        int limit = 20,
        CancellationToken ct = default);
}

public interface IAuthorService
{
    Task<ApiResponse<IEnumerable<AuthorViewModel>>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<ApiResponse<AuthorWithBooksViewModel>> GetByIdAsync(int userId, int id, CancellationToken ct = default);
    Task<ApiResponse<AuthorViewModel>> CreateAsync(int userId, CreateAuthorRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthorViewModel>> UpdateAsync(int userId, int id, UpdateAuthorRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(int userId, int id, CancellationToken ct = default);
}

public interface IGenreService
{
    Task<ApiResponse<IEnumerable<GenreViewModel>>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<ApiResponse<GenreWithBooksViewModel>> GetByIdAsync(int userId, int id, CancellationToken ct = default);
    Task<ApiResponse<GenreViewModel>> CreateAsync(int userId, CreateGenreRequest request, CancellationToken ct = default);
    Task<ApiResponse<GenreViewModel>> UpdateAsync(int userId, int id, UpdateGenreRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(int userId, int id, CancellationToken ct = default);
}

public interface IAIService
{
    Task<ApiResponse<SynopsisResponse>> GenerateSynopsisAsync(GenerateSynopsisRequest request, CancellationToken ct = default);
    Task<ApiResponse<RecommendationResponse>> GetRecommendationsAsync(int userId, int bookId, CancellationToken ct = default);
    Task<ApiResponse<TrendAnalysisResponse>> AnalyzeTrendsAsync(int userId, CancellationToken ct = default);
    Task<ApiResponse<ChatResponse>> ChatAsync(int userId, ChatRequest request, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<ApiResponse<OtpRequestResponse>> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthTokenResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthTokenResponse>> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthTokenResponse>> LoginWithGoogleCodeAsync(string code, string redirectUri, CancellationToken ct = default);
    Task<ApiResponse<UserViewModel>> MeAsync(int userId, CancellationToken ct = default);
}
