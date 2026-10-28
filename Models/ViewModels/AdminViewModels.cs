namespace PersonalFinanceApp.Models.ViewModels
{
    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsLocked { get; set; }
        public bool IsCurrentAdmin { get; set; }
        public bool IsAdmin { get; set; }
        public int WalletCount { get; set; }
        public int TransactionCount { get; set; }
    }
}
