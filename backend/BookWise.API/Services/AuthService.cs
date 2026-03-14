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
        await EnsureDefaultGenresAsync(user.Id, ct);
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
            try
            {
                var unvalidated = new JwtSecurityTokenHandler().ReadJwtToken(request.Credential);
                var aud = unvalidated.Audiences?.FirstOrDefault();
                var iss = unvalidated.Issuer;

                if (!string.IsNullOrWhiteSpace(aud) && !string.Equals(aud, clientId, StringComparison.Ordinal))
                    return ApiResponse<AuthTokenResponse>.Fail("Token do Google inválido (audience não confere).", errorCode: "GOOGLE_AUDIENCE_MISMATCH");

                if (!string.IsNullOrWhiteSpace(iss) && iss is not "https://accounts.google.com" && iss is not "accounts.google.com")
                    return ApiResponse<AuthTokenResponse>.Fail("Token do Google inválido (issuer não confere).", errorCode: "GOOGLE_ISSUER_MISMATCH");
            }
            catch
            {
                return ApiResponse<AuthTokenResponse>.Fail("Token do Google inválido.", errorCode: "GOOGLE_INVALID_TOKEN_FORMAT");
            }

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
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token do Google expirado");
            return ApiResponse<AuthTokenResponse>.Fail("Token do Google expirado.", errorCode: "GOOGLE_TOKEN_EXPIRED");
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            _logger.LogWarning(ex, "Audience inválida no token do Google");
            return ApiResponse<AuthTokenResponse>.Fail("Token do Google inválido (audience não confere).", errorCode: "GOOGLE_AUDIENCE_MISMATCH");
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "Issuer inválido no token do Google");
            return ApiResponse<AuthTokenResponse>.Fail("Token do Google inválido (issuer não confere).", errorCode: "GOOGLE_ISSUER_MISMATCH");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao validar token do Google");
            var tokenInfo = await TryValidateGoogleTokenViaTokenInfoAsync(request.Credential, clientId, ct);
            if (tokenInfo is null)
                return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_INVALID_TOKEN");

            principal = tokenInfo;
        }

        var googleSubject = principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(googleSubject))
            return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_INVALID_TOKEN");

        var email = principal.FindFirstValue("email");
        var name = principal.FindFirstValue("name");

        var user = await _uow.Users.GetByGoogleSubjectAsync(googleSubject, ct);
        if (user is null)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var byEmail = await _uow.Users.GetByEmailAsync(email, ct);
                if (byEmail is not null)
                {
                    if (!string.IsNullOrWhiteSpace(byEmail.GoogleSubject) &&
                        !string.Equals(byEmail.GoogleSubject, googleSubject, StringComparison.Ordinal))
                        return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_EMAIL_ALREADY_LINKED");

                    if (string.IsNullOrWhiteSpace(byEmail.GoogleSubject))
                        byEmail.AttachGoogleSubject(googleSubject);

                    byEmail.UpdateGoogleProfile(email, name);
                    byEmail.MarkLogin();
                    await EnsureDefaultGenresAsync(byEmail.Id, ct);
                    await _uow.CommitAsync(ct);
                    return ApiResponse<AuthTokenResponse>.Ok(IssueToken(byEmail));
                }
            }

            user = UserAccount.CreateFromGoogle(googleSubject, email, name);
            await _uow.Users.AddAsync(user, ct);
        }
        else
        {
            user.UpdateGoogleProfile(email, name);
        }

        user.MarkLogin();
        await EnsureDefaultGenresAsync(user.Id, ct);
        await _uow.CommitAsync(ct);

        return ApiResponse<AuthTokenResponse>.Ok(IssueToken(user));
    }

    public async Task<ApiResponse<AuthTokenResponse>> LoginWithGoogleCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        var clientId = _cfg["Auth:Google:ClientId"];
        var clientSecret = _cfg["Auth:Google:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return ApiResponse<AuthTokenResponse>.Fail("Google não configurado.", errorCode: "GOOGLE_NOT_CONFIGURED");

        if (string.IsNullOrWhiteSpace(code))
            return ApiResponse<AuthTokenResponse>.Fail("Código inválido.", errorCode: "GOOGLE_INVALID_CODE");

        string? idToken;
        string? accessToken;
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            });

            using var res = await client.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
                return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_TOKEN_EXCHANGE_FAILED");

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            idToken = doc.RootElement.TryGetProperty("id_token", out var idTokenProp) ? idTokenProp.GetString() : null;
            accessToken = doc.RootElement.TryGetProperty("access_token", out var accessTokenProp) ? accessTokenProp.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao trocar code por tokens do Google");
            return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_TOKEN_EXCHANGE_FAILED");
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            var userInfo = await TryGetGoogleUserInfoAsync(accessToken, ct);
            if (userInfo is not null)
                return await UpsertGoogleUserAndIssueTokenAsync(userInfo.Value.Sub, userInfo.Value.Email, userInfo.Value.Name, ct);
        }

        if (string.IsNullOrWhiteSpace(idToken))
            return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_MISSING_ID_TOKEN");

        return await LoginWithGoogleAsync(new GoogleLoginRequest(idToken), ct);
    }

    private async Task<ApiResponse<AuthTokenResponse>> UpsertGoogleUserAndIssueTokenAsync(string sub, string? email, string? name, CancellationToken ct)
    {
        var user = await _uow.Users.GetByGoogleSubjectAsync(sub, ct);
        if (user is null)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var byEmail = await _uow.Users.GetByEmailAsync(email, ct);
                if (byEmail is not null)
                {
                    if (!string.IsNullOrWhiteSpace(byEmail.GoogleSubject) &&
                        !string.Equals(byEmail.GoogleSubject, sub, StringComparison.Ordinal))
                        return ApiResponse<AuthTokenResponse>.Fail("Falha ao autenticar com Google.", errorCode: "GOOGLE_EMAIL_ALREADY_LINKED");

                    if (string.IsNullOrWhiteSpace(byEmail.GoogleSubject))
                        byEmail.AttachGoogleSubject(sub);

                    byEmail.UpdateGoogleProfile(email, name);
                    byEmail.MarkLogin();
                    await EnsureDefaultGenresAsync(byEmail.Id, ct);
                    await _uow.CommitAsync(ct);
                    return ApiResponse<AuthTokenResponse>.Ok(IssueToken(byEmail));
                }
            }

            user = UserAccount.CreateFromGoogle(sub, email, name);
            await _uow.Users.AddAsync(user, ct);
        }
        else
        {
            user.UpdateGoogleProfile(email, name);
        }

        user.MarkLogin();
        await EnsureDefaultGenresAsync(user.Id, ct);
        await _uow.CommitAsync(ct);

        return ApiResponse<AuthTokenResponse>.Ok(IssueToken(user));
    }

    private static readonly string[] DefaultGenreNames =
    [
        "Romance", "Fantasia", "Ficção Científica", "Aventura", "Mistério", "Suspense", "Thriller", "Terror", "Drama",
        "Poesia", "Contos", "Clássicos", "Literatura Brasileira", "Literatura Estrangeira", "Infantil", "Jovem Adulto",
        "Quadrinhos", "Mangá", "Não-ficção", "Biografia", "História", "Política", "Filosofia", "Psicologia",
        "Religião e Espiritualidade", "Autoajuda", "Desenvolvimento Pessoal", "Negócios", "Finanças", "Educação",
        "Ciência", "Saúde", "Tecnologia", "Programação", "Artes", "Gastronomia", "Viagem"
    ];

    private async Task EnsureDefaultGenresAsync(int userId, CancellationToken ct)
    {
        var any = await _uow.Genres.AnyAsync(userId, ct);
        if (any) return;

        foreach (var name in DefaultGenreNames)
            await _uow.Genres.AddAsync(new Genre(userId, name, null), ct);
    }

    private async Task<(string Sub, string? Email, string? Name)?> TryGetGoogleUserInfoAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var res = await client.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            var sub = doc.RootElement.TryGetProperty("sub", out var subProp) ? subProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(sub)) return null;

            var email = doc.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            var name = doc.RootElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            return (sub, email, name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao obter userinfo do Google");
            return null;
        }
    }

    private async Task<ClaimsPrincipal?> TryValidateGoogleTokenViaTokenInfoAsync(string credential, string expectedClientId, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(credential)}";
            using var res = await client.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            var aud = doc.RootElement.TryGetProperty("aud", out var audProp) ? audProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(aud) || !string.Equals(aud, expectedClientId, StringComparison.Ordinal))
                return null;

            var sub = doc.RootElement.TryGetProperty("sub", out var subProp) ? subProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(sub)) return null;

            var iss = doc.RootElement.TryGetProperty("iss", out var issProp) ? issProp.GetString() : null;
            if (!string.IsNullOrWhiteSpace(iss) && iss is not "https://accounts.google.com" && iss is not "accounts.google.com")
                return null;

            var email = doc.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            var name = doc.RootElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

            var claims = new List<Claim> { new("sub", sub) };
            if (!string.IsNullOrWhiteSpace(email)) claims.Add(new("email", email));
            if (!string.IsNullOrWhiteSpace(name)) claims.Add(new("name", name));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "google_tokeninfo"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao validar token do Google via tokeninfo");
            return null;
        }
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
