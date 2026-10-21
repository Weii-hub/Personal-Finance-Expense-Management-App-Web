using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalFinanceApp.Models
{
    public class Wallet
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên ví không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số dư ban đầu")]
        public decimal InitialBalance { get; set; } = 0;

        public string Icon { get; set; } = "fa-wallet"; // Icon mặc định

        // Khóa ngoại liên kết tới tài khoản người dùng sở hữu ví (Issue #15)
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Navigation property (Một ví có nhiều giao dịch)
        public ICollection<Transaction>? Transactions { get; set; }
    }
}
