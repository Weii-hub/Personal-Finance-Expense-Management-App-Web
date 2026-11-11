using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceApp.Models
{
    public class SavingsGoal
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên mục tiêu.")]
        [StringLength(100, ErrorMessage = "Tên mục tiêu không được vượt quá 100 ký tự.")]
        [Display(Name = "Tên mục tiêu")]
        public string Name { get; set; } = string.Empty;

        [Range(typeof(decimal), "1", "9999999999999999", ErrorMessage = "Số tiền mục tiêu phải lớn hơn 0.")]
        [Display(Name = "Số tiền cần đạt (VNĐ)")]
        public decimal TargetAmount { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Số tiền đã tiết kiệm không được âm.")]
        [Display(Name = "Đã tiết kiệm (VNĐ)")]
        public decimal CurrentAmount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày dự kiến hoàn thành")]
        public DateTime? TargetDate { get; set; }

        public DateTime CreatedAt { get; set; }

        // 0 = Tích lũy Tiền mặt ngoài đời, 1 = Tích lũy Ví điện tử / Ngân hàng
        [Display(Name = "Loại tích lũy")]
        public int GoalType { get; set; } = 0;

        public decimal ProgressPercentage =>
            TargetAmount <= 0 ? 0 : Math.Min(100m, CurrentAmount / TargetAmount * 100m);

        public bool IsCompleted => TargetAmount > 0 && CurrentAmount >= TargetAmount;

        public decimal RemainingAmount => Math.Max(0m, TargetAmount - CurrentAmount);
    }
}
