using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace PersonalFinanceApp.Services
{
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<OtpService> _logger;

        public OtpService(IMemoryCache cache, ILogger<OtpService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public string GenerateOtp(string key, TimeSpan? expiry = null)
        {
            // Sinh mã OTP 6 chữ số ngẫu nhiên an toàn bằng Cryptography
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var duration = expiry ?? TimeSpan.FromMinutes(5);

            _cache.Set(key, code, duration);

            _logger.LogInformation("🔐 [OTP GENERATED] Key='{Key}', Code='{Code}', Expiry={Minutes}m", key, code, duration.TotalMinutes);
            return code;
        }

        public bool ValidateOtp(string key, string inputOtp, bool removeOnSuccess = true)
        {
            if (string.IsNullOrWhiteSpace(inputOtp))
            {
                return false;
            }

            if (_cache.TryGetValue(key, out string? storedOtp) && storedOtp != null)
            {
                var isValid = string.Equals(storedOtp.Trim(), inputOtp.Trim(), StringComparison.Ordinal);
                if (isValid && removeOnSuccess)
                {
                    _cache.Remove(key);
                    _logger.LogInformation("✅ [OTP VALIDATED] Key='{Key}' đã xác thực thành công.", key);
                }
                return isValid;
            }

            _logger.LogWarning("⚠️ [OTP INVALID] Key='{Key}' không tồn tại hoặc đã hết hạn.", key);
            return false;
        }

        public string? GetCachedOtp(string key)
        {
            _cache.TryGetValue(key, out string? storedOtp);
            return storedOtp;
        }
    }
}
