using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace JournalApp.Services;

// Handles password login and JWT tokens
public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private const string PasswordKey = "journal_password_hash";
    private const string TokenKey = "journal_jwt_token";
    private const string SecretKey = "JournalAppSecretKey2024!@#$%^&*()_+VeryLongSecureKey";
    private bool _isAuthenticated;

    public bool IsAuthenticated => _isAuthenticated;

    public SecurityService(ILogger<SecurityService> logger)
    {
        _logger = logger;
    }

    #region Password Operations

    public async Task<bool> IsPasswordSetAsync()
    {
        try
        {
            var hash = await SecureStorage.GetAsync(PasswordKey);
            return !string.IsNullOrEmpty(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not check password status");
            return false;
        }
    }

    public async Task<bool> SetPasswordAsync(string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty", nameof(password));
            }

            if (password.Length < 4)
            {
                throw new ArgumentException("Password must be at least 4 characters", nameof(password));
            }

            var hash = HashPassword(password);
            await SecureStorage.SetAsync(PasswordKey, hash);
            
            _logger.LogInformation("Password saved successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not save password");
            throw;
        }
    }

    public async Task<bool> ValidatePasswordAsync(string password)
    {
        try
        {
            var storedHash = await SecureStorage.GetAsync(PasswordKey);
            
            if (string.IsNullOrEmpty(storedHash))
                return false;

            var inputHash = HashPassword(password);
            return storedHash == inputHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not validate password");
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        try
        {
            if (!await ValidatePasswordAsync(oldPassword))
            {
                throw new UnauthorizedAccessException("Current password is wrong");
            }

            return await SetPasswordAsync(newPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not change password");
            throw;
        }
    }

    #endregion

    #region Authentication State

    public void Authenticate()
    {
        _isAuthenticated = true;
        _logger.LogInformation("User authenticated via JWT");
    }

    public void Logout()
    {
        _isAuthenticated = false;
        SecureStorage.Remove(TokenKey);
        _logger.LogInformation("User logged out, JWT token removed");
    }

    #endregion

    #region JWT Token Operations

    // Generate a new JWT token for the user
    public async Task<string> GenerateTokenAsync(int userId, string userName)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: "JournalApp",
                audience: "JournalAppUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24), // Token valid for 24 hours
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            
            // Store token securely
            await SecureStorage.SetAsync(TokenKey, tokenString);
            
            _logger.LogInformation("JWT token generated for user: {UserName}", userName);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not generate JWT token");
            throw;
        }
    }

    // Get the current stored JWT token
    public async Task<string?> GetCurrentTokenAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(TokenKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not retrieve JWT token");
            return null;
        }
    }

    // Validate the current JWT token
    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            var token = await GetCurrentTokenAsync();
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No JWT token found");
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "JournalApp",
                ValidateAudience = true,
                ValidAudience = "JournalAppUsers",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for expiration
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            if (validatedToken is JwtSecurityToken jwtToken)
            {
                // Verify the algorithm
                if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid JWT algorithm");
                    return false;
                }

                _logger.LogInformation("JWT token validated successfully");
                _isAuthenticated = true;
                return true;
            }

            return false;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token has expired");
            SecureStorage.Remove(TokenKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT token validation failed");
            return false;
        }
    }

    #endregion

    #region Private Helpers

    private static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}
