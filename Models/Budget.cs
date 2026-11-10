using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceApp.Models
{
    public class Budget
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục chi tiêu.")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Range(typeof(decimal), "1", "9999999999999999", ErrorMessage = "Ngân sách phải lớn hơn 0.")]
        [Display(Name = "Ngân sách (VNĐ)")]
        public decimal Amount { get; set; }

        [Range(1, 12, ErrorMessage = "Tháng không hợp lệ.")]
        [Display(Name = "Tháng")]
        public int Month { get; set; } = DateTime.Today.Month;

        [Range(2000, 2100, ErrorMessage = "Năm không hợp lệ.")]
        [Display(Name = "Năm")]
        public int Year { get; set; } = DateTime.Today.Year;

        // Các thuộc tính chỉ dùng để hiển thị, không được lưu vào bảng Budgets.
        public string CategoryName { get; set; } = string.Empty;
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount => Amount - SpentAmount;
        public decimal UsagePercentage => Amount <= 0 ? 0 : SpentAmount / Amount * 100m;
        public bool IsWarning => SpentAmount > Amount * 0.8m;
    }
}
