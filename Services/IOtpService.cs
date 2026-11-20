namespace PersonalFinanceApp.Services
{
    public interface IOtpService
    {
        string GenerateOtp(string key, TimeSpan? expiry = null);
        bool ValidateOtp(string key, string inputOtp, bool removeOnSuccess = true);
        string? GetCachedOtp(string key);
    }
}
