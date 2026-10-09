using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalFinanceApp.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(1, (double)decimal.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? Note { get; set; }

        // Khóa ngoại trỏ tới bảng Wallet
        [Required(ErrorMessage = "Vui lòng chọn ví")]
        public int WalletId { get; set; }
        [ForeignKey("WalletId")]
        public Wallet? Wallet { get; set; }

        // Khóa ngoại trỏ tới bảng Category
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}
