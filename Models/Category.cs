using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        public string Name { get; set; } = string.Empty;

        // 0: Chi tiêu (Expense), 1: Thu nhập (Income)
        public int Type { get; set; } = 0;

        public string Icon { get; set; } = "fa-tag";
        
        public string Color { get; set; } = "#dc3545"; // Đỏ cho chi tiêu, Xanh cho thu nhập

        // Navigation property
        public ICollection<Transaction>? Transactions { get; set; }
    }
}
