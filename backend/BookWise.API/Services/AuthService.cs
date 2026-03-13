using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookWise.Application.DTOs.Requests;
using BookWise.Application.DTOs.Responses;
using BookWise.Application.Interfaces;
using BookWise.Domain.Entities;
using BookWise.Domain.Interfaces;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BookWise.API.Services;

public class AuthService : IAuthService
{
    private static readonly ConfigurationManager<OpenIdConnectConfiguration> GoogleOidcConfigurationManager =
        new(
            "https://accounts.google.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever()
        );

    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork uow, IConfiguration cfg, IHttpClientFactory httpClientFactory, ILogger<AuthService> logger)
    {
        _uow = uow;
        _cfg = cfg;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ApiResponse<OtpRequestResponse>> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct = default)
    {
        var phone = NormalizeE164(request.DestinationNumber);
        if (phone is null)
            return ApiResponse<OtpRequestResponse>.Fail("Número inválido. Use formato E.164 (ex: +5511999999999).", errorCode: "INVALID_PHONE");

        var now = DateTime.UtcNow;
        var cooldownSeconds = GetInt("Auth:Otp:ResendCooldownSeconds", 30);
        var ttlMinutes = GetInt("Auth:Otp:TtlMinutes", 10);

        var latest = await _uow.LoginOtps.GetLatestActiveAsync(phone, ct);
        if (latest is not null && (now - latest.CreatedAt).TotalSeconds < cooldownSeconds)
            return ApiResponse<OtpRequestResponse>.Fail("Aguarde um pouco antes de solicitar outro código.", errorCode: "OTP_COOLDOWN");

        var pilotApiKey = _cfg["PilotStatus:ApiKey"] ?? _cfg["Auth:PilotStatus:ApiKey"];
        var pilotTemplateId = _cfg["PilotStatus:TemplateId"] ?? _cfg["Auth:PilotStatus:TemplateId"];

        if (string.IsNullOrWhiteSpace(pilotApiKey) || string.IsNullOrWhiteSpace(pilotTemplateId))
            return ApiResponse<OtpRequestResponse>.Fail("Provedor de WhatsApp não configurado.", errorCode: "OTP_PROVIDER_NOT_CONFIGURED");

        var code = GenerateNumericCode(6);
        var codeHash = HashOtp(phone, code, GetOtpPepper());
        var expiresAt = now.AddMinutes(ttlMinutes);

        var pilotMessageId = await SendOtpViaPilotStatusAsync(pilotApiKey, pilotTemplateId, phone, code, ct);
        if (pilotMessageId is null)
            return ApiResponse<OtpRequestResponse>.Fail("Falha ao enviar o código por WhatsApp.", errorCode: "OTP_SEND_FAILED");

        await _uow.LoginOtps.AddAsync(LoginOtp.Create(phone, codeHash, expiresAt, pilotMessageId), ct);
        await _uow.CommitAsync(ct);

        return ApiResponse<OtpRequestResponse>.Ok(new OtpRequestResponse(true), "Código enviado.");
    }

    public async Task<ApiResponse<AuthTokenResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
    {
        var phone = NormalizeE164(request.DestinationNumber);
        if (phone is null)
            return ApiResponse<AuthTokenResponse>.Fail("Número inválido. Use formato E.164 (ex: +5511999999999).", errorCode: "INVALID_PHONE");

        if (string.IsNullOrWhiteSpace(request.Code))
            return ApiResponse<AuthTokenResponse>.Fail("Código inválido.", errorCode: "INVALID_CODE");

        var now = DateTime.UtcNow;
        var maxAttempts = GetInt("Auth:Otp:MaxAttempts", 5);

        var otp = await _uow.LoginOtps.GetLatestActiveAsync(phone, ct);
        if (otp is null || !otp.IsValidNow(now))
            return ApiResponse<AuthTokenResponse>.Fail("Código expirado ou inválido.", errorCode: "OTP_INVALID");

        if (otp.Attempts >= maxAttempts)
        {
            otp.Deactivate();
            await _uow.CommitAsync(ct);
            return ApiResponse<AuthTokenResponse>.Fail("Muitas tentativas. Solicite um novo código.", errorCode: "OTP_TOO_MANY_ATTEMPTS");
        }

        var expectedHash = otp.CodeHash;
        var providedHash = HashOtp(phone, request.Code.Trim(), GetOtpPepper());

        if (!FixedTimeEquals(expectedHash, providedHash))
        {
            otp.MarkAttempt();
            if (otp.Attempts >= maxAttempts) otp.Deactivate();
            await _uow.CommitAsync(ct);
            return ApiResponse<AuthTokenResponse>.Fail("Código expirado ou inválido.", errorCode: "OTP_INVALID");
        }

        otp.Consume();

        var user = await _uow.Users.GetByPhoneNumberAsync(phone, ct);
        if (user is null)
        {
            user = UserAccount.CreateFromPhone(phone);
            await _uow.Users.AddAsync(user, ct);
        }

        user.MarkLogin();
        await _uow.CommitAsync(ct);

        return ApiResponse<AuthTokenResponse>.Ok(IssueToken(user));
    }

    public async Task<ApiResponse<AuthTokenResponse>> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        var clientId = _cfg["Auth:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return ApiResponse<AuthTokenResponse>.Fail("Google ClientId não configurado.", errorCode: "GOOGLE_NOT_CONFIGURED");

        if (string.IsNullOrWhiteSpace(request.Credential))
            return ApiResponse<AuthTokenResponse>.Fail("Credencial inválida.", errorCode: "GOOGLE_INVALID_CREDENTIAL");

        ClaimsPrincipal principal;
        try
        {
            var oidc = await GoogleOidcConfigurationManager.GetConfigurationAsync(ct);
            var tokenHandler = new JwtSecurityTokenHandler();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
                ValidateAudience = true,
                ValidAudiences = [clientId],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = oidc.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            principal = tokenHandler.ValidateToken(request.Credential, parameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao validar token do Google");
            return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_INVALID_TOKEN");
        }

        var googleSubject = principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(googleSubject))
            return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_INVALID_TOKEN");

        var email = principal.FindFirstValue("email");
        var name = principal.FindFirstValue("name");

        var user = await _uow.Users.GetByGoogleSubjectAsync(googleSubject, ct);
        if (user is null)
        {
            user = UserAccount.CreateFromGoogle(googleSubject, email, name);
            await _uow.Users.AddAsync(user, ct);
        }
        else
        {
            user.UpdateGoogleProfile(email, name);
        }

        user.MarkLogin();
        await _uow.CommitAsync(ct);

        return ApiResponse<AuthTokenResponse>.Ok(IssueToken(user));
    }

    public async Task<ApiResponse<UserViewModel>> MeAsync(int userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user is null)
            return ApiResponse<UserViewModel>.Fail("Usuário não encontrado.", errorCode: "USER_NOT_FOUND");

        return ApiResponse<UserViewModel>.Ok(ToViewModel(user));
    }

    private AuthTokenResponse IssueToken(UserAccount user)
    {
        var signingKey = string.IsNullOrWhiteSpace(_cfg["Auth:Jwt:SigningKey"])
            ? "dev_signing_key_change_me_dev_signing_key_change_me"
            : _cfg["Auth:Jwt:SigningKey"]!;
        var issuer = string.IsNullOrWhiteSpace(_cfg["Auth:Jwt:Issuer"]) ? "BookWise" : _cfg["Auth:Jwt:Issuer"]!;
        var audience = string.IsNullOrWhiteSpace(_cfg["Auth:Jwt:Audience"]) ? "BookWise" : _cfg["Auth:Jwt:Audience"]!;
        var expiresInSeconds = GetInt("Auth:Jwt:ExpiresInSeconds", 3600);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new(JwtRegisteredClaimNames.Email, user.Email));
        if (!string.IsNullOrWhiteSpace(user.Name))
            claims.Add(new(JwtRegisteredClaimNames.Name, user.Name));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddSeconds(expiresInSeconds),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthTokenResponse(tokenString, "Bearer", expiresInSeconds, ToViewModel(user));
    }

    private static UserViewModel ToViewModel(UserAccount user) =>
        new(user.Id, user.Email, user.Name, user.PhoneNumberE164);

    private async Task<string?> SendOtpViaPilotStatusAsync(
        string apiKey,
        string templateId,
        string destinationNumberE164,
        string code,
        CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PilotStatus");
            using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/messages/send");
            msg.Headers.TryAddWithoutValidation("x-api-key", apiKey);

            var payload = new
            {
                templateId,
                destinationNumber = destinationNumberE164,
                variables = new Dictionary<string, string>
                {
                    ["code"] = code
                }
            };

            msg.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var res = await client.SendAsync(msg, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("id", out var idProp) && idProp.ValueKind == System.Text.Json.JsonValueKind.String)
                return idProp.GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar OTP via Pilot Status");
        }

        return null;
    }

    private static string? NormalizeE164(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith('+')) return null;

        for (var i = 1; i < trimmed.Length; i++)
        {
            if (!char.IsDigit(trimmed[i])) return null;
        }

        return trimmed.Length is >= 9 and <= 16 ? trimmed : null;
    }

    private int GetInt(string key, int fallback)
    {
        var raw = _cfg[key];
        return int.TryParse(raw, out var v) ? v : fallback;
    }

    private string GetOtpPepper() =>
        string.IsNullOrWhiteSpace(_cfg["Auth:Otp:Pepper"]) ? "dev_otp_pepper_change_me" : _cfg["Auth:Otp:Pepper"]!;

    private static string GenerateNumericCode(int digits)
    {
        var bytes = RandomNumberGenerator.GetBytes(digits);
        var sb = new StringBuilder(digits);
        for (var i = 0; i < digits; i++)
            sb.Append((bytes[i] % 10).ToString());
        return sb.ToString();
    }

    private static string HashOtp(string phoneE164, string code, string pepper)
    {
        var input = $"{pepper}:{phoneE164}:{code}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string hexA, string hexB)
    {
        if (hexA.Length != hexB.Length) return false;
        var a = Convert.FromHexString(hexA);
        var b = Convert.FromHexString(hexB);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
