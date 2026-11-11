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

        // 0 = Tiền mặt ngoài đời (Manual Cash), 1 = Ví điện tử / Ngân hàng liên kết (Bank / Digital)
        [Display(Name = "Loại ví")]
        public int WalletType { get; set; } = 0;

        [StringLength(100)]
        [Display(Name = "Tên Ngân hàng / Đơn vị")]
        public string? BankName { get; set; }

        [StringLength(50)]
        [Display(Name = "Số tài khoản / Số thẻ / SĐT")]
        public string? AccountNumber { get; set; }

        // Công tắc 1: Quyền sử dụng ví này cho mục tiêu tiết kiệm
        [Display(Name = "Cho phép trích tiết kiệm")]
        public bool IsSavingsAllowed { get; set; } = false;

        // Công tắc 2: Quyền theo dõi biến động số dư tự động (Open Banking Sync)
        [Display(Name = "Tự động theo dõi biến động số dư")]
        public bool IsSyncEnabled { get; set; } = false;

        // Khóa ngoại liên kết tới tài khoản người dùng sở hữu ví (Issue #15)
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Navigation property (Một ví có nhiều giao dịch)
        public ICollection<Transaction>? Transactions { get; set; }
    }
}
