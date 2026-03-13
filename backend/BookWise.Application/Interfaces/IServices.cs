using BookWise.Application.DTOs.Requests;
using BookWise.Application.DTOs.Responses;

namespace BookWise.Application.Interfaces;

public interface IBookService
{
    Task<ApiResponse<IEnumerable<BookViewModel>>> GetAllAsync(CancellationToken ct = default);
    Task<ApiResponse<BookViewModel>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResponse<BookViewModel>> CreateAsync(CreateBookRequest request, CancellationToken ct = default);
    Task<ApiResponse<BookViewModel>> ImportRemoteAsync(ImportRemoteBookRequest request, CancellationToken ct = default);
    Task<ApiResponse<BookViewModel>> UpdateAsync(int id, UpdateBookRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<BookSummaryViewModel>>> SearchAsync(string term, CancellationToken ct = default);
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
    Task<ApiResponse<IEnumerable<AuthorViewModel>>> GetAllAsync(CancellationToken ct = default);
    Task<ApiResponse<AuthorWithBooksViewModel>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResponse<AuthorViewModel>> CreateAsync(CreateAuthorRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthorViewModel>> UpdateAsync(int id, UpdateAuthorRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IGenreService
{
    Task<ApiResponse<IEnumerable<GenreViewModel>>> GetAllAsync(CancellationToken ct = default);
    Task<ApiResponse<GenreWithBooksViewModel>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResponse<GenreViewModel>> CreateAsync(CreateGenreRequest request, CancellationToken ct = default);
    Task<ApiResponse<GenreViewModel>> UpdateAsync(int id, UpdateGenreRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IAIService
{
    Task<ApiResponse<SynopsisResponse>> GenerateSynopsisAsync(GenerateSynopsisRequest request, CancellationToken ct = default);
    Task<ApiResponse<RecommendationResponse>> GetRecommendationsAsync(int bookId, CancellationToken ct = default);
    Task<ApiResponse<TrendAnalysisResponse>> AnalyzeTrendsAsync(CancellationToken ct = default);
    Task<ApiResponse<ChatResponse>> ChatAsync(ChatRequest request, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<ApiResponse<OtpRequestResponse>> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthTokenResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthTokenResponse>> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<UserViewModel>> MeAsync(int userId, CancellationToken ct = default);
}
