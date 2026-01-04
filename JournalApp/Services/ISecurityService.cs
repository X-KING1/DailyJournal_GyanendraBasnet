namespace JournalApp.Services;

// Handles password and login security
public interface ISecurityService
{
    // Password stuff
    Task<bool> IsPasswordSetAsync();
    Task<bool> SetPasswordAsync(string password);
    Task<bool> ValidatePasswordAsync(string password);
    Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);
    
    // Login state
    bool IsAuthenticated { get; }
    void Authenticate();
    void Logout();
    
    // JWT token for secure session
    Task<string?> GetCurrentTokenAsync();
    Task<bool> ValidateTokenAsync();
    Task<string> GenerateTokenAsync(int userId, string userName);
}
