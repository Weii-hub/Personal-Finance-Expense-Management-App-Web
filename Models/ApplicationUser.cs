using Microsoft.AspNetCore.Identity;

namespace PersonalFinanceApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Mã PIN bảo mật 6 số (mã hóa băm PBKDF2/Identity Hasher) để xác thực giao dịch và quyền ví
        public string? SecurityPinHash { get; set; }
    }
}
